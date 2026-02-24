using laba1.Helpers;
using System.Text;

public partial class DataManager : IDisposable
{
    private FileStream? _prodFs;
    private FileStream? _specFs;
    private ushort _nameSize;
    private string? _prodPath;
    private string? _specPath;

    // Помощники
    private FileHeaderHelper? _headerHelper;
    private NodeFinder? _nodeFinder;
    private TreePrinter? _treePrinter;
    private Reorganizer? _reorganizer;
    private TruncateHelper? _truncateHelper;

    public bool IsOpen => _prodFs != null && _specFs != null;

    // Инициализация помощников после открытия/создания файлов
    private void InitHelpers()
    {
        if (_prodFs == null || _specFs == null)
            throw new InvalidOperationException("Файлы не открыты");

        _headerHelper = new FileHeaderHelper(_prodFs, _specFs);
        _nodeFinder = new NodeFinder(_prodFs, _specFs, _nameSize);
        _treePrinter = new TreePrinter(_prodFs, _specFs, _nameSize);
        _reorganizer = new Reorganizer(_prodFs, _specFs, _nameSize, _headerHelper);
        _truncateHelper = new TruncateHelper(_prodFs, _specFs, _nameSize, _headerHelper);
    }

    //  CREATE 
    public void Create(string prodPath, ushort dataSize, string? specPath = null)
    {
        _prodFs?.Close();
        _specFs?.Close();

        specPath ??= prodPath + ".prs";
        prodPath += ".prd";
        if (specPath.Length > 16)
            throw new ArgumentException("Имя файла спецификации не может быть длиннее 16 символов.");

        _prodFs = new FileStream(prodPath, FileMode.Create);
        _specFs = new FileStream(specPath, FileMode.Create);

        // Заголовок .prd (28 байт)
        _prodFs.Write(Encoding.ASCII.GetBytes("PS"), 0, 2);
        _prodFs.Write(BitConverter.GetBytes(dataSize), 0, 2);
        _prodFs.Write(BitConverter.GetBytes(-1), 0, 4); // FirstNode
        _prodFs.Write(BitConverter.GetBytes(28), 0, 4); // FreeSpace
        byte[] nameBuf = new byte[16];
        Encoding.ASCII.GetBytes(specPath.PadRight(16)).CopyTo(nameBuf, 0);
        _prodFs.Write(nameBuf, 0, 16);

        // Заголовок .prs (8 байт)
        _specFs.Write(BitConverter.GetBytes(-1), 0, 4); // FirstNode
        _specFs.Write(BitConverter.GetBytes(8), 0, 4);  // FreeSpace

        _nameSize = dataSize;
        _prodPath = prodPath;
        _specPath = specPath;

        _prodFs.Close();
        _specFs.Close();
        _prodFs = null;
        _specFs = null;

        InitHelpers(); // после создания помощники не нужны, но можно оставить для единообразия
    }

    //  OPEN 
    public void Open(string prodPath)
    {
        _prodFs?.Close();
        _specFs?.Close();

        if (!prodPath.EndsWith(".prd")) prodPath += ".prd";
        if (!File.Exists(prodPath)) throw new Exception("Файл не найден.");

        // Проверка, что файл не занят другим процессом
        try
        {
            using (var fs = File.Open(prodPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            _prodFs = new FileStream(prodPath, FileMode.Open, FileAccess.ReadWrite);
        }
        catch (IOException)
        {
            throw new InvalidOperationException($"Файл {prodPath} уже открыт.");
        }

        byte[] sig = new byte[2];
        _prodFs.Read(sig, 0, 2);
        if (Encoding.ASCII.GetString(sig) != "PS")
        {
            _prodFs.Close();
            throw new Exception("Ошибка: Сигнатура файла не соответствует заданию.");
        }

        byte[] dsBuf = new byte[2];
        _prodFs.Read(dsBuf, 0, 2);
        _nameSize = BitConverter.ToUInt16(dsBuf, 0);

        _prodFs.Seek(8, SeekOrigin.Current); // пропускаем FirstNodePtr и FreeSpacePtr
        byte[] nameBuf = new byte[16];
        _prodFs.Read(nameBuf, 0, 16);
        string specName = Encoding.ASCII.GetString(nameBuf).Trim();
        string? dir = Path.GetDirectoryName(prodPath);
        string specPath = Path.Combine(dir ?? "", specName);

        if (!File.Exists(specPath))
            throw new FileNotFoundException($"Файл спецификации {specPath} не найден.");

        _specFs = new FileStream(specPath, FileMode.Open, FileAccess.ReadWrite);
        _prodPath = prodPath;
        _specPath = specPath;

        InitHelpers();
        Console.WriteLine($"файлы {prodPath},{specPath} открыты.");
    }

    //  INPUT (Тип: Изделие, Узел, Деталь) 
    public void AddComponent(string name, string typeStr)
    {
        if (_nodeFinder!.FindNode(name, _headerHelper!.GetFirstProd(), includeDeleted: true) != null)
            throw new Exception("Ошибка: Компонент с таким именем уже существует.");

        if (Encoding.GetEncoding(1251).GetByteCount(name) > _nameSize)
            throw new ArgumentException($"Имя компонента не может быть длиннее {_nameSize} символов.");

        byte type = typeStr.ToLower() switch
        {
            "изделие" => ComponentTypes.Product,
            "узел" => ComponentTypes.Node,
            "деталь" => ComponentTypes.Detail,
            _ => throw new Exception("Неизвестный тип компонента")
        };

        int offset = _headerHelper.GetFreeProd();
        var node = new ProdNodeHelper(_prodFs!, offset, _nameSize);

        node.CanBeDel = 0;
        node.Type = type;
        node.Name = name;
        node.NextNodePtr = _headerHelper.GetFirstProd();  // в начало списка
        node.SpecNodePtr = -1;

        _headerHelper.SetFirstProd(offset);
        _headerHelper.UpdateFreeProd(offset + node.TotalSize);
    }

    //  INPUT (Связь: Родитель/Ребенок) 
    public void AddRelation(string parentName, string childName, ushort count = 1)
    {
        if (count == 0)
            throw new ArgumentException("Кратность вхождения должна быть больше нуля.");

        var parent = _nodeFinder!.FindNode(parentName, _headerHelper!.GetFirstProd());
        var child = _nodeFinder.FindNode(childName, _headerHelper.GetFirstProd());
        if (parent == null || child == null)
            throw new Exception("Компонент не найден");

        AddRelation(parent.Offset, child.Offset, count);
    }

    //  INPUT (Связь: Родитель/Ребенок) Перегрузка для смещений
    public void AddRelation(int parentOffset, int childOffset, ushort count = 1)
    {
        if (count == 0)
            throw new ArgumentException("Кратность вхождения должна быть больше нуля.");
        if (parentOffset == childOffset)
            throw new InvalidOperationException("Нельзя включить компонент в самого себя.");

        var parent = new ProdNodeHelper(_prodFs!, parentOffset, _nameSize);
        var child = new ProdNodeHelper(_prodFs!, childOffset, _nameSize);

        if (parent.CanBeDel != 0 || child.CanBeDel != 0)
            throw new Exception("Один из компонентов помечен на удаление.");
        if (parent.Type == ComponentTypes.Detail)
            throw new Exception("Деталь не может иметь спецификацию!");
        if (child.Type == ComponentTypes.Product)
            throw new Exception("Изделие не может быть комплектующим.");
        if (_nodeFinder!.IsAncestor(parentOffset, childOffset))
            throw new InvalidOperationException("Обнаружена циклическая ссылка.");

        // Проверка на дубликат: если такая связь уже есть, увеличиваем кратность
        int cur = parent.SpecNodePtr;
        while (cur != -1)
        {
            var existing = new SpecNodeHelper(_specFs!, cur);
            if (existing.CanBeDel == 0 && existing.ProdNodePtr == childOffset)
            {
                existing.Mentions += count;
                return; // ничего больше не делаем
            }
            cur = existing.NextNodePtr;
        }

        int newSpecOff = _headerHelper!.GetFreeSpec();
        var newEntry = new SpecNodeHelper(_specFs!, newSpecOff);
        newEntry.CanBeDel = 0;
        newEntry.ProdNodePtr = childOffset;
        newEntry.Mentions = count;
        newEntry.NextNodePtr = parent.SpecNodePtr; // в начало подсписка
        parent.SpecNodePtr = newSpecOff;
        _headerHelper.UpdateFreeSpec(newSpecOff + 11);
    }

    //  DELETE (Логическое удаление компонента) 
    public void DeleteComponent(string name)
    {
        var node = _nodeFinder!.FindNode(name, _headerHelper!.GetFirstProd());
        if (node == null) throw new Exception("Компонент не найден");

        if (_nodeFinder.HasReferences(node.Offset, _headerHelper.GetFirstProd()))
            throw new Exception("Ошибка: на компонент есть ссылки в спецификациях!");

        node.CanBeDel = -1;
    }

    //  DELETE (Удаление связи из спецификации) 
    public void DeleteRelation(string parentName, string childName)
    {
        var parent = _nodeFinder!.FindNode(parentName, _headerHelper!.GetFirstProd());
        if (parent == null) throw new Exception("Родитель не найден");
        var child = _nodeFinder.FindNode(childName, _headerHelper.GetFirstProd());
        if (child == null) throw new Exception("Компонент не найден");

        int curr = parent.SpecNodePtr;
        while (curr != -1)
        {
            var spec = new SpecNodeHelper(_specFs!, curr);
            if (spec.CanBeDel == 0 && spec.ProdNodePtr == child.Offset)
            {
                spec.CanBeDel = -1;
                return;
            }
            curr = spec.NextNodePtr;
        }
        throw new Exception("Такая связь не найдена");
    }

    //  RESTORE (для всех) 
    public void RestoreAll()
    {
        _reorganizer!.RestoreAll();
        Console.WriteLine("Все записи восстановлены и отсортированы.");
    }

    //  RESTORE (для конкретного компонента) 
    public void Restore(string name)
    {
        var node = _nodeFinder!.FindNode(name, _headerHelper!.GetFirstProd(), includeDeleted: true);
        if (node == null) throw new Exception("Компонент не найден");

        if (node.CanBeDel == -1)
            node.CanBeDel = 0;

        int curr = node.SpecNodePtr;
        while (curr != -1)
        {
            var spec = new SpecNodeHelper(_specFs!, curr);
            if (spec.CanBeDel == -1)
                spec.CanBeDel = 0;
            curr = spec.NextNodePtr;
        }

        _reorganizer!.ReorderAll();
        Console.WriteLine($"Компонент {name} и его спецификация восстановлены.");
    }

    //  TRUNCATE (физическое удаление) 
    public void Truncate()
    {
        if (_prodPath == null || _specPath == null)
            throw new InvalidOperationException("Пути к файлам не сохранены.");

        // Выполняем перепаковку (метод закроет текущие потоки и создаст новые файлы)
        _truncateHelper!.Truncate();

        // После перепаковки нужно переоткрыть файлы заново
        _prodFs?.Close();
        _specFs?.Close();

        // Открываем заново
        _prodFs = new FileStream(_prodPath, FileMode.Open, FileAccess.ReadWrite);
        _specFs = new FileStream(_specPath, FileMode.Open, FileAccess.ReadWrite);

        // Переинициализируем помощников
        InitHelpers();

        Console.WriteLine("Truncate выполнен.");
    }

    //  PRINT (*) 
    public void PrintAll()
    {
        Console.WriteLine($"{"Наименование",-16} | {"Тип"}");
        int curr = _headerHelper!.GetFirstProd();
        while (curr != -1)
        {
            var n = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (n.CanBeDel == 0)
            {
                string typeStr = n.Type switch
                {
                    ComponentTypes.Product => "Изделие",
                    ComponentTypes.Node => "Узел",
                    ComponentTypes.Detail => "Деталь",
                    _ => "Неизвестно"
                };
                Console.WriteLine($"{n.Name,-16} | {typeStr}");
            }
            curr = n.NextNodePtr;
        }
    }

    //  PRINT (дерево спецификации) 
    public void PrintComponentTree(string name)
    {
        var node = _nodeFinder!.FindNode(name, _headerHelper!.GetFirstProd());
        if (node == null) throw new Exception("Компонент не найден.");

        if (node.Type == ComponentTypes.Detail)
            throw new Exception("Ошибка: Команда Print для детали невозможна.");

        _treePrinter!.Print(node);
    }

    //  HELP 
    public void Help()
    {
        Console.WriteLine("Доступные команды:");
        Console.WriteLine("  Create имя_файла макс_длина_имени [имя_файла_спецификации]");
        Console.WriteLine("  Open имя_файла");
        Console.WriteLine("  Input имя тип - добавить компонент (тип: Изделие, Узел, Деталь)");
        Console.WriteLine("  Input родитель/ребенок [количество] - добавить связь");
        Console.WriteLine("  Delete имя_компонента - удалить компонент");
        Console.WriteLine("  Delete родитель/ребенок - удалить связь");
        Console.WriteLine("  Restore имя_компонента - восстановить компонент");
        Console.WriteLine("  Restore * - восстановить все");
        Console.WriteLine("  Truncate - физически удалить помеченные записи");
        Console.WriteLine("  Print * - список всех компонентов");
        Console.WriteLine("  Print имя_компонента - дерево спецификации");
        Console.WriteLine("  Help [файл] - показать справку или сохранить в файл");
        Console.WriteLine("  Exit - выход");
        Console.WriteLine("  - Если имя содержит пробелы, заключайте его в кавычки");
        Console.WriteLine("  - Расширения .prd и .prs добавляются автоматически при необходимости");
        Console.WriteLine("  - Кратность связи по умолчанию = 1");
    }

    //  Поиск узла по имени (публичный метод)
    public ProdNodeHelper? FindNode(string name, bool includeDeleted = false)
    {
        return _nodeFinder?.FindNode(name, _headerHelper!.GetFirstProd(), includeDeleted);
    }

    //  Получить список активных компонентов (для GUI)
    public List<(int Offset, string Name, byte Type)> GetActiveComponents()
    {
        var list = new List<(int, string, byte)>();
        int curr = _headerHelper!.GetFirstProd();
        while (curr != -1)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (node.CanBeDel == 0)
            {
                list.Add((curr, node.Name, node.Type));
            }
            curr = node.NextNodePtr;
        }
        return list;
    }

    //  Обновление компонента (для GUI)
    public void UpdateComponent(int offset, string newName, byte newType)
    {
        // Проверка существования компонента с таким именем (кроме текущего)
        int curr = _headerHelper!.GetFirstProd();
        while (curr != -1)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (curr != offset && node.CanBeDel == 0 && node.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Компонент с таким именем уже существует.");
            }
            curr = node.NextNodePtr;
        }

        // Запрещаем смену на "Изделие", если компонент уже используется как комплектующий
        if (newType == ComponentTypes.Product && IsComponentUsedAsChild(offset))
        {
            throw new Exception("Нельзя изменить тип на 'Изделие', так как компонент уже используется в качестве комплектующего в других спецификациях.");
        }

        // Обновляем поля
        var target = new ProdNodeHelper(_prodFs!, offset, _nameSize);
        if (newType == ComponentTypes.Detail && target.SpecNodePtr != -1)
            throw new Exception("Нельзя изменить тип на 'Деталь', так как компонент имеет спецификацию.");

        target.Name = newName;
        target.Type = newType;

        // Перестраиваем алфавитный порядок, если необходимо
        _reorganizer!.ReorderAll();
    }

    //  Удаление компонента по смещению (для GUI)
    public void DeleteComponent(int offset)
    {
        var node = new ProdNodeHelper(_prodFs!, offset, _nameSize);
        if (node.CanBeDel != 0)
            return; // уже удалён

        if (_nodeFinder!.HasReferences(offset, _headerHelper!.GetFirstProd()))
            throw new Exception($"Ошибка: на компонент '{node.Name}' есть ссылки в спецификациях!");

        node.CanBeDel = -1;
    }

    //  Удаление связи по смещению записи спецификации (для GUI)
    public void DeleteRelation(int specOffset)
    {
        var spec = new SpecNodeHelper(_specFs!, specOffset);
        if (spec.CanBeDel != 0)
            return; // уже удалена

        spec.CanBeDel = -1;
    }

    //  Обновление кратности (для GUI)
    public void UpdateMentions(int specOffset, ushort newMentions)
    {
        if (newMentions == 0)
            throw new ArgumentException("Кратность должна быть больше нуля.");

        var spec = new SpecNodeHelper(_specFs!, specOffset);
        if (spec.CanBeDel != 0)
            throw new Exception("Запись спецификации помечена на удаление.");

        spec.Mentions = newMentions;
    }

    //  Проверка, используется ли компонент как комплектующий (вспомогательный для UpdateComponent)
    private bool IsComponentUsedAsChild(int componentOffset)
    {
        int currComp = _headerHelper!.GetFirstProd();
        while (currComp != -1)
        {
            var parent = new ProdNodeHelper(_prodFs!, currComp, _nameSize);
            if (parent.CanBeDel == 0) // только активные родители
            {
                int currSpec = parent.SpecNodePtr;
                while (currSpec != -1)
                {
                    var spec = new SpecNodeHelper(_specFs!, currSpec);
                    if (spec.CanBeDel == 0 && spec.ProdNodePtr == componentOffset)
                        return true;
                    currSpec = spec.NextNodePtr;
                }
            }
            currComp = parent.NextNodePtr;
        }
        return false;
    }

    public SpecTreeNode? GetSpecificationTree(string componentName)
    {
        var node = _nodeFinder!.FindNode(componentName, _headerHelper!.GetFirstProd());
        if (node == null) return null;
        if (node.Type == ComponentTypes.Detail) return null; // деталь не имеет спецификации
        return BuildSpecTree(node.Offset);
    }

    private SpecTreeNode BuildSpecTree(int prodOffset)
    {
        var prodNode = new ProdNodeHelper(_prodFs!, prodOffset, _nameSize);
        string typeStr = prodNode.Type switch
        {
            ComponentTypes.Product => "Изделие",
            ComponentTypes.Node => "Узел",
            ComponentTypes.Detail => "Деталь",
            _ => "Неизвестно"
        };
        var treeNode = new SpecTreeNode
        {
            ProdOffset = prodOffset,
            Name = prodNode.Name,
            Type = prodNode.Type,
            Mentions = 1,
            Text = $"{prodNode.Name} ({typeStr})"
        };

        if (prodNode.SpecNodePtr != -1)
        {
            int currSpec = prodNode.SpecNodePtr;
            while (currSpec != -1)
            {
                var spec = new SpecNodeHelper(_specFs!, currSpec);
                if (spec.CanBeDel == 0)
                {
                    int childOffset = spec.ProdNodePtr;
                    var childProd = new ProdNodeHelper(_prodFs!, childOffset, _nameSize);
                    if (childProd.CanBeDel == 0)
                    {
                        var childNode = BuildSpecTree(childOffset);
                        if (childNode != null)
                        {
                            childNode.Mentions = spec.Mentions;
                            childNode.SpecOffset = currSpec;
                            childNode.Text = $"{childProd.Name} (x{spec.Mentions})";
                            treeNode.Children.Add(childNode);
                        }
                    }
                }
                currSpec = spec.NextNodePtr;
            }
        }
        return treeNode;
    }
  
    public ushort NameSize => _nameSize;

    public void Dispose()
    {
        _prodFs?.Close();
        _specFs?.Close();
    }
}
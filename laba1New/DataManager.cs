using laba1New.Helpers;
using System.Text;

public partial class DataManager : IDisposable
{
    private FileStream? _prodFs;
    private FileStream? _specFs;
    private ushort _nameSize;

    // --- CREATE ---
    public void Create(string prodName, ushort dataSize, string? specName = null)
    {
        specName ??= prodName + ".prs";
        if (specName.Length > 16)
            throw new ArgumentException("Имя файла спецификации не может быть длиннее 16 символов.");

        _prodFs = new FileStream(prodName + ".prd", FileMode.Create);
        _specFs = new FileStream(specName, FileMode.Create);

        // Заголовок .prd (28 байт)
        _prodFs.Write(Encoding.ASCII.GetBytes("PS"), 0, 2);
        _prodFs.Write(BitConverter.GetBytes(dataSize), 0, 2);
        _prodFs.Write(BitConverter.GetBytes(-1), 0, 4); // FirstNode
        _prodFs.Write(BitConverter.GetBytes(28), 0, 4); // FreeSpace
        byte[] nameBuf = new byte[16];
        Encoding.ASCII.GetBytes(specName.PadRight(16)).CopyTo(nameBuf, 0);
        _prodFs.Write(nameBuf, 0, 16);

        // Заголовок .prs (8 байт)
        _specFs.Write(BitConverter.GetBytes(-1), 0, 4); // FirstNode
        _specFs.Write(BitConverter.GetBytes(8), 0, 4);  // FreeSpace
        _nameSize = dataSize;
    }

    // --- OPEN ---
    public void Open(string prodPath)
    {
        if (!prodPath.EndsWith(".prd")) prodPath += ".prd";
        _prodFs = new FileStream(prodPath, FileMode.Open, FileAccess.ReadWrite);

        byte[] sig = new byte[2];
        _prodFs.Read(sig, 0, 2);
        if (Encoding.ASCII.GetString(sig) != "PS")
            throw new Exception("Неверная сигнатура файла .prd");

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

        Console.WriteLine($"База {prodPath} открыта.");
    }



    // --- INPUT (Тип: Изделие, Узел, Деталь) ---
    public void AddComponent(string name, string typeStr)
    {
        if (FindNode(name, includeDeleted: true) != null)
            throw new Exception("Ошибка: Компонент с таким именем уже существует.");
        byte type = typeStr.ToLower() switch
        {
            "изделие" => ComponentTypes.Product,
            "узел" => ComponentTypes.Node,
            "деталь" => ComponentTypes.Detail,
            _ => throw new Exception("Неизвестный тип компонента")
        };

        int offset = GetFreeProd();
        var node = new ProdNodeHelper(_prodFs!, offset, _nameSize);

        node.CanBeDel = 0;
        node.Type = type;
        node.Name = name;
        node.NextNodePtr = GetFirstProd();  // в начало списка
        node.SpecNodePtr = -1;

        SetFirstProd(offset);
        UpdateFreeProd(offset + node.TotalSize);
    }

    // --- INPUT (Связь: Родитель/Ребенок) ---
    public void AddRelation(string parentName, string childName, ushort count = 1)
    {
        if(count == 0)
        throw new ArgumentException("Кратность вхождения должна быть больше нуля.");

        var p = FindNode(parentName);
        var c = FindNode(childName);
        if (p == null || c == null) throw new Exception("Компонент не найден");

        if (p.Type == ComponentTypes.Detail)
            throw new Exception("Деталь не может иметь спецификацию!");

        int newSpecOff = GetFreeSpec();
        var newEntry = new SpecNodeHelper(_specFs!, newSpecOff);

        newEntry.CanBeDel = 0;
        newEntry.ProdNodePtr = c.Offset;
        newEntry.Mentions = count;
        newEntry.NextNodePtr = p.SpecNodePtr; // в начало списка

        p.SpecNodePtr = newSpecOff;
        UpdateFreeSpec(newSpecOff + 11);
    }

    // --- DELETE (Логическое удаление компонента) ---
    public void DeleteComponent(string name)
    {
        var node = FindNode(name);
        if (node == null) throw new Exception("Компонент не найден");

        if (HasReferences(node.Offset))
            throw new Exception("Ошибка: на компонент есть ссылки в спецификациях!");

        node.CanBeDel = -1;
    }

    // --- DELETE (Удаление связи из спецификации) ---
    public void DeleteRelation(string parentName, string childName)
    {
        var parent = FindNode(parentName);
        if (parent == null) throw new Exception("Родитель не найден");
        var child = FindNode(childName);
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

    // --- RESTORE (для всех) ---
    public void RestoreAll()
    {
        int curr = 28;
        int freeSpace = GetFreeProd();
        while (curr < freeSpace)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            node.CanBeDel = 0;
            curr += node.TotalSize;
        }
        ReorderAll();
        Console.WriteLine("Все записи восстановлены и отсортированы.");
    }

    // --- RESTORE (для конкретного компонента) ---
    public void Restore(string name)
    {
        var node = FindNode(name, includeDeleted: true);
        if (node == null) throw new Exception("Компонент не найден");
        RestoreNodeAndSpec(node);
        ReorderAll();
        Console.WriteLine($"Компонент {name} и его спецификация восстановлены.");
    }

    private void RestoreNodeAndSpec(ProdNodeHelper node)
    {
        if (node.CanBeDel == 0) return;
        node.CanBeDel = 0;
        int curr = node.SpecNodePtr;
        while (curr != -1)
        {
            var spec = new SpecNodeHelper(_specFs!, curr);
            if (spec.CanBeDel == -1)
                spec.CanBeDel = 0;
            curr = spec.NextNodePtr;
        }
    }

    // --- Перестроение алфавитного порядка всех активных записей .prd ---
    private void ReorderAll()
    {
        var activeNodes = new List<(int offset, string name)>();
        int curr = 28;
        int freeSpace = GetFreeProd();
        while (curr < freeSpace)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (node.CanBeDel == 0)
                activeNodes.Add((curr, node.Name));
            curr += node.TotalSize;
        }
        activeNodes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
        if (activeNodes.Count > 0)
        {
            SetFirstProd(activeNodes[0].offset);
            for (int i = 0; i < activeNodes.Count; i++)
            {
                var node = new ProdNodeHelper(_prodFs!, activeNodes[i].offset, _nameSize);
                node.NextNodePtr = (i < activeNodes.Count - 1) ? activeNodes[i + 1].offset : -1;
            }
        }
        else
        {
            SetFirstProd(-1);
        }
    }

    // --- TRUNCATE (физическое удаление) ---
    public void Truncate()
    {
        Console.WriteLine("Выполняется физическое сжатие...");

        // --- .prd ---
        var activeProd = new List<(int oldOffset, byte[] data, int nextPtr, int specPtr, byte type, string name)>();
        int curr = 28;
        int freeProd = GetFreeProd();
        while (curr < freeProd)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (node.CanBeDel == 0)
            {
                byte[] data = new byte[node.TotalSize];
                _prodFs.Seek(curr, SeekOrigin.Begin);
                _prodFs.Read(data, 0, node.TotalSize);
                activeProd.Add((curr, data, node.NextNodePtr, node.SpecNodePtr, node.Type, node.Name));
            }
            curr += node.TotalSize;
        }

        if (activeProd.Count == 0)
        {
            SetFirstProd(-1);
            UpdateFreeProd(28);
            SetFirstSpec(-1);
            UpdateFreeSpec(8);
            Console.WriteLine("Файлы пусты.");
            return;
        }

        Dictionary<int, int> oldToNewProd = new();
        int newOffset = 28;
        foreach (var item in activeProd)
        {
            oldToNewProd[item.oldOffset] = newOffset;
            newOffset += 10 + _nameSize;
        }

        string tempProd = _prodFs!.Name + ".tmp";
        using (var fs = new FileStream(tempProd, FileMode.Create))
        {
            fs.Write(Encoding.ASCII.GetBytes("PS"), 0, 2);
            fs.Write(BitConverter.GetBytes(_nameSize), 0, 2);
            fs.Write(BitConverter.GetBytes(28), 0, 4);
            fs.Write(BitConverter.GetBytes(28 + activeProd.Count * (10 + _nameSize)), 0, 4);
            byte[] nameBuf = new byte[16];
            string specFileName = Path.GetFileName(_specFs!.Name);
            Encoding.ASCII.GetBytes(specFileName.PadRight(16)).CopyTo(nameBuf, 0);
            fs.Write(nameBuf, 0, 16);

            for (int i = 0; i < activeProd.Count; i++)
            {
                var item = activeProd[i];
                int newNext = (i < activeProd.Count - 1) ? oldToNewProd[activeProd[i + 1].oldOffset] : -1;
                fs.Seek(oldToNewProd[item.oldOffset], SeekOrigin.Begin);
                fs.WriteByte(0);
                fs.WriteByte(item.type);
                fs.Write(BitConverter.GetBytes(item.specPtr), 0, 4);
                fs.Write(BitConverter.GetBytes(newNext), 0, 4);
                byte[] nameBytes = new byte[_nameSize];
                Encoding.UTF8.GetBytes(item.name.PadRight(_nameSize, ' ')).CopyTo(nameBytes, 0);
                fs.Write(nameBytes, 0, _nameSize);
            }
        }

        _prodFs.Close();
        File.Delete(_prodFs.Name);
        File.Move(tempProd, _prodFs.Name);
        _prodFs = new FileStream(_prodFs.Name, FileMode.Open, FileAccess.ReadWrite);

        // --- .prs ---
        var activeSpec = new List<(int oldOffset, byte[] data, int prodPtr, int nextPtr, ushort mentions)>();
        curr = 8;
        int freeSpec = GetFreeSpec();
        while (curr < freeSpec)
        {
            var spec = new SpecNodeHelper(_specFs!, curr);
            if (spec.CanBeDel == 0 && oldToNewProd.ContainsKey(spec.ProdNodePtr))
            {
                byte[] data = new byte[11];
                _specFs.Seek(curr, SeekOrigin.Begin);
                _specFs.Read(data, 0, 11);
                activeSpec.Add((curr, data, spec.ProdNodePtr, spec.NextNodePtr, spec.Mentions));
            }
            curr += 11;
        }

        Dictionary<int, int> oldToNewSpec = new();
        newOffset = 8;
        foreach (var item in activeSpec)
        {
            oldToNewSpec[item.oldOffset] = newOffset;
            newOffset += 11;
        }

        string tempSpec = _specFs!.Name + ".tmp";
        using (var fs = new FileStream(tempSpec, FileMode.Create))
        {
            fs.Write(BitConverter.GetBytes(activeSpec.Count > 0 ? 8 : -1), 0, 4);
            fs.Write(BitConverter.GetBytes(8 + activeSpec.Count * 11), 0, 4);

            for (int i = 0; i < activeSpec.Count; i++)
            {
                var item = activeSpec[i];
                int newNext = (i < activeSpec.Count - 1) ? oldToNewSpec[activeSpec[i + 1].oldOffset] : -1;
                int newProd = oldToNewProd[item.prodPtr];
                fs.Seek(oldToNewSpec[item.oldOffset], SeekOrigin.Begin);
                fs.WriteByte(0);
                fs.Write(BitConverter.GetBytes(newProd), 0, 4);
                fs.Write(BitConverter.GetBytes(item.mentions), 0, 2);
                fs.Write(BitConverter.GetBytes(newNext), 0, 4);
            }
        }

        _specFs.Close();
        File.Delete(_specFs.Name);
        File.Move(tempSpec, _specFs.Name);
        _specFs = new FileStream(_specFs.Name, FileMode.Open, FileAccess.ReadWrite);

        Console.WriteLine("Физическое сжатие завершено.");
    }

    // --- PRINT (*) ---
    public void PrintAll()
    {
        Console.WriteLine($"{"Наименование",-16} | {"Тип"}");
        int curr = GetFirstProd();
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

    // --- PRINT (дерево спецификации) ---
    public void PrintComponentTree(string name)
    {
        var node = FindNode(name);
        if (node == null)
        {
            Console.WriteLine("Ошибка: Компонент не найден.");
            return;
        }

        if (node.SpecNodePtr == -1)
        {
            Console.WriteLine($"Ошибка: {name} является деталью и не имеет спецификации.");
            return;
        }

        Console.WriteLine($"\n{node.Name}");
        PrintRecursive(node.SpecNodePtr, 1);
    }

    private void PrintRecursive(int specOffset, int level)
    {
        var spec = new SpecNodeHelper(_specFs!, specOffset);
        int currentEntry = specOffset;

        while (currentEntry != -1)
        {
            spec.SetOffset(currentEntry);
            if (spec.CanBeDel == 0)
            {
                var component = new ProdNodeHelper(_prodFs!, spec.ProdNodePtr, _nameSize);

                string indent = new string('|', level).Replace("|", "|   ");
                Console.WriteLine($"{indent}|");
                Console.WriteLine($"{indent} {component.Name} (x{spec.Mentions})");

                if (component.SpecNodePtr != -1)
                {
                    PrintRecursive(component.SpecNodePtr, level + 1);
                }
            }
            currentEntry = spec.NextNodePtr;
        }
    }

    // --- HELP ---
    public void Help()
    {
        Console.WriteLine("Доступные команды:");
        Console.WriteLine("  Create имя_файла(длина_имени[, имя_файла_спецификаций])");
        Console.WriteLine("  Open имя_файла");
        Console.WriteLine("  Input (имя_компонента, тип) - тип: Изделие, Узел, Деталь");
        Console.WriteLine("  Input (родитель/ребенок) - добавить связь в спецификацию");
        Console.WriteLine("  Delete (имя_компонента) - удалить компонент");
        Console.WriteLine("  Delete (родитель/ребенок) - удалить связь");
        Console.WriteLine("  Restore имя_компонента - восстановить компонент и его спецификацию");
        Console.WriteLine("  Restore * - восстановить все");
        Console.WriteLine("  Truncate - физически удалить помеченные записи");
        Console.WriteLine("  Print * - список всех компонентов");
        Console.WriteLine("  Print имя_компонента - дерево спецификации");
        Console.WriteLine("  Exit - выход");
    }

    // --- Вспомогательные методы для работы с указателями ---
    private int GetFirstProd() { _prodFs!.Seek(4, SeekOrigin.Begin); return ReadInt(_prodFs); }
    private void SetFirstProd(int v) { _prodFs!.Seek(4, SeekOrigin.Begin); WriteInt(_prodFs, v); }
    private int GetFreeProd() { _prodFs!.Seek(8, SeekOrigin.Begin); return ReadInt(_prodFs); }
    private void UpdateFreeProd(int v) { _prodFs!.Seek(8, SeekOrigin.Begin); WriteInt(_prodFs, v); }

    private int GetFirstSpec() { _specFs!.Seek(0, SeekOrigin.Begin); return ReadInt(_specFs); }
    private void SetFirstSpec(int v) { _specFs!.Seek(0, SeekOrigin.Begin); WriteInt(_specFs, v); }
    private int GetFreeSpec() { _specFs!.Seek(4, SeekOrigin.Begin); return ReadInt(_specFs); }
    private void UpdateFreeSpec(int v) { _specFs!.Seek(4, SeekOrigin.Begin); WriteInt(_specFs, v); }

    private int ReadInt(FileStream fs) { byte[] b = new byte[4]; fs.Read(b, 0, 4); return BitConverter.ToInt32(b, 0); }
    private void WriteInt(FileStream fs, int v) { fs.Write(BitConverter.GetBytes(v), 0, 4); }

    // --- Поиск узла по имени ---
    public ProdNodeHelper? FindNode(string name, bool includeDeleted = false)
    {
        int curr = GetFirstProd();
        while (curr != -1)
        {
            var n = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (n.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (includeDeleted || n.CanBeDel == 0) return n;
            }
            curr = n.NextNodePtr;
        }
        return null;
    }

    // --- Проверка наличия ссылок на компонент в спецификациях ---
    private bool HasReferences(int prodOffset)
    {
        int curr = GetFirstSpec();
        while (curr != -1)
        {
            var spec = new SpecNodeHelper(_specFs!, curr);
            if (spec.CanBeDel == 0 && spec.ProdNodePtr == prodOffset)
                return true;
            curr = spec.NextNodePtr;
        }
        return false;
    }

    public List<(int Offset, string Name, byte Type)> GetActiveComponents()
    {
        var list = new List<(int, string, byte)>();
        int curr = GetFirstProd();
        while (curr != -1)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (node.CanBeDel == 0)
            {
                list.Add((curr, node.Name, node.Type));
            }
            curr = node.NextNodePtr;
        }
        // Возвращаем в текущем порядке (алфавитном, если файл поддерживает)
        return list;
    }

    public void UpdateComponent(int offset, string newName, byte newType)
    {
        // Проверка существования компонента с таким именем (кроме текущего)
        int curr = GetFirstProd();
        while (curr != -1)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (curr != offset && node.CanBeDel == 0 && node.Name.Equals(newName, StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception("Компонент с таким именем уже существует.");
            }
            curr = node.NextNodePtr;
        }

        // Обновляем поля
        var target = new ProdNodeHelper(_prodFs!, offset, _nameSize);
        target.Name = newName;
        target.Type = newType;

        // Перестраиваем алфавитный порядок, если необходимо
        ReorderAll();
    }

    public void DeleteComponent(int offset)
    {
        var node = new ProdNodeHelper(_prodFs!, offset, _nameSize);
        if (node.CanBeDel != 0)
            return; // уже удалён

        string name = node.Name; // для сообщения
        if (HasReferences(offset))
            throw new Exception($"Ошибка: на компонент '{name}' есть ссылки в спецификациях!");

        node.CanBeDel = -1;
    }

    // Доступ к потокам и размеру имени (для использования в GUI)
    public FileStream GetProdStream()
    {
        if (_prodFs == null) throw new InvalidOperationException("Файл не открыт.");
        return _prodFs;
    }

    public FileStream GetSpecStream()
    {
        if (_specFs == null) throw new InvalidOperationException("Файл не открыт.");
        return _specFs;
    }

    public ushort NameSize => _nameSize;

    // Добавление связи по смещениям
    public void AddRelation(int parentOffset, int childOffset, ushort count = 1)
    {
        if (count == 0)
            throw new ArgumentException("Кратность вхождения должна быть больше нуля.");

        var parent = new ProdNodeHelper(_prodFs!, parentOffset, _nameSize);
        var child = new ProdNodeHelper(_prodFs!, childOffset, _nameSize);

        // Проверки как в оригинальном методе
        if (parent.CanBeDel != 0 || child.CanBeDel != 0)
            throw new Exception("Один из компонентов помечен на удаление.");

        if (parent.Type == ComponentTypes.Detail)
            throw new Exception("Деталь не может иметь спецификацию!");

        int newSpecOff = GetFreeSpec();
        var newEntry = new SpecNodeHelper(_specFs!, newSpecOff);

        newEntry.CanBeDel = 0;
        newEntry.ProdNodePtr = childOffset;
        newEntry.Mentions = count;
        newEntry.NextNodePtr = parent.SpecNodePtr;

        parent.SpecNodePtr = newSpecOff;
        UpdateFreeSpec(newSpecOff + 11);
    }

    // Удаление связи по смещению записи спецификации
    public void DeleteRelation(int specOffset)
    {
        var spec = new SpecNodeHelper(_specFs!, specOffset);
        if (spec.CanBeDel != 0)
            return; // уже удалена

        spec.CanBeDel = -1;
        // Примечание: не корректируем указатели, так как это логическое удаление
    }

    // Обновление кратности
    public void UpdateMentions(int specOffset, ushort newMentions)
    {
        if (newMentions == 0)
            throw new ArgumentException("Кратность должна быть больше нуля.");

        var spec = new SpecNodeHelper(_specFs!, specOffset);
        if (spec.CanBeDel != 0)
            throw new Exception("Запись спецификации помечена на удаление.");

        spec.Mentions = newMentions;
    }

    

    public void Dispose()
    {
        _prodFs?.Close();
        _specFs?.Close();
    }
}
using laba1New.Helpers;
using System.Text;

public partial class DataManager : IDisposable
{
    private FileStream? _prodFs;
    private FileStream? _specFs;
    private ushort _nameSize;

    //  CREATE 
    public void Create(string prodName, ushort dataSize, string? specName = null)
    {
        _prodFs?.Close();
        _specFs?.Close();

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
        
        // Принудительно сбрасываем буферы на диск
        _prodFs.Flush();
        _specFs.Flush();
        _nameSize = dataSize;
    }

    //  OPEN 
    public void Open(string prodPath)
    {

        if (!prodPath.EndsWith(".prd")) prodPath += ".prd";
        if (!File.Exists(prodPath)) throw new Exception("Файл не найден.");
        try
        {
            using (var fs = File.Open(prodPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            { }
            _prodFs = new FileStream(prodPath, FileMode.Open, FileAccess.ReadWrite);
        }
        catch (IOException)
        {
            throw new InvalidOperationException($"Файл {prodPath} уже открыт.");
        }

        // Проверка сигнатуры
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



        Console.WriteLine($"База {prodPath} открыта.");
    }

    //  INPUT (Тип: Изделие, Узел, Деталь) 
    public void AddComponent(string name, string typeStr)
    {
        if (FindNode(name, includeDeleted: true) != null)
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

    //  INPUT (Связь: Родитель/Ребенок) 
    public void AddRelation(string parentName, string childName, ushort count = 1)
    {
        if (count == 0)
            throw new ArgumentException("Кратность вхождения должна быть больше нуля.");

        var parent = FindNode(parentName);
        var child = FindNode(childName);
        if (parent == null || child == null)
            throw new Exception("Компонент не найден");

        // Вызов основной логики по смещениям
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
        if (IsAncestor(parentOffset, childOffset))
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

        int newSpecOff = GetFreeSpec();
        var newEntry = new SpecNodeHelper(_specFs!, newSpecOff);
        newEntry.CanBeDel = 0;
        newEntry.ProdNodePtr = childOffset;
        newEntry.Mentions = count;
        newEntry.NextNodePtr = parent.SpecNodePtr; // в начало подсписка
        parent.SpecNodePtr = newSpecOff;
        UpdateFreeSpec(newSpecOff + 11);
    }

    //  DELETE (Логическое удаление компонента) 
    public void DeleteComponent(string name)
    {
        var node = FindNode(name);
        if (node == null) throw new Exception("Компонент не найден");

        if (HasReferences(node.Offset))
            throw new Exception("Ошибка: на компонент есть ссылки в спецификациях!");

        node.CanBeDel = -1;
    }

    //  DELETE (Удаление связи из спецификации) 
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
    
    //  RESTORE (для всех) 
    public void RestoreAll()
    {
        int curr = 28;
        int freeProd = GetFreeProd();
        while (curr < freeProd)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            node.CanBeDel = 0;
            curr += node.TotalSize;
        }
        int currSpec = 8;
        int freeSpec = GetFreeSpec();
        while (currSpec < freeSpec)
        {
            var spec = new SpecNodeHelper(_specFs!, currSpec);
            spec.CanBeDel = 0;
            currSpec += 11; // фиксированный размер записи спецификации
        }
        ReorderAll();
        Console.WriteLine("Все записи восстановлены и отсортированы.");
    }
    
    //  RESTORE (для конкретного компонента) 
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
    }
    
    //  Перестроение алфавитного порядка всех активных записей .prd 
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
    
    //  TRUNCATE (физическое удаление) 
    public void Truncate()
    {
        // 1. Сохраняем старые границы файлов
        int oldFreeProd = GetFreeProd();
        int oldFreeSpec = GetFreeSpec();

        // 2. Собираем все активные компоненты из .prd
        var activeComps = new List<(int oldOffset, byte type, int oldSpecPtr, string name)>();
        int curr = 28;
        while (curr < oldFreeProd)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            if (node.CanBeDel == 0)
                activeComps.Add((curr, node.Type, node.SpecNodePtr, node.Name));
            curr += node.TotalSize;
        }

        // 3. Маппинг старых смещений компонентов на новые
        var oldToNewComp = new Dictionary<int, int>();
        int newCompOffset = 28;
        int compSize = 10 + _nameSize;
        foreach (var comp in activeComps)
        {
            oldToNewComp[comp.oldOffset] = newCompOffset;
            newCompOffset += compSize;
        }

        // 4. Читаем все записи спецификаций из .prs
        var allSpecs = new Dictionary<int, (sbyte canBeDel, int prodPtr, ushort mentions, int nextPtr)>();
        curr = 8;
        while (curr < oldFreeSpec)
        {
            var spec = new SpecNodeHelper(_specFs!, curr);
            allSpecs[curr] = (spec.CanBeDel, spec.ProdNodePtr, spec.Mentions, spec.NextNodePtr);
            curr += 11;
        }

        // 5. Для каждого активного компонента собираем цепочки активных записей спецификации,
        //    которые ссылаются на активные компоненты
        var compToSpecs = new Dictionary<int, List<int>>(); // ключ – старый offset компонента
        foreach (var comp in activeComps)
        {
            var specOffsets = new List<int>();
            int curSpec = comp.oldSpecPtr;
            while (curSpec != -1)
            {
                if (allSpecs.TryGetValue(curSpec, out var specData) &&
                    specData.canBeDel == 0 &&
                    oldToNewComp.ContainsKey(specData.prodPtr))
                {
                    specOffsets.Add(curSpec);
                }
                curSpec = allSpecs.ContainsKey(curSpec) ? allSpecs[curSpec].nextPtr : -1;
            }
            if (specOffsets.Count > 0)
                compToSpecs[comp.oldOffset] = specOffsets;
        }

        // 6. Определяем новые смещения для всех активных записей спецификации,
        //    сохраняя порядок обхода компонентов (это станет глобальным порядком)
        var oldToNewSpec = new Dictionary<int, int>();
        int newSpecOffset = 8;
        foreach (var comp in activeComps)
        {
            if (compToSpecs.TryGetValue(comp.oldOffset, out var specList))
            {
                foreach (var oldSpec in specList)
                {
                    oldToNewSpec[oldSpec] = newSpecOffset;
                    newSpecOffset += 11;
                }
            }
        }
        int totalActiveSpecs = oldToNewSpec.Count;
        // 8. Записываем новый файл спецификаций
string tempSpecPath = _specFs!.Name + ".tmp";
using (var newSpecFs = new FileStream(tempSpecPath, FileMode.Create))
{
    // Заголовок: FirstSpec = -1 (не используется), FreeSpace
    WriteInt(newSpecFs, -1);
    WriteInt(newSpecFs, 8 + totalActiveSpecs * 11);

    // Записываем записи группами по родителям
    foreach (var comp in activeComps)
    {
        if (compToSpecs.TryGetValue(comp.oldOffset, out var specList))
        {
            for (int i = 0; i < specList.Count; i++)
            {
                int oldSpec = specList[i];
                var data = allSpecs[oldSpec];
                int newOff = oldToNewSpec[oldSpec];
                int nextInGroup = (i < specList.Count - 1) ? oldToNewSpec[specList[i + 1]] : -1;

                newSpecFs.WriteByte(0); // canBeDel
                WriteInt(newSpecFs, oldToNewComp[data.prodPtr]);
                WriteUshort(newSpecFs, data.mentions);
                WriteInt(newSpecFs, nextInGroup);
            }
        }
    }
}

// 9. Записываем новый файл компонентов
string tempProdPath = _prodFs!.Name + ".tmp";
using (var newProdFs = new FileStream(tempProdPath, FileMode.Create))
{
    // Заголовок
    newProdFs.Write(Encoding.ASCII.GetBytes("PS"), 0, 2);
    WriteUshort(newProdFs, _nameSize);
    int firstProd = activeComps.Count > 0 ? oldToNewComp[activeComps[0].oldOffset] : -1;
    WriteInt(newProdFs, firstProd);
    WriteInt(newProdFs, 28 + activeComps.Count * compSize); // freeSpace
    
    // Имя файла спецификации в заголовке (должно быть ASCII, т.к. это имя файла)
    byte[] nameBuf = new byte[16];
    string specFileName = Path.GetFileName(_specFs.Name);
    Encoding.ASCII.GetBytes(specFileName.PadRight(16)).CopyTo(nameBuf, 0);
    newProdFs.Write(nameBuf, 0, 16);

    // Кодировка для имён компонентов (windows-1251 для поддержки русского)
    Encoding nameEncoding = Encoding.GetEncoding(1251);

    // Записи компонентов
    for (int i = 0; i < activeComps.Count; i++)
    {
        var comp = activeComps[i];
        int newOff = oldToNewComp[comp.oldOffset];
        int newNext = (i < activeComps.Count - 1) ? oldToNewComp[activeComps[i + 1].oldOffset] : -1;
        int newSpecPtr = -1;
        if (compToSpecs.TryGetValue(comp.oldOffset, out var specList) && specList.Count > 0)
            newSpecPtr = oldToNewSpec[specList[0]];

        newProdFs.Seek(newOff, SeekOrigin.Begin);
        newProdFs.WriteByte(0); // canBeDel
        newProdFs.WriteByte(comp.type);
        WriteInt(newProdFs, newSpecPtr);
        WriteInt(newProdFs, newNext);
        
        // Запись имени компонента в windows-1251 с правильным заполнением пробелами
        byte[] nameBytes = new byte[_nameSize];
        byte[] src = nameEncoding.GetBytes(comp.name);
        
        if (src.Length > _nameSize)
            throw new Exception($"Имя компонента '{comp.name}' слишком длинное (макс. {_nameSize} байт)");
        
        // Копируем байты имени
        Array.Copy(src, nameBytes, src.Length);
        
        // Заполняем остаток пробелами (код 32 в любой однобайтовой кодировке)
        for (int j = src.Length; j < _nameSize; j++)
            nameBytes[j] = 32;
        
        newProdFs.Write(nameBytes, 0, _nameSize);
    }
}

// 10. Замена старых файлов новыми
_prodFs.Close();
_specFs.Close();
File.Delete(_prodFs.Name);
File.Move(tempProdPath, _prodFs.Name);
File.Delete(_specFs.Name);
File.Move(tempSpecPath, _specFs.Name);
_prodFs = new FileStream(_prodFs.Name, FileMode.Open, FileAccess.ReadWrite);
_specFs = new FileStream(_specFs.Name, FileMode.Open, FileAccess.ReadWrite);

Console.WriteLine("Truncate выполнен.");
        }
    
    // Добавим метод WriteUshort для удобства
    private void WriteUshort(FileStream fs, ushort value)
    {
        fs.Write(BitConverter.GetBytes(value), 0, 2);
    } 
    
    //  PRINT (*) 
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
    
    //  PRINT (дерево спецификации) 
    public void PrintComponentTree(string name)
    {
        var node = FindNode(name);
        if (node == null) throw new Exception("Компонент не найден.");

        if (node.Type == ComponentTypes.Detail)
            throw new Exception("Ошибка: Команда Print для детали невозможна.");

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
                if (component.CanBeDel != 0) continue; // пропускаем удалённые
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

    //  HELP 
    public void Help()
    {
        Console.WriteLine("Доступные команды:");
        Console.WriteLine();
        Console.WriteLine("  Create имя_файла макс_длина_имени [имя_файла_спецификации]");
        Console.WriteLine("    Пример: Create mydb 30");
        Console.WriteLine("    Пример: Create mydb 30 myspec.prs");
        Console.WriteLine();
        Console.WriteLine("  Open имя_файла");
        Console.WriteLine("    Пример: Open mydb.prd");
        Console.WriteLine("    Пример: Open mydb");
        Console.WriteLine();
        Console.WriteLine("  Input имя тип - добавить компонент (тип: Изделие, Узел, Деталь)");
        Console.WriteLine("    Пример: Input Двигатель Изделие");
        Console.WriteLine("    Пример: Input \"Коленчатый вал\" Узел");
        Console.WriteLine();
        Console.WriteLine("  Input родитель/ребенок [количество] - добавить связь");
        Console.WriteLine("    Пример: Input Двигатель/Поршень");
        Console.WriteLine("    Пример: Input Двигатель/Поршень 4");
        Console.WriteLine();
        Console.WriteLine("  Delete имя_компонента - удалить компонент");
        Console.WriteLine("    Пример: Delete Двигатель");
        Console.WriteLine();
        Console.WriteLine("  Delete родитель/ребенок - удалить связь");
        Console.WriteLine("    Пример: Delete Двигатель/Поршень");
        Console.WriteLine();
        Console.WriteLine("  Restore имя_компонента - восстановить компонент");
        Console.WriteLine("  Restore * - восстановить все");
        Console.WriteLine("    Пример: Restore Двигатель");
        Console.WriteLine("    Пример: Restore *");
        Console.WriteLine();
        Console.WriteLine("  Truncate - физически удалить помеченные записи");
        Console.WriteLine();
        Console.WriteLine("  Print * - список всех компонентов");
        Console.WriteLine("  Print имя_компонента - дерево спецификации");
        Console.WriteLine("    Пример: Print *");
        Console.WriteLine("    Пример: Print Двигатель");
        Console.WriteLine();
        Console.WriteLine("  Help [файл] - показать справку или сохранить в файл");
        Console.WriteLine("    Пример: Help");
        Console.WriteLine("    Пример: Help help.txt");
        Console.WriteLine();
        Console.WriteLine("  Exit - выход");
        Console.WriteLine();
        Console.WriteLine("Примечания:");
        Console.WriteLine("  - Если имя содержит пробелы, заключайте его в кавычки");
        Console.WriteLine("  - Расширения .prd и .prs добавляются автоматически при необходимости");
        Console.WriteLine("  - Кратность связи по умолчанию = 1");
    }

    //  Вспомогательные методы для работы с указателями 
    private int GetFirstProd() { _prodFs!.Seek(4, SeekOrigin.Begin); return ReadInt(_prodFs); }
    private void SetFirstProd(int v) { _prodFs!.Seek(4, SeekOrigin.Begin); WriteInt(_prodFs, v); }
    private int GetFreeProd() { _prodFs!.Seek(8, SeekOrigin.Begin); return ReadInt(_prodFs); }
    private void UpdateFreeProd(int v) { _prodFs!.Seek(8, SeekOrigin.Begin); WriteInt(_prodFs, v); }
    private int GetFreeSpec() { _specFs!.Seek(4, SeekOrigin.Begin); return ReadInt(_specFs); }
    private void UpdateFreeSpec(int v) { _specFs!.Seek(4, SeekOrigin.Begin); WriteInt(_specFs, v); }
    private int ReadInt(FileStream fs) { byte[] b = new byte[4]; fs.Read(b, 0, 4); return BitConverter.ToInt32(b, 0); }
    private void WriteInt(FileStream fs, int v) { fs.Write(BitConverter.GetBytes(v), 0, 4); }
    //  Поиск узла по имени 
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
    //  Проверка наличия ссылок на компонент в спецификациях 
    private bool HasReferences(int prodOffset)
    {
        int currComp = GetFirstProd();
        while (currComp != -1)
        {
            var comp = new ProdNodeHelper(_prodFs!, currComp, _nameSize);
            if (comp.CanBeDel == 0) // только активные родители
            {
                int currSpec = comp.SpecNodePtr;
                while (currSpec != -1)
                {
                    var spec = new SpecNodeHelper(_specFs!, currSpec);
                    if (spec.CanBeDel == 0 && spec.ProdNodePtr == prodOffset)
                        return true;
                    currSpec = spec.NextNodePtr;
                }
            }
            currComp = comp.NextNodePtr;
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
    private bool IsComponentUsedAsChild(int componentOffset)
    {
        int currComp = GetFirstProd();
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
    // Проверка, является ли potentialAncestor предком node
    private bool IsAncestor(int potentialAncestorOffset, int nodeOffset)
    {
        var node = new ProdNodeHelper(_prodFs!, nodeOffset, _nameSize);
        if (node.SpecNodePtr == -1) return false;
        int currSpec = node.SpecNodePtr;
        while (currSpec != -1)
        {
            var spec = new SpecNodeHelper(_specFs!, currSpec);
            if (spec.CanBeDel == 0)
            {
                int childOffset = spec.ProdNodePtr;
                if (childOffset == potentialAncestorOffset)
                    return true;
                if (IsAncestor(potentialAncestorOffset, childOffset))
                    return true;
            }
            currSpec = spec.NextNodePtr;
        }
        return false;
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
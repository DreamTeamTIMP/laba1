using laba1New.Helpers;
using System.Text;

public class DataManager : IDisposable
{
    private FileStream? _prodFs;
    private FileStream? _specFs;
    private ushort _nameSize;

    // --- CREATE ---
    public void Create(string prodName, ushort dataSize, string? specName = null)
    {
        specName ??= prodName + ".prs";
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

    // --- INPUT (Тип: Изделие, Узел, Деталь) ---
    public void AddComponent(string name, string type)
    {
        int offset = GetFreeProd();
        var node = new ProdNodeHelper(_prodFs!, offset, _nameSize);

        node.CanBeDel = 0;
        node.Name = name;
        node.NextNodePtr = GetFirstProd();

        // Если Изделие или Узел - резервируем заголовок в .prs
        // (ТЗ говорит: запись с изделием может порождать новый список)
        if (type.ToLower() == "деталь")
        {
            node.SpecNodePtr = -1;
        }
        else
        {
            // Для Изделия/Узла создаем пустую "голову" спецификации
            node.SpecNodePtr = -1; // Пока компонентов нет, но тип мы запомним в коде
        }

        SetFirstProd(offset);
        UpdateFreeProd(offset + node.TotalSize);
    }

    // --- INPUT (Связь: Родитель/Ребенок) ---
    public void AddRelation(string parentName, string childName, ushort count = 1)
    {
        var p = FindNode(parentName);
        var c = FindNode(childName);

        if (p == null || c == null) throw new Exception("Компонент не найден");
        if (p.SpecNodePtr == -1 && !IsNodeOrProduct(p))
            throw new Exception("Деталь не может иметь спецификацию!");

        int newSpecOff = GetFreeSpec();
        var newEntry = new SpecNodeHelper(_specFs!, newSpecOff);

        newEntry.CanBeDel = 0;
        newEntry.ProdNodePtr = c.Offset;
        newEntry.Mentions = count;
        newEntry.NextNodePtr = p.SpecNodePtr; // Вставляем в начало списка

        p.SpecNodePtr = newSpecOff; // Обновляем голову в .prd
        UpdateFreeSpec(newSpecOff + 11);
    }

    // --- DELETE (Логическое) ---
    public void DeleteComponent(string name)
    {
        var node = FindNode(name);
        if (node == null) return;

        // Проверка: нет ли ссылок на него в других спецификациях (требование ТЗ)
        if (HasReferences(node.Offset))
            throw new Exception("Ошибка: на компонент есть ссылки в спецификациях!");

        node.CanBeDel = -1;
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
                string type = n.SpecNodePtr == -1 ? "Деталь" : "Узел/Изделие";
                Console.WriteLine($"{n.Name,-16} | {type}");
            }
            curr = n.NextNodePtr;
        }
    }
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
        int currentEntry = specOffset; // В ТЗ спецификация — это просто список

        while (currentEntry != -1)
        {
            spec.SetOffset(currentEntry);
            if (spec.CanBeDel == 0)
            {
                var component = new ProdNodeHelper(_prodFs!, spec.ProdNodePtr, _nameSize);

                // Рисуем иерархию
                string indent = new string('|', level).Replace("|", "|   ");
                Console.WriteLine($"{indent}|");
                Console.WriteLine($"{indent}┗━ {component.Name} (x{spec.Mentions})");

                // Если это не деталь, идем глубже
                if (component.SpecNodePtr != -1)
                {
                    PrintRecursive(component.SpecNodePtr, level + 1);
                }
            }
            currentEntry = spec.NextNodePtr;
        }
    }
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
    public void RestoreAll()
    {
        // 1. Снимаем бит удаления везде
        int curr = 28; // Начало данных в .prd
        var allNodes = new List<(int offset, string name)>();
        int freeSpace = GetFreeProd();

        while (curr < freeSpace)
        {
            var node = new ProdNodeHelper(_prodFs!, curr, _nameSize);
            node.CanBeDel = 0; // Снимаем пометку
            allNodes.Add((curr, node.Name));
            curr += node.TotalSize;
        }

        // 2. Сортируем список по алфавиту имен
        allNodes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

        // 3. Перестраиваем связи NextNodePtr
        if (allNodes.Count > 0)
        {
            SetFirstProd(allNodes[0].offset); // Новый корень списка
            for (int i = 0; i < allNodes.Count; i++)
            {
                var node = new ProdNodeHelper(_prodFs!, allNodes[i].offset, _nameSize);
                node.NextNodePtr = (i < allNodes.Count - 1) ? allNodes[i + 1].offset : -1;
            }
        }

        Console.WriteLine("Все записи восстановлены и отсортированы.");
    }
    // Вспомогательные методы чтения указателей из заголовков
    private int GetFirstProd() { _prodFs!.Seek(4, SeekOrigin.Begin); return ReadInt(_prodFs); }
    private void SetFirstProd(int v) { _prodFs!.Seek(4, SeekOrigin.Begin); WriteInt(_prodFs, v); }
    private int GetFreeProd() { _prodFs!.Seek(8, SeekOrigin.Begin); return ReadInt(_prodFs); }
    private void UpdateFreeProd(int v) { _prodFs!.Seek(8, SeekOrigin.Begin); WriteInt(_prodFs, v); }

    private int GetFreeSpec() { _specFs!.Seek(4, SeekOrigin.Begin); return ReadInt(_specFs); }
    private void UpdateFreeSpec(int v) { _specFs!.Seek(4, SeekOrigin.Begin); WriteInt(_specFs, v); }

    private int ReadInt(FileStream fs) { byte[] b = new byte[4]; fs.Read(b, 0, 4); return BitConverter.ToInt32(b, 0); }
    private void WriteInt(FileStream fs, int v) { fs.Write(BitConverter.GetBytes(v), 0, 4); }

    private bool IsNodeOrProduct(ProdNodeHelper n) => n.SpecNodePtr != -1;

    private bool HasReferences(int prodOffset)
    {
        // Проход по всему .prs в поисках ProdNodePtr == prodOffset
        // Реализуется простым перебором файла .prs от заголовка до FreeSpace
        return false; // Заглушка: нужно реализовать цикл по записям .prs
    }

    public void Dispose() { _prodFs?.Close(); _specFs?.Close(); }
}
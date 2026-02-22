using System.Buffers.Binary;
using laba1New.Helpers;

namespace laba1New;

// Насчёт Resize использовать его если и придётся то нужно только через особый метод так как нужно будет обновить prodHeader и specHeader

public class DataManager // : IDisposable позже реализуем 
{
    private ProdHeaderHelper? prodHeader;
    private SpecHeaderHelper? specHeader;

    private byte[] _prodData = [];
    private byte[] _specData = [];
    private string _prodPath = "";
    private string _specPath = "";

    public void Create(string name, ushort dataSize = 16, string? specName = null)
    {
        specName ??= name;
        _prodPath = name + ".prd";
        _specPath = specName + ".prs";

        _prodData = new byte[1024 * 32];
        _specData = new byte[1024 * 32];

        prodHeader = new(_prodData, _specData);
        specHeader = new(_prodData, _specData);

        prodHeader.Signature = "PS"u8;
        prodHeader.FreeSpacePtr = ProdHeaderOffset.TotalOffset;
        prodHeader.FirstNodePtr = -1;
        prodHeader.CompDataSize = dataSize;
        prodHeader.SpecFileName = name;

        specHeader.Signature = "PRS"u8;
        specHeader.FirstNodePtr = -1;
        specHeader.FreeSpacePtr = SpecHeaderOffset.TotalOffset;

        Save();
    }

    public void Save()
    {
        File.WriteAllBytes(_prodPath, _prodData);
        File.WriteAllBytes(_specPath, _specData);
    }

    public void AddProduct(string name)
    {
        if (prodHeader is null || specHeader is null)
            throw new InvalidOperationException("You must first create or open file");

        if (FindProduct(name) is not null)
            throw new ArgumentException("Component with this name already exist!");

        var newNode = prodHeader.NewNode(name);
        specHeader.NewSpec(newNode);
        
        Save();    
    }

    public void AddDetail(string name)
    {
        if (prodHeader is null)
            throw new InvalidOperationException("You must first create or open file");
        
        if (FindProduct(name) is not null)
            throw new ArgumentException("Component with this name already exist!");

        prodHeader.NewNode(name);

        Save();
    }

    //public void AddProduct(string name)
    //{
    //    int currentFree = ProdHeaderData.FreeSpacePtr(_prodData);
    //    int recordSize = ProdNodeOffset.TotalOffset(_prodData);
    //    
    //    Array.Resize(ref _prodData, currentFree + recordSize);
//
    //    var helper = new ProdNodeHelper(_prodData, _specData);
    //    helper.SetOffset(currentFree);
//
    //    helper.CanBeDel = 0;
    //    helper.SpecNodePtr = -1;
    //    int oldHead = BinaryPrimitives.ReadInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr));
    //    helper.NextNodePtr = oldHead;
//
    //    var nameBytes = System.Text.Encoding.Default.GetBytes(name);
    //    helper.Data = nameBytes;
//
    //    BinaryPrimitives.WriteInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr), currentFree);
    //    BinaryPrimitives.WriteInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FreeSpacePtr), currentFree + recordSize);
//
    //    Save();
    //}

    public void AddToSpec(string prodName, string compName, ushort mentions)
    {
        if (specHeader is null)
            throw new InvalidOperationException("You must first create or open file");

        if(prodName == compName)
            throw new ArgumentException("You can't add a component in its own specification");

        var prod = FindProduct(prodName);
        if (prod is null)
            throw new ArgumentException($"Product with {prodName} name doesn't exist");

        var comp = FindProduct(compName);
        if (comp is null) 
            throw new ArgumentException($"Component with {compName} name doesn't exist");
        
        
        var specList = prod.Spec;
        if (specList is null)
            throw new ArgumentException("Detail can't have specification!");
        

        while (true)
        {
            if (specList.ProdNodePtr == comp.offset)
                throw new ArgumentException("Product already have this entry in specificication!");

            if (specList.Next != null)
                specList = specList.Next;
            else
                break;
        }

        specHeader.NewSpecRecord(specList, comp, mentions);
    }

    public void AddRelation(string ownerName, string partName, ushort count)
    {
        // 1. Найти оффсеты владельца и детали (перебором по NextNodePtr)
        int ownerOffset = FindProduct(ownerName)?.offset ?? -1;
        int partOffset = FindProduct(partName)?.offset ?? -1;

        // 2. Создать новую запись в SpecData
        int specFree = SpecHeaderData.FreeSpacePtr(_specData);
        Array.Resize(ref _specData, specFree + SpecNodeOffset.TotalOffset);

        var specHelper = new SpecNodeHelper(_prodData, _specData);
        specHelper.SetOffset(specFree);

        var prodHelper = new ProdNodeHelper(_prodData, _specData)[ownerOffset];

        // 3. Настройка связей
        specHelper.CanBeDel = 0;
        specHelper.ProdNodePtr = partOffset;
        specHelper.Mentions = count;
        specHelper.NextNodePtr = prodHelper.SpecNodePtr; // В начало списка спецификаций изделия

        prodHelper.SpecNodePtr = specFree; // Узел теперь ссылается на новую запись в .prs

        // 4. Обновить заголовок .prs
        BinaryPrimitives.WriteInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FreeSpacePtr), specFree + SpecNodeOffset.TotalOffset);

        Save();
    }

    public void PrintAll()
    {
        if (prodHeader is null || specHeader is null)
            throw new InvalidOperationException("You must first create or open file");
            

        Console.WriteLine($"{"", 15}{_prodPath}:");
        Console.WriteLine($"{"DataSpaceSize:", -30}{prodHeader.CompDataSize}B");
        Console.WriteLine($"{"SpecFileName:", -30}{prodHeader.SpecFileName}");
        Console.WriteLine($"{"", 15}Products:");
        Console.WriteLine($"{"Product name", -30}Product Type");

        for (var prod = prodHeader.FirstNode; prod is not null; prod = prod.Next)
        {
            if (prod.Spec is not null)
                Console.WriteLine($"{prod.DataAsString, -30}Node");
            else
                Console.WriteLine($"{prod.DataAsString, -30}Detail");
        }

        Console.WriteLine("");

        Console.WriteLine($"{"", 15}{_specPath}:");
        Console.WriteLine($"{"", 15}All spec records:");
        Console.WriteLine($"{"Spec", -30}Mentions");

        for (var spec = specHeader.FirstNode; spec is not null && spec.offset < specHeader.FreeSpacePtr; spec.SetOffset(spec.offset + specHeader.NodeSize))
        {
            if (spec.CanBeDel == 0)
                Console.WriteLine($"{spec.Prod.DataAsString, -30}{spec.Mentions}");
        }
        
        Console.WriteLine("");
        
        Console.WriteLine($"{"", 15}Specs:");

        for (var spec = specHeader.FirstNode; spec is not null && spec.offset < specHeader.FreeSpacePtr; spec.SetOffset(spec.offset + specHeader.NodeSize))
        {
            if (spec.Mentions != 0)
                continue;

            Console.WriteLine($"{"", 15}{spec.Prod.DataAsString} spec:");
            Console.WriteLine($"{"Spec", -30}Mentions");

            spec.Print(0);
        }
    }

    public void PrintTree(string name)
    {
        int offset = FindProduct(name)?.offset ?? -1;
        if (offset == -1) return;

        PrintRecursive(offset, 0);
    }

    private void PrintRecursive(int prodOffset, int level)
    {
        var prod = new ProdNodeHelper(_prodData, _specData)[prodOffset];
        Console.WriteLine(new string(' ', level * 4) + System.Text.Encoding.Default.GetString(prod.Data.ToArray()).Trim());

        // Идем по цепочке спецификации
        var currentSpec = prod.Spec;
        while (currentSpec != null)
        {
            PrintRecursive(currentSpec.ProdNodePtr, level + 1);
            currentSpec = currentSpec.Next;
        }
    }

    private ProdNodeHelper? FindProduct(string NodeName)
    {
        for (var node = prodHeader?.FirstNode; node is not null; node = node.Next)
        {
            if (node.DataAsString == NodeName)
                return node;
        }

        return null;
    }
}
using System.Text;

namespace laba1;

public class ListRoot
{
    private byte[] _prodFileData = Encoding.UTF8.GetBytes("products.ps");
    private byte[] _specFileData = Encoding.UTF8.GetBytes("products.prs");
    public string ProdFileName 
    {
        get => Encoding.UTF8.GetString(_prodFileData);
        set 
        {
            if (value is not null)
            {
                byte[] temp = Encoding.UTF8.GetBytes(value);
                if (temp.Length > 16)
                    throw new InvalidDataException("Products file name to long!");
                _prodFileData = temp;                    
            }
            else
            {
                throw new InvalidDataException("Products file name is empty!");
            }
        }
    }

    public string SpecFileName 
    {
        get => Encoding.UTF8.GetString(_specFileData);
        set 
        {
            if (value is not null)
            {
                byte[] temp = Encoding.UTF8.GetBytes(value);
                if (temp.Length > 16)
                    throw new InvalidDataException("Specifications file name to long!");
                _specFileData = temp;                    
            }
            else
            {
                throw new InvalidDataException("Specifications file name is empty!");
            }
        }
    }

    public short DateSpaceSize = 16; // В байтах

    //private ProdListNode? lastProd = null;
    //private SpecListNode? lastSpec = null;
    
    private List<ProdListNode> ProdList = [];
    private List<SpecListNode> SpecList = [];
    
    public void AddNode(ushort mentions, byte[] data)
    {
        if (ProdList.Find(prod => prod.componentData == data) is not null)
            throw new Exception("Component with this name already exist!");

        ProdListNode node = new(data);
        SpecListNode spec = new(node, mentions); 
        node.Spec = spec;
        SpecList.Add(spec);

        AddComponent(node);
    }

    public void AddDetail(byte[] data)
    {
        if (ProdList.Find(prod => prod.componentData == data) is not null)
            throw new Exception("Component with this name already exist!");

        ProdListNode detail = new(data);
        
        AddComponent(detail);
    }

    private void AddComponent(ProdListNode component)
    {
        var lastElem = ProdList.LastOrDefault();
        if (lastElem is not null)
            lastElem.Next = component;

        ProdList.Add(component);
    }

    //private void NewSpec()

    public void AddToSpec(ProdListNode prod, ProdListNode component, ushort mentions)
    {
        if (prod.Spec is null)
            throw new ArgumentException("Detail can't have specification!");            

        // Надеюсь это сравнение ссылок хотя по идеи это не критично
        for (var i = prod.Spec; i is not null; i = i.Next)
            if (i.Prod == component)
                throw new Exception("Product already have this entry in specificication!");

        SpecListNode newRecord = new(component, mentions); 
        var lastRecord = prod.Spec.GetLastElementInSequence();
        lastRecord.Next = newRecord;
        SpecList.Add(newRecord);
    }

    public void PrintAll()
    {
        Console.WriteLine($"{"", 15}{ProdFileName}:");
        Console.WriteLine($"{"DateSpaceSize:", -30}{DateSpaceSize}B");
        Console.WriteLine($"{"SpecFileName:", -30}{SpecFileName}");
        Console.WriteLine($"{"", 15}Products:");
        Console.WriteLine($"{"Product name", -30}Product Type");
        foreach(var prod in ProdList)
        {
            if (prod.Spec is not null)
                Console.WriteLine($"{prod.componentData, -30}Node");
            else
                Console.WriteLine($"{prod.componentData, -30}Detail");
        }

        Console.WriteLine("");

        Console.WriteLine($"{"", 15}{SpecFileName}:");
        Console.WriteLine($"{"", 15}Specs:");
        
        int i = 0;
        foreach(var spec in SpecList)
        {
            if(i == 0)
            {
                Console.WriteLine($"{"", 15}{spec.Prod.componentData} spec:");
                Console.WriteLine($"{"Spec", -30} Mentions");
            }
            else
                Console.WriteLine($"{spec.Prod.componentData, -30}{spec.Mentions}");

            if(spec.Next is null) 
                i = 0;
        }
    }
}

public class ProdListNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    private sbyte CanBeDeleted = 0;
    public ProdListNode? Next = null;
    public SpecListNode? Spec = null; // У деталей нет спецификации => null  

    public byte[] componentData = []; // Например для DataSpaceSize 16 байт "abcdefqw123\0\0\0\0\0"u8

    public ProdListNode(SpecListNode? spec = null, byte[]? data = null)
    {
        if(data is null)
            componentData = "default"u8.ToArray();
        Spec = spec;
    }

    public ProdListNode(byte[] data)
    {
        componentData = data;
    }
    
    public void AddToEnd(ProdListNode prod)
    {
        var end = this;
        for (; end.Next is not null; end = end.Next);
        end.Next = prod;
    }
}

public class SpecListNode
{
    public sbyte CanBeDeleted = 0;
    public SpecListNode? Next = null;
    public ProdListNode Prod;
    public ushort Mentions = 1;

    public SpecListNode(ProdListNode prod, ushort mentions = 1)
    {
        Mentions = mentions;
        Prod = prod;
    }

    public SpecListNode GetLastElementInSequence()
    {
        var i = this;
        while (i.Next is not null)
            i = i.Next;
        return i;
    }
}

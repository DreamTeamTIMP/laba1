using System.Text;

namespace laba1;

public class ListRoot
{
    private byte[] _prodFileData = new byte[16]; //= Encoding.UTF8.GetBytes("products.ps");
    private byte[] _specFileData = new byte[16]; //= Encoding.UTF8.GetBytes("products.prs");
    public string ProdFileName
    {
        get => Encoding.UTF8.GetString(_prodFileData);
        set 
        {
            if (value is not null)
            {
                StringToData(value, _prodFileData);
            }
            else
            {
                throw new ArgumentException("Products file name is empty!");
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
                StringToData(value, _specFileData);
            }
            else
            {
                throw new ArgumentException("Specifications file name is empty!");
            }
        }
    }

    public short DataSpaceSize = 16; // В байтах

    private List<ProdListNode> ProdList = [];
    private List<SpecListNode> SpecList = [];

    private void StringToData(ReadOnlySpan<char> str, Span<byte> data)
    {
        if (!Encoding.UTF8.TryGetBytes(str, data, out int byteCount))
            throw new ArgumentException("Name to long!");

        data.Slice(byteCount).Fill(0);
    }
    
    public void AddNode(string name)
    {
        byte[] data = new byte[DataSpaceSize];
        StringToData(name, data);

        if (ProdList.Any(prod => prod.componentData.AsSpan().SequenceEqual(data.AsSpan())))
            throw new ArgumentException("Component with this name already exist!");

        ProdListNode node = new(data);
        
        SpecListNode spec = new(node);
        node.Spec = spec;
        SpecList.Add(spec);

        AddComponent(node);
    }

    public void AddDetail(string name)
    {   
        byte[] data = new byte[DataSpaceSize];
        StringToData(name, data);

        if (ProdList.Any(prod => prod.componentData.AsSpan().SequenceEqual(data.AsSpan())))
            throw new ArgumentException("Component with this name already exist!");

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

    public void AddToSpec(string prodName, string compName, ushort mentions)
    {
        if(prodName == compName)
            throw new ArgumentException("You can't add a component in its own specification");

        byte[] prodData = new byte[DataSpaceSize];
        StringToData(prodName, prodData);

        byte[] compData = new byte[DataSpaceSize];
        StringToData(compName, compData);

        var prod = ProdList.Find(p => p.componentData.AsSpan().SequenceEqual(prodData.AsSpan())) 
            ?? throw new ArgumentException($"Product with {prodName} name doesn't exist");
        var comp = ProdList.Find(p => p.componentData.AsSpan().SequenceEqual(compData.AsSpan()))
            ?? throw new ArgumentException($"Component with {compName} name doesn't exist");
        
        AddToSpec(prod, comp, mentions);
    }

    public void AddToSpec(ProdListNode prod, ProdListNode component, ushort mentions)
    {
        if (prod.Spec is null)
            throw new ArgumentException("Detail can't have specification!");            

        for (var i = prod.Spec; i is not null; i = i.Next)
            if (i.Prod == component)
                throw new ArgumentException("Product already have this entry in specificication!");

        SpecListNode newRecord = new(component, mentions); 
        var lastRecord = prod.Spec.GetLastElementInSequence();
        lastRecord.Next = newRecord;
        SpecList.Add(newRecord);
    }

    public void PrintAll()
    {
        Console.WriteLine($"{"", 15}{ProdFileName}:");
        Console.WriteLine($"{"DataSpaceSize:", -30}{DataSpaceSize}B");
        Console.WriteLine($"{"SpecFileName:", -30}{SpecFileName}");
        Console.WriteLine($"{"", 15}Products:");
        Console.WriteLine($"{"Product name", -30}Product Type");
        foreach(var prod in ProdList)
        {
            if (prod.Spec is not null)
                Console.WriteLine($"{prod, -30}Node");
            else
                Console.WriteLine($"{prod, -30}Detail");
        }

        Console.WriteLine("");

        Console.WriteLine($"{"", 15}{SpecFileName}:");
        Console.WriteLine($"{"", 15}All spec records:");
        Console.WriteLine($"{"Spec", -30}Mentions");
        
        foreach(var spec in SpecList)
        {
            Console.WriteLine($"{spec.Prod, -30}{spec.Mentions}");
        }

        Console.WriteLine("");
        
        Console.WriteLine($"{"", 15}Specs:");

        foreach(var spec in SpecList)
        {
            if (spec.Mentions != 0)
                continue;
            Console.WriteLine($"{"", 15}{spec.Prod}spec:");
            Console.WriteLine($"{"Spec", -30}Mentions");

            spec.Print(0);
        }
    }
}

public class ProdListNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    private sbyte CanBeDeleted = 0;
    public ProdListNode? Next = null;
    public SpecListNode? Spec = null; // У деталей нет спецификации => null  

    public byte[] componentData; // Например для DataSpaceSize 16 байт "abcdefqw123\0\0\0\0\0"u8

    public ProdListNode(SpecListNode? spec, byte[] data)
    {
        componentData = data;
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

    public override string ToString() => Encoding.UTF8.GetString(componentData).Trim('\0', ' ');
}

public class SpecListNode
{
    public sbyte CanBeDeleted = 0;
    public SpecListNode? Next = null;
    public ProdListNode Prod;
    public ushort Mentions = 0;

    public SpecListNode(ProdListNode prod, ushort mentions = 0)
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

    public void Print(int offset)
    {
        char[] spacing = new char[offset];
        var span = spacing.AsSpan();
        span.Fill(' ');

        var i = this.Next;       
        while(i is not null)
        {
            Console.Write(spacing);
            Console.WriteLine($"{i.Prod, -30}{i.Mentions}");
            
            i.Prod.Spec?.Print(offset + 2);

            i = i.Next;
        }
    }
}

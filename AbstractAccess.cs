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

    public List<ProdListNode> ProdList { get; private set; } = [];
    public List<SpecListNode> SpecList { get; private set; } = [];

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

        ProdListNode node = new(this, data);
        AddComponent(node);
        
        SpecListNode spec = new(this, ProdList.Count - 1);
        SpecList.Add(spec);

        node.SpecIndex = SpecList.Count - 1;
    }

    public void AddDetail(string name)
    {   
        byte[] data = new byte[DataSpaceSize];
        StringToData(name, data);

        if (ProdList.Any(prod => prod.componentData.AsSpan().SequenceEqual(data.AsSpan())))
            throw new ArgumentException("Component with this name already exist!");

        ProdListNode detail = new(this, data);
        
        AddComponent(detail);
    }

    private void AddComponent(ProdListNode component)
    {
        var lastIndex = ProdList.Count;
        ProdList.Add(component);

        if (lastIndex != 0)
            ProdList[lastIndex - 1].NextIndex = lastIndex;
    }

    public void AddToSpec(string prodName, string compName, ushort mentions)
    {
        if(prodName == compName)
            throw new ArgumentException("You can't add a component in its own specification");

        byte[] prodData = new byte[DataSpaceSize];
        StringToData(prodName, prodData);

        byte[] compData = new byte[DataSpaceSize];
        StringToData(compName, compData);

        var prodIndex = ProdList.FindIndex(p => p.componentData.AsSpan().SequenceEqual(prodData.AsSpan()));
        if (prodIndex == -1) 
            throw new ArgumentException($"Product with {prodName} name doesn't exist");
        var compIndex = ProdList.FindIndex(p => p.componentData.AsSpan().SequenceEqual(compData.AsSpan()));
        if (compIndex == -1) 
            throw new ArgumentException($"Component with {compName} name doesn't exist");
        
        var prod = ProdList[prodIndex];
        var comp = ProdList[compIndex];

        if (prod.SpecIndex == -1)
            throw new ArgumentException("Detail can't have specification!");            

        for (var i = prod.Spec; i is not null; i = i.Next)
            if (i.ProdIndex == compIndex)
                throw new ArgumentException("Product already have this entry in specificication!");

        SpecListNode newRecord = new(this, compIndex, mentions); 
        SpecList.Add(newRecord);
        
        //Проверка в if. LSP не понимает, что это одно и тоже
        var lastRecord = prod.Spec!.GetLastElementInSequence();
        lastRecord.Next = newRecord;
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
    private readonly ListRoot _root; 
    private int _nextIndex = -1;
    private int _specIndex = -1; // У деталей нет спецификации => null  
    public ProdListNode? Next 
    {
        get
        {
            if (_nextIndex == -1)
                return null;
            return _root.ProdList[_nextIndex];
        }
        set 
        {
            if (value is null)
            {
                _nextIndex = -1;
            }
            else
            {
                var temp = _root.ProdList.IndexOf(value);
                if (temp == -1)
                    throw new InvalidOperationException("The node does not belong to the current list.");
                _nextIndex = temp;
            }
        }   
    }

    public int NextIndex
    {
        get => _nextIndex;
        set
        {
            if(value < -1 || value > _root.ProdList.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Index must be between -1 and {_root.ProdList.Count - 1}\nOld index: {_nextIndex}");
            _nextIndex = value;
        }   
    }

    public SpecListNode? Spec 
    {
        get
        {
            if (_specIndex == -1)
                return null;
            return _root.SpecList[_specIndex];
        }
        set
        {
            if (value is null)
            {
                _specIndex = -1;
            }
            else
            {
                _specIndex = _root.SpecList.IndexOf(value);
            }
        }   
    }

    public int SpecIndex
    {
        get => _specIndex;
        set
        {
            if(value < -1 || value > _root.SpecList.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Index must be between -1 and {_root.SpecList.Count - 1}\nOld index: {_specIndex}");
            _specIndex = value;
        }
    }

    public byte[] componentData; // Например для DataSpaceSize 16 байт "abcdefqw123\0\0\0\0\0"u8

    public ProdListNode(ListRoot root, SpecListNode? spec, byte[] data)
    {
        _root = root;
        componentData = data;
        Spec = spec;
    }

    public ProdListNode(ListRoot root, int specIndex, byte[] data)
    {
        _root = root;
        componentData = data;
        SpecIndex = specIndex;
    }

    public ProdListNode(ListRoot root, byte[] data)
    {
        _root = root;
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
    private readonly ListRoot _root;
    public sbyte CanBeDeleted = 0;
    public int _nextIndex = -1;
    public int _prodIndex;
    public SpecListNode? Next 
    {
        get
        {
            if (_nextIndex == -1)
                return null;
            return _root.SpecList[_nextIndex];
        }
        set 
        {
            if (value is null)
            {
                _nextIndex = -1;
            }
            else
            {
                var temp = _root.SpecList.IndexOf(value);
                if (temp == -1)
                    throw new InvalidOperationException("The node does not belong to the current list.");
                _nextIndex = temp;
            }
        }   
    }

    public int NextIndex
    {
        get => _nextIndex;
        set
        {
            if(value < -1 || value > _root.SpecList.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Index must be between -1 and {_root.SpecList.Count - 1}\nOld index: {_nextIndex}");
            _nextIndex = value;
        }   
    }

    public ProdListNode Prod 
    {
        get
        {
            return _root.ProdList[_prodIndex];
        }
        init
        {
            var temp = _root.ProdList.IndexOf(value);
            if (temp == -1)
                throw new InvalidOperationException("The node does not belong to the current list.");
            _prodIndex = temp;
        }
    }

    public int ProdIndex
    {
        get => _prodIndex;
        set
        {
            if(value < -1 || value > _root.ProdList.Count - 1)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Index must be between -1 and {_root.ProdList.Count - 1}\nOld index: {_prodIndex}");
            _prodIndex = value;
        }
    }

    public ushort Mentions = 0;

    public SpecListNode(ListRoot root, ProdListNode prod, ushort mentions = 0)
    {
        _root = root;
        Mentions = mentions;
        Prod = prod;
    }

    public SpecListNode(ListRoot root, int prod, ushort mentions = 0)
    {
        _root = root;
        Mentions = mentions;
        ProdIndex = prod;
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
        string spacing = new('-', offset);

        var i = this.Next;       
        while(i is not null)
        {
            var str = spacing + i.Prod.ToString();
            Console.WriteLine($"{str, -30}{i.Mentions}");
            
            i.Prod.Spec?.Print(offset + 2);

            i = i.Next;
        }
    }
}

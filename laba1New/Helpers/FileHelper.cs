using System.Buffers.Binary;

namespace laba1New.Helpers;

public class ProdNodeHelper(byte[] rawProdFileData, byte[] rawSpecFileData)
{
    readonly private byte[] RawProdFileData = rawProdFileData;
    readonly private byte[] RawSpecFileData = rawSpecFileData;
    public int offset { get; private set; } = 0;

    private int DataSpaceSize => ProdHeaderData.DataSpaceSize(RawProdFileData);

    public void ValidateSpecPtr(int ptr) => PointerHelper.ValidateSpecPtr(ptr, RawSpecFileData);
    public void ValidateProdPtr(int ptr) => PointerHelper.ValidateProdPtr(ptr, RawProdFileData);

    public ProdNodeHelper SetOffset(int _offset)
    {
        if (_offset == -1)
            throw new ArgumentNullException("Null pointer access. Can't represent node with -1 address");
        
        ValidateProdPtr(_offset);

        offset = _offset;
        return this;
    }

    public ProdNodeHelper this [int _offset]
    {
        get => SetOffset(_offset);
    }

    public sbyte CanBeDel
    {
        get 
        {
            return (sbyte)RawProdFileData[offset + ProdNodeOffset.CanBeDel];
        }
        set 
        {
            RawProdFileData[offset + ProdNodeOffset.CanBeDel] = (byte)value;
        }
    }

    public int SpecNodePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.SpecNodePtr));
        }
        set
        {
            ValidateSpecPtr(value);

            BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.SpecNodePtr), value);
        }
    }

    public SpecNodeHelper? Spec
    {
        get
        {
            if (SpecNodePtr == -1)
                return null;
            return new SpecNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(SpecNodePtr);
        }
    }

    public int NextNodePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.NextNodePtr));
        }
        set
        {
            ValidateProdPtr(value);
                
            BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData.AsSpan(offset + ProdNodeOffset.NextNodePtr), value);
        }
    }

    public ProdNodeHelper? Next
    {
        get
        {
            if (NextNodePtr == -1)
                return null;
            return new ProdNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(NextNodePtr);
        }
    }

    public ReadOnlySpan<byte> Data
    {
        get
        {
            return RawProdFileData.AsSpan(offset + ProdNodeOffset.Data, DataSpaceSize);
        }
        set
        {
            StringHelper.WriteData(value, RawProdFileData.AsSpan(offset + ProdNodeOffset.Data, DataSpaceSize));
        }
    }

    public string DataAsString
    {
        get
        {
            return StringHelper.DataToString(Data);
        }
        set
        {
            StringHelper.StringToData(value, RawProdFileData.AsSpan(offset + ProdNodeOffset.Data, DataSpaceSize));
        }
    }
}

public class SpecNodeHelper(byte[] rawProdFileData, byte[] rawSpecFileData)
{
    readonly private byte[] RawProdFileData = rawProdFileData;
    readonly private byte[] RawSpecFileData = rawSpecFileData;
    public int offset { get; private set; } = 0;

    public void ValidateSpecPtr(int ptr) => PointerHelper.ValidateSpecPtr(ptr, RawSpecFileData);
    public void ValidateProdPtr(int ptr) => PointerHelper.ValidateProdPtr(ptr, RawProdFileData);

    public SpecNodeHelper SetOffset(int _offset)
    {
        if (_offset == -1)
            throw new ArgumentNullException("Null pointer access. Can't represent node with -1 address");
        
        ValidateSpecPtr(_offset);

        offset = _offset;
        return this;
    }

    public SpecNodeHelper this [int _offset]
    {
        get => SetOffset(_offset);
    }

    public sbyte CanBeDel
    {
        get 
        {
            return (sbyte)RawSpecFileData[offset + SpecNodeOffset.CanBeDel];
        }
        set 
        {
            RawSpecFileData[offset + SpecNodeOffset.CanBeDel] = (byte)value;
        }
    }

    public int ProdNodePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.ProdNodePtr));
        }
        set
        {
            if (ProdNodePtr == -1)
                throw new InvalidOperationException("Specification record can't exist without product node");
            
            ValidateProdPtr(value);

            BinaryPrimitives.WriteInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.ProdNodePtr), value);
        }
    }

    public ProdNodeHelper Prod
    {
        get
        {
            return new ProdNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(ProdNodePtr);
        }
    }

    public ushort Mentions
    {
        get
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.Mentions));
        }

        set
        {
            BinaryPrimitives.WriteUInt16LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.Mentions), value);
        }
    }

    public int NextNodePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.NextNodePtr));
        }
        set
        {
            ValidateSpecPtr(value);
                
            BinaryPrimitives.WriteInt32LittleEndian(RawSpecFileData.AsSpan(offset + SpecNodeOffset.NextNodePtr), value);
        }
    }

    public SpecNodeHelper? Next
    {
        get
        {
            if (NextNodePtr == -1)
                return null;
            return new SpecNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(NextNodePtr);
        }
    }

    public void Print(int offset)
    {
        string spacing = new('-', offset);

        var i = Next;       
        while(i is not null)
        {
            var str = spacing + i.Prod.DataAsString;
            Console.WriteLine($"{str, -30}{i.Mentions}");
            
            i.Prod.Spec?.Print(offset + 2);

            i = i.Next;
        }
    }
}

public class ProdHeaderHelper(byte[] RawProdFileData, byte[] RawSpecFileData)
{
    public void ValidateProdPtr(int ptr) => PointerHelper.ValidateProdPtr(ptr, RawProdFileData);

    public int NodeSize => ProdNodeOffset.TotalOffset(RawProdFileData);

    public ReadOnlySpan<byte> Signature 
    {
        get
        {
            return RawProdFileData.AsSpan(ProdHeaderOffset.Signature, ProdHeaderOffset.CompDataSize);
        }
        set 
        {
            if (!value.SequenceEqual("PS"u8))
                throw new ArgumentException("Signature of produts file can be only \"PS\" ");
            
            value.CopyTo(RawProdFileData.AsSpan(ProdHeaderOffset.Signature, ProdHeaderOffset.CompDataSize));
        }
    }

    public ushort CompDataSize
    {
        get
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(RawProdFileData.AsSpan(ProdHeaderOffset.CompDataSize));
        }
        set
        {
            if (FirstNodePtr != -1 && FreeSpacePtr != ProdHeaderOffset.TotalOffset)
                throw new InvalidOperationException("CompDataSize can be changed only if file is empty (FirstNodePtr must be -1)");
            BinaryPrimitives.WriteUInt16LittleEndian(RawProdFileData.AsSpan(ProdHeaderOffset.CompDataSize), value);
        }
    }

    public int FirstNodePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData.AsSpan(ProdHeaderOffset.FirstNodePtr));
        }
        set
        {
            if (FreeSpacePtr <= value)
                throw new InvalidOperationException("FirstNodePtr can't be bigget then FreeSpacePtr! If you are adding a node, change the value of FreeSpacePtr first.");

            ValidateProdPtr(value);

            BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData.AsSpan(ProdHeaderOffset.FirstNodePtr), value);
        }
    }

    public ProdNodeHelper? FirstNode
    {
        get
        {
            if (FirstNodePtr == -1)
                return null;
            return new ProdNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(FirstNodePtr);
        }
    }

    public int FreeSpacePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData.AsSpan(ProdHeaderOffset.FreeSpacePtr));
        }
        set
        {
            if (value <= FirstNodePtr)
                throw new InvalidOperationException($"FreeSpacePtr can't be lower then FirstNodePtr! If you are deleting a node, change the value of FirstNodePtr first.  {value}");
            if (value == -1)
                throw new ArgumentNullException("FreeSpacePtr can't be -1!");

            ValidateProdPtr(value);

            BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData.AsSpan(ProdHeaderOffset.FreeSpacePtr), value);
        }
    }

    private ProdNodeHelper FreeSpaceAsNode
    {
        get
        {
            return new ProdNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(FreeSpacePtr);
        }
    }

    public ProdNodeHelper? GetLastNode()
    {
        var node = FirstNode;
        if (node is null) 
            return null;
        
        while (node.Next is not null)
        {
            node = node.Next;
        }

        return node;
    }

    public ProdNodeHelper NewNode(string dataAsString)
    {
        var newNode = FreeSpaceAsNode;
        var lastNode = GetLastNode();

        var oldFreeSpacePtr = FreeSpacePtr;        
        
        FreeSpacePtr = FreeSpacePtr + NodeSize;
        
        if (lastNode is not null)
            lastNode.NextNodePtr = oldFreeSpacePtr;

        newNode.CanBeDel = 0;
        newNode.SpecNodePtr = -1;
        newNode.NextNodePtr = -1;
        newNode.DataAsString = dataAsString;

        if (FirstNodePtr == -1)
            FirstNodePtr = oldFreeSpacePtr;

        return newNode;
    }

    // Использовать с осторожностью. Возвращает, меняет имя поля В МАССИВЕ БАЙТОВ не более.
    public ReadOnlySpan<byte> SpecFileData
    {
        get
        {
            return RawProdFileData.AsSpan(ProdHeaderOffset.SpecFileName, ProdHeaderData.SpecFileNameLen);
        }
        set
        {
            StringHelper.WriteData(value, RawProdFileData.AsSpan(ProdHeaderOffset.SpecFileName, ProdHeaderData.SpecFileNameLen));
        }
    }

    public string SpecFileName
    {
        get
        {
            return StringHelper.DataToString(SpecFileData);
        }
        set
        {
            StringHelper.StringToData(value, RawProdFileData.AsSpan(ProdHeaderOffset.SpecFileName, ProdHeaderData.SpecFileNameLen));
        }
    }
}

public class SpecHeaderHelper(byte[] RawProdFileData, byte[] RawSpecFileData)
{
    public void ValidateSpecPtr(int ptr) => PointerHelper.ValidateSpecPtr(ptr, RawSpecFileData);
    public int NodeSize => SpecNodeOffset.TotalOffset;

    public ReadOnlySpan<byte> Signature 
    {
        get
        {
            return RawSpecFileData.AsSpan(SpecHeaderOffset.Signature, SpecHeaderOffset.FirstNodePtr);
        }
        set 
        {
            if (!value.SequenceEqual("PRS"u8))
                throw new ArgumentException("Signature of specifications file can be only \"PS\" ");
            
            value.CopyTo(RawSpecFileData.AsSpan(SpecHeaderOffset.Signature, SpecHeaderOffset.FirstNodePtr));
        }
    }

    public int FirstNodePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawSpecFileData.AsSpan(SpecHeaderOffset.FirstNodePtr));
        }
        set
        {
            if (value >= FreeSpacePtr)
                throw new InvalidOperationException($"FirstNodePtr can't be bigget then FreeSpacePtr! If you are adding a node, change the value of FreeSpacePtr first.  {value}");

            ValidateSpecPtr(value);

            BinaryPrimitives.WriteInt32LittleEndian(RawSpecFileData.AsSpan(SpecHeaderOffset.FirstNodePtr), value);
        }
    }

    public SpecNodeHelper? FirstNode
    {
        get
        {
            if (FirstNodePtr == -1)
                return null;
            return new SpecNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(FirstNodePtr);
        }
    }

    public int FreeSpacePtr
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawSpecFileData.AsSpan(SpecHeaderOffset.FreeSpacePtr));
        }
        set
        {
            if (FirstNodePtr >= value)
                throw new InvalidOperationException("FreeSpacePtr can't be lower then FirstNodePtr! If you are deleting a node, change the value of FirstNodePtr first.");
            if (FreeSpacePtr == -1)
                throw new ArgumentNullException("FreeSpacePtr can't be -1!");

            ValidateSpecPtr(value);

            BinaryPrimitives.WriteInt32LittleEndian(RawSpecFileData.AsSpan(SpecHeaderOffset.FreeSpacePtr), value);
        }
    }

    // Стоит использовать, при создании новых узлов. Но осторожно и внимательно 
    public SpecNodeHelper FreeSpaceAsNode
    {
        get
        {
            return new SpecNodeHelper(RawProdFileData, RawSpecFileData).SetOffset(FreeSpacePtr);
        }
    }


    public SpecNodeHelper NewSpec(ProdNodeHelper prod)
    {
        var newSpec = FreeSpaceAsNode;
        
        newSpec.CanBeDel = 0;
        newSpec.ProdNodePtr = prod.offset;
        newSpec.Mentions = 0;
        newSpec.NextNodePtr = -1;

        prod.SpecNodePtr = newSpec.offset;

        
        var oldFreeSpacePtr = FreeSpacePtr; 
        
        FreeSpacePtr = FreeSpacePtr + NodeSize;

        if (FirstNodePtr == -1)
            FirstNodePtr = oldFreeSpacePtr;

        return newSpec;
    }

    public SpecNodeHelper NewSpecRecord(SpecNodeHelper lastSpecRecord, ProdNodeHelper comp, ushort mentions)
    {
        var newSpecRecord = FreeSpaceAsNode;

        FreeSpacePtr = FreeSpacePtr + NodeSize;

        lastSpecRecord.NextNodePtr = newSpecRecord.offset;

        newSpecRecord.CanBeDel = 0;
        newSpecRecord.ProdNodePtr = comp.offset;
        newSpecRecord.Mentions = mentions;
        newSpecRecord.NextNodePtr = -1;

        return newSpecRecord;
    }
}

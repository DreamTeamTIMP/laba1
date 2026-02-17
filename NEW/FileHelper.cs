using System.Buffers.Binary;

namespace laba1New;

// Интерфейс работы будет такой ProdNodeHelper[offset].CanBeDeleted = -1;
// Интерфейс работы будет такой Console.WriteLine(ProdNodeHelper[offset].CanBeDeleted);

public class ProdNodeHelper
{
    private Span<byte> RawProdFileData;
    private int offset;

    public ProdNodeHelper(byte[] __rawProdFileData)
    {
        RawProdFileData = __rawProdFileData;
    }

    public int this [int _offset]
    {
        get
        {
            offset = _offset;
            this;
        }
    }

    readonly int ShiftToCanBeDeleted = 0;

    public sbyte CanBeDeleted
    {
        get 
        {
            return RawProdFileData[offset + ShiftToCanBeDeleted];
        }
        set 
        {
            RawProdFileData[offset + ShiftToCanBeDeleted] = value;
        }
    }

    readonly int ShiftToPointerToSpecNode = 1;

    public ushort PointerToSpecNode
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData(offset + ShiftToPointerToSpecNode));
        }
        set
        {
            return BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData(offset + ShiftToPointerToSpecNode));
        }
    }

    readonly int ShiftToPointerToNextNode = 5;
    public ushort PointerToNextNode
    {
        get
        {
            return BinaryPrimitives.ReadInt32LittleEndian(RawProdFileData(offset + ShiftToPointerToNextNode));
        }
        set
        {
            return BinaryPrimitives.WriteInt32LittleEndian(RawProdFileData(offset + ShiftToPointerToNextNode));
        }
    }

    public Span<byte> componentData
    {

    }



}

// А не работают подсказки они от тебя тянутся они на русском

public class SpecNodeHelper
{

}

public class FileHeader
{
    public char[] Signature = { 'P', 'S' };
    public short NameLen;
    public int FirstNodePtr = -1;
    public int FreeSpacePtr = 26; // Размер заголовка
    public string SpecFileName = "data.prs";
    public void ToBytes(Span<byte> prim)
    {
        prim.Clear();

    }
}
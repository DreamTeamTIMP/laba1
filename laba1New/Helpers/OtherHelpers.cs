using System.Text;

namespace laba1New.Helpers;

public static class StringHelper
{
    public static void StringToData(ReadOnlySpan<char> src, Span<byte> dist)
    {
        int byteCount = Encoding.UTF8.GetByteCount(src);

        if(byteCount > dist.Length)
            throw new ArgumentException("Name to long!");

        Encoding.UTF8.GetBytes(src, dist);

        dist.Slice(byteCount).Fill(0);
    }

    public static string DataToString(ReadOnlySpan<byte> data) => Encoding.UTF8.GetString(data).Trim('\0', ' ');

    public static void WriteData(ReadOnlySpan<byte> src, Span<byte> dist)
    {
        if (src.Length > dist.Length)
            throw new ArgumentException("Name to long!");

        src.CopyTo(dist);

        dist.Slice(src.Length).Fill(0);
    }
}

public static class PointerHelper
{
    public static  void ValidateSpecPtr(int ptr, byte[] RawSpecFileData)
    {
        if (ptr == -1) return;

        if (ptr + SpecNodeOffset.TotalOffset > RawSpecFileData.Length)
            throw new IndexOutOfRangeException("(pointer + node_size) out of array. Probably you must resize array, don't forget update all links");

        if (ptr < -1)
            throw new InvalidOperationException("Incorrect pointer!");

        if ((ptr - SpecHeaderOffset.TotalOffset) % SpecNodeOffset.TotalOffset != 0)
            throw new ArgumentException("Invalid pointer alignment. The pointer must be exactly at the start of a node.");
    }

    public static void ValidateProdPtr(int ptr, byte[] RawProdFileData)
    {
        if (ptr == -1) return;
        
        if (ptr + ProdNodeOffset.TotalOffset(RawProdFileData) > RawProdFileData.Length)
            throw new IndexOutOfRangeException("(pointer + node_size) out of array. Probably you must resize array, don't forget update all links");

        if (ptr < -1)
            throw new InvalidOperationException("Incorrect pointer!");

        if ((ptr - ProdHeaderOffset.TotalOffset) % ProdNodeOffset.TotalOffset(RawProdFileData) != 0)
            throw new ArgumentException("Invalid pointer alignment. The pointer must be exactly at the start of a node.");
    }
}
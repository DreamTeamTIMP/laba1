using System.Buffers.Binary;

namespace laba1New.Helpers;

public static class SpecHeaderData
{
    public static int FreeSpacePtr(byte[] specFileData) => BinaryPrimitives.ReadInt32LittleEndian(specFileData.AsSpan(SpecHeaderOffset.FreeSpacePtr));
}

public static class ProdHeaderData
{
    public const int SpecFileNameLen = 16;
    
    public static ushort DataSpaceSize(byte[] prodFileData) => BinaryPrimitives.ReadUInt16LittleEndian(prodFileData.AsSpan(ProdHeaderOffset.CompDataSize));
    public static int FreeSpacePtr(byte[] prodFileData) => BinaryPrimitives.ReadInt32LittleEndian(prodFileData.AsSpan(ProdHeaderOffset.FreeSpacePtr));
}

public static class ProdHeaderOffset
{
    public const int Signature = 0;
    public const int CompDataSize = 2;
    public const int FirstNodePtr = 4; 
    public const int FreeSpacePtr = 8;
    public const int SpecFileName = 12;

    public const int TotalOffset = 28;
}

public static class ProdNodeOffset
{
    public const int CanBeDel = 0;
    public const int Type = 1;               // новое поле
    public const int SpecNodePtr = 2;
    public const int NextNodePtr = 6;
    public const int Data = 10;

    public static int TotalOffset(byte[] prodFileData) => 10 + ProdHeaderData.DataSpaceSize(prodFileData);
}

public static class SpecHeaderOffset
{
    public const int FirstNodePtr = 0;   // смещение указател€ на первую запись (4 байта)
    public const int FreeSpacePtr = 4;   // смещение указател€ на свободную область (4 байта)
    public const int TotalOffset = 8;    // общий размер заголовка
}

public static class ComponentTypes
{
    public const byte Product = 0;   // »зделие
    public const byte Node = 1;      // ”зел
    public const byte Detail = 2;    // ƒеталь
}
public static class SpecNodeOffset
{
    public const int CanBeDel = 0;
    public const int ProdNodePtr = 1;
    public const int Mentions = 5;
    public const int NextNodePtr = 7;

    public const int TotalOffset = 11;
}
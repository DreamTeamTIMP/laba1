using System.Buffers.Binary;
using System.Text;

namespace laba1New.Helpers;

using System.Text;

public class ProdNodeHelper(FileStream stream, int offset, ushort nameSize)
{
    private readonly FileStream _stream = stream;
    public int Offset { get; } = offset;
    public int TotalSize => 9 + nameSize;

    // Смещение полей относительно начала узла
    private const int CanBeDelOff = 0;
    private const int SpecPtrOff = 1;
    private const int NextPtrOff = 5;
    private const int NameOff = 9;

    public sbyte CanBeDel
    {
        get => ReadSByte(CanBeDelOff);
        set => WriteSByte(CanBeDelOff, value);
    }
    public int SpecNodePtr
    {
        get => ReadInt(SpecPtrOff);
        set => WriteInt(SpecPtrOff, value);
    }
    public int NextNodePtr
    {
        get => ReadInt(NextPtrOff);
        set => WriteInt(NextPtrOff, value);
    }

    public string Name
    {
        get
        {
            byte[] buf = new byte[nameSize];
            _stream.Seek(Offset + NameOff, SeekOrigin.Begin);
            _stream.Read(buf, 0, nameSize);
            return Encoding.UTF8.GetString(buf).TrimEnd('\0');
        }
        set
        {
            byte[] buf = new byte[nameSize];
            Encoding.UTF8.GetBytes(value.PadRight(nameSize, '\0')).CopyTo(buf, 0);
            _stream.Seek(Offset + NameOff, SeekOrigin.Begin);
            _stream.Write(buf, 0, nameSize);
        }
    }

    private int ReadInt(int rel)
    {
        _stream.Seek(Offset + rel, SeekOrigin.Begin);
        byte[] b = new byte[4]; _stream.Read(b, 0, 4);
        return BitConverter.ToInt32(b, 0);
    }
    private void WriteInt(int rel, int val)
    {
        _stream.Seek(Offset + rel, SeekOrigin.Begin);
        _stream.Write(BitConverter.GetBytes(val), 0, 4);
    }
    private void WriteSByte(int rel, sbyte val)
    {
        _stream.Seek(Offset + rel, SeekOrigin.Begin);
        _stream.WriteByte((byte)val);
    }
    private sbyte ReadSByte(int rel)
    {
        _stream.Seek(Offset + rel, SeekOrigin.Begin);
        return (sbyte)_stream.ReadByte();
    }
}

public class SpecNodeHelper
{
    private readonly FileStream _stream;
    public int Offset { get; private set; }

    // Константные смещения внутри узла (всего 11 байт)
    private const int CanBeDelOff = 0;
    private const int ProdPtrOff = 1;
    private const int MentionsOff = 5;
    private const int NextPtrOff = 7;

    public SpecNodeHelper(FileStream stream, int offset)
    {
        _stream = stream;
        Offset = offset;
    }

    // Позволяет переиспользовать объект для другого смещения (удобно при обходе списка)
    public SpecNodeHelper SetOffset(int offset)
    {
        if (offset < 0 && offset != -1)
            throw new ArgumentOutOfRangeException(nameof(offset));
        Offset = offset;
        return this;
    }

    public sbyte CanBeDel
    {
        get => ReadSByte(CanBeDelOff);
        set => WriteSByte(CanBeDelOff, value);
    }

    public int ProdNodePtr
    {
        get => ReadInt(ProdPtrOff);
        set => WriteInt(ProdPtrOff, value);
    }

    public ushort Mentions
    {
        get => ReadUInt16(MentionsOff);
        set => WriteUInt16(MentionsOff, value);
    }

    public int NextNodePtr
    {
        get => ReadInt(NextPtrOff);
        set => WriteInt(NextPtrOff, value);
    }

    #region Приватные методы чтения/записи

    private int ReadInt(int relOffset)
    {
        _stream.Seek(Offset + relOffset, SeekOrigin.Begin);
        byte[] buffer = new byte[4];
        _stream.Read(buffer, 0, 4);
        return BitConverter.ToInt32(buffer, 0);
    }

    private void WriteInt(int relOffset, int value)
    {
        _stream.Seek(Offset + relOffset, SeekOrigin.Begin);
        _stream.Write(BitConverter.GetBytes(value), 0, 4);
    }

    private ushort ReadUInt16(int relOffset)
    {
        _stream.Seek(Offset + relOffset, SeekOrigin.Begin);
        byte[] buffer = new byte[2];
        _stream.Read(buffer, 0, 2);
        return BitConverter.ToUInt16(buffer, 0);
    }

    private void WriteUInt16(int relOffset, ushort value)
    {
        _stream.Seek(Offset + relOffset, SeekOrigin.Begin);
        _stream.Write(BitConverter.GetBytes(value), 0, 2);
    }

    private sbyte ReadSByte(int relOffset)
    {
        _stream.Seek(Offset + relOffset, SeekOrigin.Begin);
        return (sbyte)_stream.ReadByte();
    }

    private void WriteSByte(int relOffset, sbyte value)
    {
        _stream.Seek(Offset + relOffset, SeekOrigin.Begin);
        _stream.WriteByte((byte)value);
    }

    #endregion
}
using System.Buffers.Binary;
using System.Text;

namespace laba1New.Helpers;

using System.Text;

public class ProdNodeHelper(FileStream stream, int offset, ushort _nameSize)
{
    private readonly FileStream _stream = stream;
    public int Offset { get; } = offset;

    // Размер записи: 1 (canBeDel) + 1 (type) + 4 (spec) + 4 (next) + nameSize
    public int TotalSize => 10 + _nameSize;

    // Бит удаления (смещение 0)
    public sbyte CanBeDel
    {
        get => ReadSByte(0);
        set => WriteSByte(0, value);
    }

    // Тип компонента (смещение 1)
    public byte Type
    {
        get => (byte)ReadSByte(1);
        set => WriteSByte(1, (sbyte)value);
    }

    // Указатель на спецификацию (смещение 2)
    public int SpecNodePtr
    {
        get => ReadInt(2);
        set => WriteInt(2, value);
    }

    // Указатель на следующий узел (смещение 6)
    public int NextNodePtr
    {
        get => ReadInt(6);
        set => WriteInt(6, value);
    }

    // Имя компонента (смещение 10)
    private static readonly Encoding NameEncoding = Encoding.GetEncoding(1251);

    public string Name
    {
        get
        {
            byte[] b = new byte[_nameSize];
            _stream.Seek(Offset + 10, SeekOrigin.Begin);
            _stream.Read(b, 0, _nameSize);
            return NameEncoding.GetString(b).TrimEnd(' ', '\0');
        }
        set
        {
            byte[] b = new byte[_nameSize];
            byte[] src = NameEncoding.GetBytes(value);
            if (src.Length > _nameSize)
                throw new ArgumentException($"Имя слишком длинное (макс. {_nameSize} байт)");
            Array.Copy(src, b, src.Length);
            // Заполняем остаток пробелами (код 32)
            for (int i = src.Length; i < _nameSize; i++) b[i] = 32;
            _stream.Seek(Offset + 10, SeekOrigin.Begin);
            _stream.Write(b, 0, _nameSize);
        }
    }

    // Вспомогательные методы для чтения/записи
    private int ReadInt(int rel) { _stream.Seek(Offset + rel, SeekOrigin.Begin); byte[] b = new byte[4]; _stream.Read(b, 0, 4); return BitConverter.ToInt32(b, 0); }
    private void WriteInt(int rel, int v) { _stream.Seek(Offset + rel, SeekOrigin.Begin); _stream.Write(BitConverter.GetBytes(v), 0, 4); }
    private void WriteSByte(int rel, sbyte v) { _stream.Seek(Offset + rel, SeekOrigin.Begin); _stream.WriteByte((byte)v); }
    private sbyte ReadSByte(int rel) { _stream.Seek(Offset + rel, SeekOrigin.Begin); return (sbyte)_stream.ReadByte(); }
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
using System.Security.Authentication.ExtendedProtection;

namespace laba1;

public class ListRoot
{
    private byte[] _prodFileName = "products"u8.ToArray();
    private byte[] _specFileName = "products"u8.ToArray();
    public byte[] ProdFileName 
    {
        get 
        {
            return _prodFileName;
        }
        set 
        {
            if(value is not null && value.Length <= 16)
            {
                _prodFileName = value;
            }
            else
            {
                throw new InvalidDataException("Products file name to long!");
            }
        }
    }

    public byte[] SpecFileName 
    {
        get 
        {
            return _specFileName;
        }
        set 
        {
            if(value is not null && value.Length <= 16)
            {
                _specFileName = value;
            }
            else
            {
                throw new InvalidDataException("Specifications file name to long!");
            }
        }
    }

    public short DateSpaceSize = 16; // В байтах

    public ProdListNode? prodList = null;
    public SpecListNode? specList = null;

    public void AddProd(ProdListNode prod)
    {
        prodList?.AddToEnd(prod);
    }

    public void AddProd(byte[]? specName, )
    {
        prodList?.AddToEnd(prod);
    }

    public void AddSpec(ProdListNode prod)
    {
        prodList?.AddToEnd(prod);
    }
}

public class ProdListNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    private sbyte canBeDeleted = -1;
    public ProdListNode? Next = null;
    public SpecListNode? Spec = null; // У деталей нет спецификации => null  

    private byte[] componentData = []; // Например для DataSpaceSize 16 байт "abcdefqw123\0\0\0\0\0"u8

    public ProdListNode(SpecListNode? spec = null, byte[]? data = null,sbyte canBeDel = -1)
    {
        if(data is null)
            componentData = "default.ps"u8.ToArray();
        canBeDeleted = canBeDel;
        Spec = spec;
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
    private sbyte canBeDeleted = -1;
    public SpecListNode? next = null;
    public ProdListNode? prod = null;
    public short mentions = 0;
}

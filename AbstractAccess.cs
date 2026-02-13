
namespace laba1;

public class SpecListRoot
{

}

public class SpecListNode
{

}

public class ProductListRoot
{
    private byte[] ThisFileName = "productList"u8.ToArray(); // Without .ps
    private short DateSpaceSize = 16; // В байтах

    public byte[] SpecFileName = "productList"u8.ToArray(); // Without .prs

    public List<SpecListNode> RefToSpecList { get; private set; } 
    
    public List<ProductListNode> ProdList { get; private set; } = [];
}

public class ProductListNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    private sbyte canBeDeleted = -1;
    
    public SpecListNode? spec = null; // У деталей нет спецификации => null  
    
    private byte[] componentData; // Например для DataSpaceSize 16 байт "abcdefqw123\0\0\0\0\0"u8
}
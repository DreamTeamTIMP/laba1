namespace laba1;

class Product
{
    private string _name;
    private readonly PartTypes _type;
    private Product _next;
    private Spec _spec;
    private Node _node;
}
class Node
{
    private string _name;
    private Node next;
    private Spec? spec;
}
enum PartTypes
{
    Detail,
    Node,
    Super
}


public class Spec
{
    private string _Name;

}

public abstract class FileManager
{
}
public class FileCreator : FileManager
{
    public void Create(string name) { }
}
public class FileOpener : FileManager 
{
    public void Open(string name) { }
}


public class ProductListFile
{
    private byte[] signature = [80,83]; // Сигнатура два байта, представляющие символы ‘PS’.
    private short DateSpaceSize = 16; // Длина имени компанента в записи
    private int pointerToFirstRecord = -1;
    private int pointerToFreeSpace;
    private string? specificationFileName;
}

public class ProductRecord
{
    private sbyte canBeDeleted = -1;
    private int pointerToFirstComponent = -1;
    private int pointerToNextRecord = -1;
    private string? componentName; 
}

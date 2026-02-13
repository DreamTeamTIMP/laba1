namespace laba1;

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

//-1 аналог null по заданию

public class FileSpecListRoot
{
    // В задании не сказанно про сигнатуру, но это очевидно необходимая часть
    private byte[] signature = "PRS"u8.ToArray(); // Сигнатура два байта, представляющие символы ‘PRS’.
    private int pointerToFirstNode = -1;
    private int pointerToFreeSpace;
}

public class FileSpecListNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    private sbyte canBeDeleted = -1;
    private int pointerToProductNode = -1;
    private short countOfMentions = 0; // Число вхождений
    private int pointerToNextNode = -1;
}


public class FileProductListRoot
{
    private byte[] signature = "PS"u8.ToArray(); // Сигнатура два байта, представляющие символы ‘PS’.
    private short DateSpaceSize = 16; // Длина имени компанента в записи
    private int pointerToFirstNode = -1;
    private int pointerToFreeSpace;
    private byte[] specFileName; //!!! Without .prs
}

public class FileProductListNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    private sbyte canBeDeleted = -1;
    private int pointerToSpecNode = -1;
    private int pointerToNextNode = -1;
    private byte[] componentName; 
}

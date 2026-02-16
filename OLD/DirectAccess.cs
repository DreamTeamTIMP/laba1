using System.IO;
using System.Runtime.InteropServices;

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

public class RootFile
{
    public FileProdHeader ProdFile;
    public FileSpecHeader SpecFile;

    public List<FileProdHeader> ProdNodes = [];
    public List<FileSpecHeader> SpecNodes = [];

    public byte[] RawSpec;
    public byte[] RawProd;

    public string Path;

    public void Create(string path, string fileName, ushort compDataSize, string specFileName)
    {
        
    }

    public void Open(string path)
    {

    }
}






//-1 аналог null по заданию
public class FileSpecHeader
{
    // В задании не сказанно про сигнатуру, но это очевидно необходимая часть
    byte[] signature = "PRS"u8.ToArray(); // Сигнатура два байта, представляющие символы ‘PRS’.
    int pointerToFirstNode = -1;
    int pointerToFreeSpace = -1;
}

public class FileSpecNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    sbyte canBeDeleted = 0;
    int pointerToProductNode = -1;
    ushort countOfMentions = 0; // Число вхождений
    int pointerToNextNode = -1;
}

public class FileProdHeader
{
    byte[] signature = "PS"u8.ToArray(); // Сигнатура два байта, представляющие символы ‘PS’.
    ushort DateSpaceSize = 16; // Длина имени компанента в записи
    int pointerToFirstNode = -1;
    int pointerToFreeSpace = -1;
    byte[] specFileName = "products.prs"u8.ToArray(); // 16 байт
}

public class FileProdNode
{
    //Бит удаления может иметь значение 0 (запись активна) или -1 (запись помечена наудаление).
    sbyte canBeDeleted = 0;
    int pointerToSpecNode = -1;
    int pointerToNextNode = -1;
    byte[] componentName = "default"u8.ToArray(); 
}

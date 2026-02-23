using System;
using System.Text;


namespace laba1New;

public class Program
{
    static public int Main()
    {
        CLI.Run();
        return 0;
    }
}

public static class Constants
{
    public const string ProductSignature = "PS";
    public const string ProductExt = ".prd";     
    public const string SpecExt = ".prs";        
    public const int NullPointer = -1;           
}

public interface IManage
{
    public void Create(string fileName, string? specFileName = null);
    public void AddProduct(string name, string type);
    public void PrintAll();
    public void PrintProducts();
    public void PrintSpecs();

    // Input 
    public void AddNode(string name);
    public void AddDetail(string name);
    public void AddToSpec(string prodName, string compName, ushort mentions);
}

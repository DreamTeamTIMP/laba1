using System;
using System.Text;

using laba1New.Interactive;

namespace laba1New;

public class Program
{
    static public int Main()
    {
        Console.WriteLine("Hello World!");
        
        DataManager manager = new();

        CLI cli = new(manager);

        cli.Start();
        
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

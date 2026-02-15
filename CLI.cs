using System.Text;

namespace laba1;

public class CLI
{
    private string input = "";
    private ListRoot listRoot;

    private string[][] commandsInfo = [
        ["input", "bla bla"],
        ["printall", "bla bla"],
        ["help", "bla bla"],
        ["exit", "bla bla"],
    ];

    public CLI()
    {
        listRoot = new();
        listRoot.ProdFileName = "Sigma.ps";
        listRoot.SpecFileName = "Sigma.prs";

    }

    public void Start()
    {
        while(true)
        {
            try
            {
                ReadLine();
                if (ParseAndExec())
                    break;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error: {exception.Message}");
            }
        }
    }

    public void ReadLine()
    {
        Console.Write("PS> ");
        input = Console.ReadLine() ?? "";
    }

    public bool ParseAndExec()
    {
        if(input is null)
            throw new Exception("Incorrect input!");
        
        input.ToLower();
        string[] args = input.Split(' ');
        int argc = args.Length;


        switch (args[0])
        {
            case "input":
                switch (argc) 
                {
                    case 1:
                    case 2:                        
                        throw new Exception("Not enought argumets");
                    case 3:
                        switch (args[2])
                        {
                            case "product":
                            case "node":
                                listRoot.AddNode(args[1]);
                                break;
                            case "detail":
                                listRoot.AddDetail(args[1]);
                                break;        
                            default:
                                listRoot.AddToSpec(args[1], args[2], 1);
                                break;
                        }
                        break;
                    case 4:
                        if (!ushort.TryParse(args[3], out ushort mentions)) 
                            throw new ArgumentException($"Incorrect number. It must be a natural number not exceeding {ushort.MaxValue}");
                        listRoot.AddToSpec(args[1], args[2], mentions);
                        break;    

                    default:
                        throw new Exception("To many argumets");
                }
                break;
            
            case "printall":
                listRoot.PrintAll();
                break;

            case "exit":
                return true;
            case "help":
                ShowHelp();
                break;
            default:
                Console.WriteLine("Unknown command. Try help");
                break;
        }
        
        return false;
    }

    private void ShowHelp()
    {
        Console.WriteLine($"{"", 15}Help");
        Console.WriteLine($"{"Command", -30}Definition");
        foreach(var info in commandsInfo)
        {
            Console.WriteLine($"{info[0], -30}{info[1]}");
        }
    }
}
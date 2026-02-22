namespace laba1New.Interactive;

public class CLI(DataManager manager)
{
    private string input = "";
    private readonly string[][] commandsInfo = [
        ["create", "bla bla"],
        ["input", "bla bla"],
        ["printall", "bla bla"],
        ["help", "bla bla"],
        ["exit", "bla bla"],
    ];

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
                //Console.WriteLine($"Error: {exception.Message}");
                Console.WriteLine(exception);
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
            case "create":
                switch (argc)
                {
                    case 1:
                        throw new Exception("Not enought argumets");
                    case 2:
                        manager.Create(args[1], 16, null);
                        break;
                    case 3:
                    case 4:
                        if (!ushort.TryParse(args[2], out ushort dataSize) || dataSize < 0)
                            throw new ArgumentException($"Incorrect number. It must be a natural number not exceeding {int.MaxValue}");
                        if (argc == 3)
                            manager.Create(args[1], dataSize, null);
                        else
                            manager.Create(args[1], dataSize, args[3]);
                        break;
                    default:
                        throw new Exception("To many argumets");
                }
                break;
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
                                manager.AddProduct(args[1]);
                                break;
                            case "detail":
                                manager.AddDetail(args[1]);
                                break;        
                            default:
                                manager.AddToSpec(args[1], args[2], 1);
                                break;
                        }
                        break;
                    case 4:
                        if (!ushort.TryParse(args[3], out ushort mentions)) 
                            throw new ArgumentException($"Incorrect number. It must be a natural number not exceeding {ushort.MaxValue}");
                        manager.AddToSpec(args[1], args[2], mentions);
                        break;    

                    default:
                        throw new Exception("To many argumets");
                }
                break;
            
            case "printall":
                manager.PrintAll();
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
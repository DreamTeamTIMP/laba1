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
        input = input.Trim().ToLower();
        string[] args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) return false;

        switch (args[0])
        {
            case "create":
                manager.Create(args[1]);
                break;
            case "input":
                // input product <name> | input detail <name>
                if (args[1] == "product") manager.AddProduct(args[2], true);
                else if (args[1] == "detail") manager.AddDetail(args[2]);
                break;
            case "delete":
                manager.Delete(args[1]);
                Console.WriteLine("Marked as deleted.");
                break;
            case "restore":
                manager.Restore(args[1]);
                Console.WriteLine("Restored.");
                break;
            case "truncate":
                manager.Truncate();
                break;
            case "print":
                manager.PrintAll();
                break;
            case "open":
                if (args.Length < 2) throw new Exception("Usage: open <filename>");
                manager.Open(args[1]);
                break;

            case "delete":
                if (args.Length < 2) throw new Exception("Usage: delete <name>");
                manager.Delete(args[1]);
                break;

            case "restore":
                if (args.Length < 2) throw new Exception("Usage: restore <name>");
                manager.Restore(args[1]);
                break;

            case "help":
                ShowHelp();
                break;
            case "exit":
                return true;
            default:
                Console.WriteLine("Unknown command.");
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
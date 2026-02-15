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
        
        switch (args[0])
        {
            case "input":
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
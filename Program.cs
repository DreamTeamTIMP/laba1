using System;
using System.Linq.Expressions;


namespace laba1;

public class Program
{
    static public int Main()
    {
        ListRoot listRoot = new();
        listRoot.ProdFileName = "Sigma.ps";
        listRoot.SpecFileName = "Sigma.prs";
        

        while(true)
        {
            try
            {
                Console.Write("PS> ");
                string? input = Console.ReadLine();

                if(string.IsNullOrEmpty(input))
                    throw new Exception("Incorrect input!");

                if(input == "PrintAll")
                    listRoot.printAll();
                    
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error: {exception.Message}");
            }

        }



        return 0;
    }
}
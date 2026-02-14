using System;
using System.Linq.Expressions;


namespace laba1;

public class Program
{
    static public int Main()
    {
        ListRoot listRoot = new();
        listRoot.ProdFileName = "Sigma.ps"u8.ToArray();
        listRoot.SpecFileName = "Sigma.prs"u8.ToArray();
        

        while(true)
        {
            try
            {
                Console.Write("PS> ");
                string? input = Console.ReadLine();

                if(string.IsNullOrEmpty(input))
                    throw new Exception("Incorrect input!");
            
                    
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error: {exception.Message}");
            }

        }



        return 0;
    }
}
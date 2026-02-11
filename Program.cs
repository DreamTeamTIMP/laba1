using System;
using System.Linq.Expressions;


namespace laba1;

public class Program
{
    static public int Main()
    {
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
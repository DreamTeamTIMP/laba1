using System;
using System.Text;


namespace laba1New;

public class Program
{
    static public int Main()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        CLI.Run();
        return 0;
    }
}
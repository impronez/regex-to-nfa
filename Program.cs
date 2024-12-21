namespace regex_to_nfa;

class Program
{
    static void Main(string[] args)
    {
        /*if (args.Length != 2)
        {
            Console.WriteLine("Usage: regexToNFA <output.csv> <regex>");
            return;
        }
        */
        string outputFilePath = "out.csv";
        
        string regex = "ab|c(a|(cd+|c)*)";// ab*|c+  ab|c(a|(cd+|c)*)   (a|b)*|(b|c)+   (a+b(b+ab)*aa)*
        Console.WriteLine($"{regex}");

        /*regex = regex.Replace(" ", "");*/
        RegexToNfaConverter converter = new RegexToNfaConverter(outputFilePath, regex);
        converter.SplitExpression();
    }
}
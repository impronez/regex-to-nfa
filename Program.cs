namespace regex_to_nfa;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: <program> <output.csv> <regex>");
            return;
        }
        string outputFilePath = args[0];
        
        string regex = args[1];// c+|ab* ab*|c+  ab|c(a|(cd+|c)*)   (a|b)*|(b|c)+   (a+b(b+ab)*aa)*    (r*|su*t)*su*
        RemoveWhiteSpaces(ref regex);
        Console.WriteLine($"{regex}");
        
        RegexToNodeTree regexToNodeTreeСonverter = new RegexToNodeTree(outputFilePath, regex);
        Node node = regexToNodeTreeСonverter.SplitExpression();
        
        NodeTreeToNfa nodeTreeToNfa = new NodeTreeToNfa();
        Nfa automata = nodeTreeToNfa.GetNfaFromNodeTree(node);
        
        automata.ExportToFile($"{outputFilePath}");
    }

    static void RemoveWhiteSpaces(ref string str)
    {
        str = str.Replace(" ", "");
    }
}
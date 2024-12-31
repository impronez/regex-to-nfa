namespace regex_to_nfa;

public class Nfa(
    List<string> states,
    List<string> inputs,
    string startState,
    string finalState,
    Dictionary<string, Dictionary<string, List<string>>> transitions)
{
    private List<string> _states = states;
    private List<string> _inputs = inputs;
    private string _startState = startState;
    private string _finalState = finalState;
    
    private Dictionary<string, Dictionary<string, List<string>>> _transitions = transitions;

    public void ExportToFile(string filePath)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            writer.Write(";");
            for (int i = 0; i < _states.Count; i++)
            {
                if (_states[i] == _finalState)
                {
                    writer.Write("F");
                }

                if (_states.Count != i + 1)
                {
                    writer.Write(";");
                }
            }
            writer.WriteLine();
            
            foreach (var state in _states)
            {
                writer.Write($";{state}");
            }
            writer.WriteLine();

            foreach (var input in _inputs)
            {
                writer.Write($"{input}");

                foreach (var state in _states)
                {
                    writer.Write(";");

                    if (_transitions[state].ContainsKey(input))
                    {
                        int count = _transitions[state][input].Count;
                        for (int i = 0; i < count; i++)
                        {
                            string nextState = _transitions[state][input][i];
                            
                            writer.Write($"{nextState}");

                            if (i != count - 1)
                            {
                                writer.Write(",");
                            }
                        }
                    }
                }
                
                writer.WriteLine();
            }
        }
    }
}
namespace regex_to_nfa;

public class NodeTreeToNfa
{
    public const string EmptyTransition = "ε";
    
    private List<string> _states = new();
    private List<string> _inputs = new();
    private Dictionary<string, Dictionary<string, List<string>>> _transitions = new(); // state input nextStates
    private int _currentState = 1;
    private string _finalState = "";

    public Nfa GetNfaFromNodeTree(Node node)
    {
        string startState = "q0";
        InitializeState(startState);
        string finalState = "";
        
        ProcessGroupNode(node, startState, ref finalState);

        //PrintAutomata();
        
        Console.WriteLine($"Final state: {finalState}");
        
        return new Nfa(_states, _inputs, startState, finalState, _transitions);
    }

    private void ProcessGroupNode(Node node, string startState, ref string finalState)
    {
        string thisStartState = startState;
        foreach (var child in node.Children)
        {
            bool isLastChild = child == node.Children.Last();
            switch (child.Type)
            {
                case StateType.Alternation:
                    ProcessAlternationNode(child, thisStartState, ref finalState);
                    break;
                case StateType.Quantifier:
                    ProcessQuantifierNode(child, thisStartState, ref finalState);
                    break;
                case StateType.Group:
                    ProcessGroupNode(child, thisStartState, ref finalState);
                    break;
                case StateType.Symbol:
                    ProcessSymbolNode(child, thisStartState, ref finalState, isLastChild);
                    break;
                case StateType.None:
                    throw new ArgumentException("Child has none-state in group node processing");
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            thisStartState = finalState;
        }
    }
    
    private void ProcessAlternationNode(Node node, string startState, ref string finalState)
    {
        finalState = GetNewState();
        
        foreach (var child in node.Children)
        {
            switch (child.Type)
            {
                case StateType.Alternation:
                    throw new ArgumentException("Alternation node has alternation child");
                case StateType.Quantifier:
                    ProcessQuantifierNodeByAlternationNode(child, startState, finalState);
                    break;
                case StateType.Group:
                    ProcessGroupNodeByAlternationNode(child, startState, finalState);
                    break;
                case StateType.Symbol:
                    AddTransition(startState, child.Value, finalState);
                    break;
                case StateType.None:
                    throw new ArgumentException("Child has none-state in alternation node processing");
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    private void ProcessGroupNodeByAlternationNode(Node node, string startState, string finalState)
    {
        string forThisStartState = startState;
        //string forThisStartState = GetNewState();
        //AddTransition(startState, EmptyTransition, forThisStartState);

        string forThisFinalState = finalState;
        //string forThisFinalState = "";
        
        ProcessGroupNode(node, forThisStartState, ref forThisFinalState);
        
        AddTransition(forThisFinalState, EmptyTransition, finalState);
    }

    private void ProcessQuantifierNodeByAlternationNode(Node node, string startState, string finalState)
    {
        if (node.Children.Count > 1)
            throw new Exception("Can't quantifier node more than one children");
        
        if (node.LastNode == null)
            throw new Exception("Quantifier can't be empty");

        switch (node.LastNode.Type)
        {
            case StateType.Group:
                ProcessGroupNodeByQuantifierNodeInAlternationNode(node.LastNode, node.Value, startState, finalState);
                break;
            case StateType.Symbol:
                ProcessSymbolNodeByQuantifierNodeInAlternationNode(node.LastNode, node.Value, startState, finalState);
                break;
            default:
                throw new Exception($"Quantifier node child can't be {node.LastNode.Type} type");
        }
    }

    private void ProcessGroupNodeByQuantifierNodeInAlternationNode(Node node, string type,
        string startState, string finalState)
    {
        string forThisStartState = GetNewState();
        AddTransition(startState, EmptyTransition, forThisStartState);

        string forThisFinalState = "";
        
        ProcessGroupNode(node, forThisStartState, ref forThisFinalState);
        
        AddTransition(forThisFinalState, EmptyTransition, forThisStartState);

        switch (type)
        {
            case "*":
                AddTransition(forThisStartState, EmptyTransition, finalState);
                break;
            case "+":
                AddTransition(forThisFinalState, EmptyTransition, finalState);
                break;
            default:
                throw new ArgumentException($"Impossible quantifier: {type} in process group node by quantifier in alternation node");
        }
    }

    private void ProcessSymbolNodeByQuantifierNodeInAlternationNode(Node node, string type,
        string startState, string finalState)
    {
        var nextState = GetNewState();

        if (type == "*")
        {
            AddTransition(startState, EmptyTransition, finalState);
        }
        
        AddTransition(startState, node.Value, nextState);
        AddTransition(nextState, node.Value, nextState);
        
        AddTransition(nextState, EmptyTransition, finalState);
    }

    private void ProcessSymbolNode(Node node, string fromState, ref string outState, bool isLastChild)
    {
        outState = GetNewState();
        
        AddTransition(fromState, node.Value, outState);
    }

    private void ProcessQuantifierNode(Node node, string inState, ref string outState)
    {
        if (node.Children.Count > 1)
            throw new Exception("Can't quantifier node more than one children");
        
        if (node.LastNode == null)
            throw new Exception("Quantifier can't be empty");

        switch (node.LastNode.Type)
        {
            case StateType.Group:
                ProcessGroupNodeByQuantifierNode(node.LastNode, node.Value, inState, ref outState);
                break;
            case StateType.Symbol:
                ProcessSymbolNodeByQuantifierNode(node.LastNode, node.Value, inState, ref outState);
                break;
            default:
                throw new Exception($"Quantifier node child can't be {node.LastNode.Type} type");
        }
    }

    private void ProcessGroupNodeByQuantifierNode(Node node, string type, string inState, ref string outState)
    {
        //string startState = inState;
        string startState = GetNewState();
        AddTransition(inState, EmptyTransition, startState);

        string finalState = !string.IsNullOrEmpty(outState) ? outState : GetNewState();
        
        ProcessGroupNode(node, startState, ref finalState); // outState or finalState
        
        if (type == "*")
        {
            AddTransition(startState, EmptyTransition, finalState);
        }
        
        AddTransition(finalState, EmptyTransition, startState);

        if (!string.IsNullOrEmpty(outState))
        {
            AddTransition(finalState, EmptyTransition, outState); // TODO: CHECK!!!
        }
        else
        {
            outState = finalState;
        }

        if (type == "+")
        {
            outState = finalState;
        }    
    }

    private void ProcessSymbolNodeByQuantifierNode(Node node, string type, string inState, ref string outState)
    {
        var nextState = GetNewState();
        AddTransition(nextState, node.Value, nextState);
        switch (type)
        {
            case "*":
                AddTransition(inState, EmptyTransition, nextState);
                break;
            case "+":
                AddTransition(inState, node.Value, nextState);
                break;
            default:
                throw new Exception($"Quantifier has an impossible value: {node.Value}");
        }
        
        outState = nextState;
    }
    
    private void AddTransition(string fromState, string input, string nextState)
    {
        InitializeState(fromState);
        InitializeState(nextState);
        InitializeInputByState(fromState, input);
        
        if (!_transitions[fromState][input].Contains(nextState))
        {
            _transitions[fromState][input].Add(nextState);
        }
    }

    private string GetNewState()
    {
        return "q" + _currentState++;
    }

    private void InitializeInputByState(string state, string input)
    {
        if (!_inputs.Contains(input))
        {
            _inputs.Add(input);
        }

        if (!_transitions[state].ContainsKey(input))
        {
            _transitions[state][input] = new List<string>();
        }
    }

    private void InitializeState(string state)
    {
        if (!_states.Contains(state))
        {
            _states.Add(state);
        }

        if (!_transitions.ContainsKey(state))
        {
            _transitions.Add(state, new());
        }
    }
    
    private void PrintAutomata()
    {
        foreach (var state in _states)
        {
            Console.WriteLine($"{state}:");
            foreach (var list in _transitions[state])
            {
                Console.Write($"    {list.Key} -> ");
                foreach (var nextState in list.Value)
                {
                    Console.Write($" {nextState}");
                }
                Console.WriteLine();
            }
        }
    }
}
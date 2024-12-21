namespace regex_to_nfa;

public enum StateType
{
    Alternation,
    Quantifier,
    Group,
    Symbol,
    None
};

public class RegexToNfaConverter
{
    private const string AlternationOperator = "|";
    private const string ZeroOrMoreQuantifier = "*";
    private const string OneOrMoreQuantifier = "+";
    private const string GroupOperator = "(";

    private readonly string _outputFilePath;
    private readonly string _regex;

    private StateType _state = StateType.None;
    private readonly Node _root;
    private Node _lastNode;
    
    public RegexToNfaConverter(string outputFilePath, string regex)
    {
        _outputFilePath = outputFilePath;
        _regex = regex;

        _root = new("s", null);
        _lastNode = _root;
    }
    
    public void SplitExpression()
    {
        ParseExpression(_root, _regex);

        PrintNode(_root, 0);
    }

    private void ParseExpression(Node node, string regex)
    {
        _lastNode = node;
        SetState(StateType.None);
        
        for (int i = 0; i < regex.Length; i++)
        {
            ParseSymbol(node, regex[i].ToString(), regex, ref i);
        }
    }
    
    private void ParseSymbol(Node node, string symbol, string regex, ref int index)
    {
        switch (symbol)
        {
            case AlternationOperator:
                AddAlternationNode(node);
                SetState(StateType.Alternation);
                break;
            case ZeroOrMoreQuantifier:
            case OneOrMoreQuantifier:
                AddQuantifierNode(node, symbol);
                SetState(StateType.Quantifier);
                break;
            case GroupOperator:
                AddGroupNode(node, regex, ref index);
                SetState(StateType.Group);
                break;
            default:
                AddSymbolNode(node, symbol);
                SetState(StateType.Symbol);
                break;
        }
    }

    private void AddGroupNode(Node node, string regex, ref int index)
    {
        var prevState = _state;
        var lastNode = _lastNode;
        
        var groupNode = new Node(GroupOperator, null);
        
        string expression = GetGroupExpressionString(regex, ref index);
        ParseExpression(groupNode, expression);

        AttachGroupNode(prevState, lastNode, groupNode);
    }

    private void AttachGroupNode(StateType prevState, Node lastNode, Node groupNode)
    {
        switch (prevState)
        {
            case StateType.Alternation:
            case StateType.None:
                groupNode.Parent = lastNode;
                lastNode.AddNode(groupNode);
                break;
            case StateType.Quantifier:
            case StateType.Group:
            case StateType.Symbol:
                if (lastNode.Parent == null)
                {
                    throw new InvalidOperationException("Parent of node is null");
                }
                
                if (IsParentAlternationNode(lastNode))
                {
                    lastNode = GroupedNode(lastNode, groupNode);
                    lastNode.Parent!.LastNode = lastNode;
                }
                else
                {
                    lastNode.Parent.AddNode(groupNode);
                    groupNode.Parent = lastNode.Parent;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        _lastNode = groupNode;
    }

    private void AddAlternationNode(Node node)
    {
        if (node.IsEmpty())
        {
            throw new ArgumentException("Can't add an alternation node for empty node");
        }

        var alternationNode = new Node(AlternationOperator, _lastNode);

        switch (_state)
        {
            case StateType.Alternation:
                throw new ArgumentException("Can't add an alternation node for alternation node");
            case StateType.Quantifier:
            case StateType.Group:
            case StateType.Symbol:
                if (_lastNode.Parent == null)
                    throw new ArgumentException("Can't add an alternation node to node without parent");

                if (IsAlternationNode(_lastNode.Parent))
                {
                    _lastNode = _lastNode.Parent;
                    return;
                }
                
                if (_lastNode.Parent.Parent != null && IsAlternationNode(_lastNode.Parent.Parent))
                {
                    _lastNode = _lastNode.Parent.Parent;
                    return;
                }

                if (_lastNode.Parent.Children.Count > 1)
                {
                    var groupNode = new Node(GroupOperator, _lastNode.Parent);
                    groupNode.Children = new List<Node>(_lastNode.Parent.Children);
                    SetChildrenParent(groupNode);
                    _lastNode = groupNode;
                }
                
                alternationNode.AddNode(_lastNode);
                _lastNode.Parent!.Children.Clear();
                _lastNode.Parent!.AddNode(alternationNode);
                _lastNode = alternationNode;
                break;
            case StateType.None:
                throw new ArgumentException("Can't add an alternation node by none-state");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SetChildrenParent(Node parentNode)
    {
        foreach (var node in parentNode.Children)
        {
            node.Parent = parentNode;
        }
    }

    private void AddQuantifierNode(Node node, string symbol)
    {
        if (node.IsEmpty())
        {
            throw new ArgumentException("Can't add an quantifier node for empty node");
        }
        
        var quantifierNode = new Node(symbol, _lastNode.Parent);

        switch (_state)
        {
            case StateType.Alternation:
                throw new ArgumentException("Can't add an quantifier node for alternation node");
            case StateType.Quantifier:
                throw new ArgumentException("Can't add an quantifier node for quantifier node");
            case StateType.Group:
            case StateType.Symbol:
                if (_lastNode.Parent == null)
                    throw new ArgumentException("Can't add an quantifier node to node without parent");
                
                quantifierNode.AddNode(_lastNode);
                _lastNode.Parent.LastNode = quantifierNode;
                _lastNode = quantifierNode;
                break;
            case StateType.None:
                throw new ArgumentException("Can't add an quantifier node by none-state");
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void AddSymbolNode(Node node, string symbol)
    {
        var symbolNode = new Node(symbol, _lastNode);

        switch (_state)
        {
            case StateType.Alternation:
            case StateType.None:
                _lastNode.AddNode(symbolNode);
                break;
            case StateType.Quantifier:
            case StateType.Group:
            case StateType.Symbol:
                if (_lastNode.Parent == null)
                {
                    throw new InvalidOperationException("Parent of node is null");
                }
                
                if (IsParentAlternationNode(_lastNode))
                {
                    _lastNode = GroupedNode(_lastNode, symbolNode);
                    _lastNode.Parent!.LastNode = _lastNode;
                }
                else
                {
                    _lastNode.Parent.AddNode(symbolNode);
                    symbolNode.Parent = _lastNode.Parent;
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        _lastNode = symbolNode;
    }

    private Node GroupedNode(Node existingNode, Node addingNode)
    {
        var groupNode = new Node(GroupOperator, existingNode.Parent);
        groupNode.AddNode(existingNode);
        groupNode.AddNode(addingNode);
        
        existingNode.Parent = groupNode;
        addingNode.Parent = groupNode;

        return groupNode;
    }

    private bool IsParentAlternationNode(Node node)
    {
        return node.Parent != null && IsAlternationNode(node.Parent);
    }

    private bool IsAlternationNode(Node node)
    {
        return node.Value == AlternationOperator;
    }
    
    private void SetState(StateType state)
    {
        _state = state;
    }
    
    private string GetGroupExpressionString(string expression, ref int index)
    {
        if (expression[index] != '(')
        {
            throw new ArgumentException("Expected opening parenthesis '(' at the current position.");
        }

        int balance = 1;
        int startIndex = ++index;

        while (balance > 0 && index < expression.Length)
        {
            char currentChar = expression[index];

            if (currentChar == '(')
            {
                balance++;
            }
            else if (currentChar == ')')
            {
                balance--;
            }

            index++;
        }

        --index;

        if (balance != 0)
        {
            throw new ArgumentException("Mismatched parentheses in the expression.");
        }

        return expression.Substring(startIndex, index - startIndex);
    }
    
    private static void PrintNode(Node node, int depth)
    {
        Console.WriteLine($"{new string(' ', depth)} --> {node.Value}");

        foreach (var child in node.Children)
        {
            PrintNode(child, depth + 2);
        }
    }
}
namespace regex_to_nfa;

public enum StateType
{
    Alternation,
    Quantifier,
    Group,
    Symbol,
    None
};

public class Node
{
    public string Value;
    public List<Node> Children;
    public Node? Parent;
    public StateType Type;

    public Node(string value, Node? parent, StateType type)
    {
        Value = value;
        Children = new List<Node>();
        Parent = parent;
        Type = type;
    }

    public Node? LastNode
    {
        get => Children.Count > 0? Children[^1] : this;
        set => Children[^1] = value!;
    }
    
    public bool IsEmpty() => Children.Count == 0;
    
    public void AddNode(Node node)
    {
        Children.Add(node);
    }
}
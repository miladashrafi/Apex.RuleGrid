namespace RuleGrid.Models;

public class Rule
{
    public string Index { get; set; }
    public Dictionary<string, string> Conditions { get; set; }
    public Dictionary<string, string> Actions { get; set; }
}



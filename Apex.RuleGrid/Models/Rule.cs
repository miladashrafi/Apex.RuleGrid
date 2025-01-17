using System.Text.Json;

namespace Apex.RuleGrid.Models;

public class Rule
{
    public string Index { get; set; }
    public Dictionary<string, string> Conditions { get; set; }
    public Dictionary<string, string> Actions { get; set; }


    public static bool EvaluateCondition(string operatorPhrase, JsonElement prop, string conditionValue)
    {
        return operatorPhrase switch
        {
            "GreaterThan" => int.Parse(prop.GetRawText()) > int.Parse(conditionValue),
            "LowerThan" => int.Parse(prop.GetRawText()) < int.Parse(conditionValue),
            "Equals" => prop.ToString() == conditionValue,
            _ => false,
        };
    }
}



using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Apex.RuleGrid.Models;

public class RuleSetDbModel
{

    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; }
    public MetaData Metadata { get; set; }
    public List<Rule> Rules { get; set; }


    public static string GetRuleField(List<Rule> rules, string key, string index)
    {
        if (key.StartsWith("Condition_", StringComparison.InvariantCultureIgnoreCase))
        {
            return rules.FirstOrDefault(r => r.Index == index)?.Conditions[key] ?? string.Empty;
        }
        if (key.StartsWith("Action_", StringComparison.InvariantCultureIgnoreCase))
        {
            return rules.FirstOrDefault(r => r.Index == index)?.Actions[key] ?? string.Empty;
        }
        return string.Empty;
    }
}



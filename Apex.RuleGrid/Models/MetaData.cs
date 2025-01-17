using MongoDB.Bson.Serialization.Attributes;

namespace Apex.RuleGrid.Models;
public class MetaData
{
    [BsonId]
    public string Id { get; set; }
    public string Name { get; set; }
    public string ClassName { get; set; }
    public string GeneralAction { get; set; }
    public string ConditionsOperator { get; set; }
    public int Priority { get; set; }
}



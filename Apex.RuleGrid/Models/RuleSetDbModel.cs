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
}



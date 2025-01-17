using MongoDB.Bson;
using MongoDB.Driver;
using RuleGrid.Models;

namespace RuleGrid;
public class MongoDbService
{
    private readonly IMongoCollection<RuleSetDbModel> _collection;

    public MongoDbService(IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("MongoDB:ConnectionString");
        var databaseName = configuration["MongoDB:DatabaseName"];

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<RuleSetDbModel>("RuleSet");
    }

    public async Task SaveJsonAsync(RuleSetDbModel inputJson)
    {
        var existingMetadata = await _collection.Find(x => x.Metadata.Id == inputJson.Metadata.Id).FirstOrDefaultAsync();
        if (existingMetadata != null)
        {
            existingMetadata.Metadata = inputJson.Metadata;
            existingMetadata.Rules = inputJson.Rules;
            await _collection.ReplaceOneAsync(x => x.Metadata.Id == inputJson.Metadata.Id, existingMetadata, new ReplaceOptions { IsUpsert = true });
        }
        else
        {
            await _collection.InsertOneAsync(inputJson);
        }
    }

    public async Task<IList<RuleSetDbModel>> GetRulesAsync(string ClassName)
    {
        var filter = Builders<RuleSetDbModel>.Filter.Regex(x => x.Metadata.ClassName, new BsonRegularExpression(ClassName, "i"));
        var existingMetadata = await _collection.Find(filter).ToListAsync();

        return existingMetadata;
    }

}
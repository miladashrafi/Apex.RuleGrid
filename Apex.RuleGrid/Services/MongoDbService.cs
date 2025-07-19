using Apex.RuleGrid.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Apex.RuleGrid.Services;
public class MongoDbService
{
    private readonly IMongoCollection<RuleSetDbModel> _collection;
    private readonly ILogger _logger;

    public MongoDbService(IConfiguration configuration, ILogger logger)
    {
        _logger = logger.ForContext<MongoDbService>();
        
        var connectionString = configuration.GetValue<string>("MongoDB:ConnectionString");
        var databaseName = configuration["MongoDB:DatabaseName"];

        _logger.Information("Initializing MongoDB service with database {DatabaseName}", databaseName);

        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);
        _collection = database.GetCollection<RuleSetDbModel>("RuleSet");
        
        _logger.Debug("MongoDB service initialized successfully");
    }

    public async Task SaveJsonAsync(RuleSetDbModel inputJson)
    {
        _logger.Information("Saving rule set {RuleSetId} with {RuleCount} rules", 
            inputJson.Metadata.Id, inputJson.Rules.Count);
            
        try
        {
            var existingMetadata = await _collection.Find(x => x.Metadata.Id == inputJson.Metadata.Id).FirstOrDefaultAsync();
            if (existingMetadata != null)
            {
                _logger.Information("Updating existing rule set {RuleSetId}", inputJson.Metadata.Id);
                existingMetadata.Metadata = inputJson.Metadata;
                existingMetadata.Rules = inputJson.Rules;
                await _collection.ReplaceOneAsync(x => x.Metadata.Id == inputJson.Metadata.Id, existingMetadata, new ReplaceOptions { IsUpsert = true });
            }
            else
            {
                _logger.Information("Inserting new rule set {RuleSetId}", inputJson.Metadata.Id);
                await _collection.InsertOneAsync(inputJson);
            }
            
            _logger.Debug("Successfully saved rule set {RuleSetId}", inputJson.Metadata.Id);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to save rule set {RuleSetId}", inputJson.Metadata.Id);
            throw;
        }
    }

    public async Task<IList<RuleSetDbModel>> GetRulesAsync(string ClassName)
    {
        _logger.Information("Retrieving rules for class {ClassName}", ClassName);
        
        try
        {
            var filter = Builders<RuleSetDbModel>.Filter.Regex(x => x.Metadata.ClassName, new BsonRegularExpression(ClassName, "i"));
            var existingMetadata = await _collection.Find(filter).ToListAsync();

            _logger.Information("Found {RuleSetCount} rule sets for class {ClassName}", 
                existingMetadata.Count, ClassName);
                
            return existingMetadata;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to retrieve rules for class {ClassName}", ClassName);
            throw;
        }
    }

}
using Apex.RuleGrid.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Apex.RuleGrid.Exceptions;
using System.Text.Json;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Apex.RuleGrid.Services
{

    public class RuleEngineService
    {
        private readonly MongoDbService _dbService;
        private readonly ReteNetwork _reteNetwork;
        private readonly ILogger _logger;

        public RuleEngineService(MongoDbService dbService, ILogger logger)
        {
            _dbService = dbService;
            _reteNetwork = new ReteNetwork();
            _logger = logger.ForContext<RuleEngineService>();
            
            _logger.Debug("RuleEngineService initialized");
        }
        private static string ConvertExcelToJson(XLWorkbook workbook)
        {
            var result = new Dictionary<string, object>();

            foreach (var worksheet in workbook.Worksheets)
            {
                foreach (var table in worksheet.Tables)
                {
                    var headers = table.Fields.Select(f => f.Name).ToList();
                    var tableData = table.DataRange.Rows()
                        .Select(row => headers
                            .Select((header, index) => new { header, value = row.Cell(index + 1).Value.ToString() })
                            .ToDictionary(item => item.header, item => (object)item.value))
                        .ToList();

                    result[worksheet.Name] = worksheet.Name == "Metadata" ? tableData.FirstOrDefault() : tableData;
                }
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        private static RuleSetDbModel ConvertToDbModel(string inputJson)
        {
            using var document = JsonDocument.Parse(inputJson);
            var root = document.RootElement;

            var options = new JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                WriteIndented = true
            };

            var output = new RuleSetDbModel
            {
                Metadata = JsonSerializer.Deserialize<MetaData>(root.GetProperty("Metadata").GetRawText(), options),
                Rules = root.GetProperty("Rules").EnumerateArray().Select(rule => new Rule
                {
                    Index = rule.GetProperty("Index").GetString(),
                    Conditions = rule.EnumerateObject()
                                   .Where(p => p.Name.StartsWith("Condition_"))
                                   .ToDictionary(p => p.Name, p => p.Value.GetString()),
                    Actions = rule.EnumerateObject()
                                .Where(p => p.Name.StartsWith("Action_"))
                                .ToDictionary(p => p.Name, p => p.Value.GetString())
                }).ToList()
            };
            output.Rules = output.Rules.Where(x => x.Actions.Any(a => string.IsNullOrWhiteSpace(a.Value) == false)).ToList();
            var index = 1;
            foreach (var item in output.Rules.SkipWhile(x => x.Index?.Contains("#") is true))
            {
                item.Index = index.ToString();
                index++;
            }
            return output;
        }

        public async Task UploadRuleSet([FromForm] IList<IFormFile> files)
        {
            _logger.Information("Processing {FileCount} rule set files", files.Count);
            
            foreach (var file in files)
            {
                if (file == null || file.Length == 0)
                {
                    _logger.Warning("Invalid file encountered: {FileName}, Length: {FileLength}", 
                        file?.FileName ?? "null", file?.Length ?? 0);
                    throw new RuleGridException("Invalid file.");
                }

                _logger.Information("Processing Excel file {FileName} with size {FileSize} bytes", 
                    file.FileName, file.Length);

                try
                {
                    using var stream = file.OpenReadStream();
                    using var workbook = new XLWorkbook(stream);
                    
                    _logger.Debug("Successfully loaded Excel workbook from {FileName}", file.FileName);
                    
                    var json = ConvertExcelToJson(workbook);
                    _logger.Debug("Converted Excel to JSON for {FileName}", file.FileName);
                    
                    var dbModel = ConvertToDbModel(json);
                    _logger.Information("Converted to database model. RuleSetId: {RuleSetId}, RuleCount: {RuleCount}",
                        dbModel.Metadata.Id, dbModel.Rules.Count);
                    
                    _reteNetwork.AddRuleSet(dbModel);
                    _logger.Debug("Added rule set to RETE network for {RuleSetId}", dbModel.Metadata.Id);
                    
                    await _dbService.SaveJsonAsync(dbModel);
                    _logger.Information("Successfully processed and saved rule set from {FileName}", file.FileName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process file {FileName}", file.FileName);
                    throw;
                }
            }
            
            _logger.Information("Successfully processed all {FileCount} rule set files", files.Count);
        }

        public async Task<IList<JsonElement>> ApplyRulesWithRete(RuleApplicationRequest request)
        {
            _logger.Information("Applying rules using RETE algorithm for {ClassName} to {ObjectCount} objects", 
                request.ClassName, request.Objects.Count);
                
            try
            {
                var ruleSets = await _dbService.GetRulesAsync(request.ClassName);
                if (!ruleSets.Any())
                {
                    _logger.Warning("No rule sets found for class {ClassName}", request.ClassName);
                    return request.Objects;
                }

                _logger.Information("Found {RuleSetCount} rule sets for {ClassName}", 
                    ruleSets.Count, request.ClassName);

                var results = new List<JsonElement>();

                foreach (var ruleSet in ruleSets)
                {
                    _reteNetwork.AddRuleSet(ruleSet);
                    _logger.Debug("Added rule set {RuleSetId} to RETE network", ruleSet.Metadata.Id);

                    foreach (var obj in request.Objects)
                    {
                        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(obj.GetRawText());
                        if (jsonObject == null) continue;

                        var matchedRules = _reteNetwork.Match(jsonObject, ruleSet.Metadata.ConditionsOperator);
                        _logger.Debug("RETE matched {MatchedRuleCount} rules for object in rule set {RuleSetId}", 
                            matchedRules.Count(), ruleSet.Metadata.Id);
                            
                        foreach (var rule in matchedRules)
                        {
                            ApplyActions(ruleSet, rule, jsonObject, obj);
                            _logger.Debug("Applied RETE-matched rule {RuleIndex} from rule set {RuleSetId}", 
                                rule.Index, ruleSet.Metadata.Id);
                        }

                        results.Add(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(jsonObject)));
                    }
                }

                _logger.Information("Successfully applied rules using RETE for {ClassName}. Processed {ObjectCount} objects", 
                    request.ClassName, request.Objects.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to apply rules using RETE for {ClassName}", request.ClassName);
                throw;
            }
        }


        public async Task<IList<JsonElement>> ApplyRules([FromBody] RuleApplicationRequest request)
        {
            _logger.Information("Applying rules for {ClassName} to {ObjectCount} objects", 
                request.ClassName, request.Objects.Count);
                
            try
            {
                var ruleSets = await _dbService.GetRulesAsync(request.ClassName);
                if (!ruleSets.Any())
                {
                    _logger.Warning("No rule sets found for class {ClassName}", request.ClassName);
                    return request.Objects;
                }

                _logger.Information("Found {RuleSetCount} rule sets for {ClassName}", 
                    ruleSets.Count, request.ClassName);

                var results = new List<JsonElement>();

                foreach (var ruleSet in ruleSets)
                {
                    var filteredRules = ruleSet.Rules.Where(r => r.Index?.Contains("#") == false);
                    _logger.Debug("Processing {RuleCount} rules for rule set {RuleSetId}", 
                        filteredRules.Count(), ruleSet.Metadata.Id);

                    foreach (var obj in request.Objects)
                    {
                        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(obj.GetRawText());
                        if (jsonObject == null) continue;

                        int appliedRuleCount = 0;
                        foreach (var rule in filteredRules)
                        {
                            if (EvaluateConditions(ruleSet, rule, jsonObject, obj))
                            {
                                ApplyActions(ruleSet, rule, jsonObject, obj);
                                appliedRuleCount++;
                                _logger.Debug("Applied rule {RuleIndex} from rule set {RuleSetId}", 
                                    rule.Index, ruleSet.Metadata.Id);
                            }
                        }

                        if (appliedRuleCount > 0)
                        {
                            _logger.Debug("Applied {AppliedRuleCount} rules to object in rule set {RuleSetId}", 
                                appliedRuleCount, ruleSet.Metadata.Id);
                        }

                        results.Add(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(jsonObject)));
                    }
                }

                _logger.Information("Successfully applied rules for {ClassName}. Processed {ObjectCount} objects", 
                    request.ClassName, request.Objects.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to apply rules for {ClassName}", request.ClassName);
                throw;
            }
        }

        private bool EvaluateConditions(RuleSetDbModel ruleSet, Rule rule, Dictionary<string, object> jsonObject, JsonElement obj)
        {
            var conditionsOperator = ruleSet.Metadata.ConditionsOperator;
            var conditionMet = conditionsOperator == "AND";

            foreach (var condition in rule.Conditions)
            {
                var fieldName = RuleSetDbModel.GetRuleField(ruleSet.Rules, condition.Key, "#FieldName");
                if (string.IsNullOrWhiteSpace(fieldName) || !obj.TryGetProperty(fieldName, out var prop))
                {
                    conditionMet = false;
                    continue;
                }

                var operatorPhrase = RuleSetDbModel.GetRuleField(ruleSet.Rules, condition.Key, "#Operator");
                var conditionValue = condition.Value;

                conditionMet = conditionsOperator == "AND"
                    ? conditionMet && Rule.EvaluateCondition(operatorPhrase, prop, conditionValue)
                    : conditionMet || Rule.EvaluateCondition(operatorPhrase, prop, conditionValue);
            }

            return conditionMet;
        }

        private void ApplyActions(RuleSetDbModel ruleSet, Rule rule, Dictionary<string, object> jsonObject, JsonElement obj)
        {
            foreach (var action in rule.Actions)
            {
                var fieldName = RuleSetDbModel.GetRuleField(ruleSet.Rules, action.Key, "#FieldName");
                if (string.IsNullOrWhiteSpace(fieldName) || !obj.TryGetProperty(fieldName, out var prop))
                    continue;

                var operatorPhrase = RuleSetDbModel.GetRuleField(ruleSet.Rules, action.Key, "#Operator");
                var actionValue = action.Value;

                switch (operatorPhrase)
                {
                    case "Set":
                        jsonObject[fieldName] = ParseValue(prop, actionValue);
                        break;
                    case "Increase":
                        jsonObject[fieldName] = long.Parse(prop.GetRawText()) + long.Parse(actionValue);
                        break;
                    case "Decrease":
                        jsonObject[fieldName] = long.Parse(prop.GetRawText()) - long.Parse(actionValue);
                        break;
                }

                if (ruleSet.Metadata.GeneralAction == "SetAppliedRules")
                {
                    List<string> appliedRules = [];
                    if (jsonObject.ContainsKey("AppliedRules"))
                    {
                        appliedRules = jsonObject["AppliedRules"] as List<string>;
                    }
                    appliedRules.Add($"RuleId:{ruleSet.Metadata.Id} RuleIndex:{rule.Index}");
                    jsonObject["AppliedRules"] = appliedRules.Distinct().ToList();
                }
            }
        }

        private static object ParseValue(JsonElement prop, string value)
        {
            return prop.ValueKind switch
            {
                JsonValueKind.True or JsonValueKind.False => bool.Parse(value),
                _ => value
            };
        }
    }
}

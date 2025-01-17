using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RuleEngine.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text.Json;

namespace RuleEngine.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RuleEngineController : ControllerBase
{
    private readonly MongoDbService _dbService;

    public RuleEngineController(MongoDbService dbService)
    {
        _dbService = dbService;
    }
    public static string ConvertExcelToJson(XLWorkbook workbook)
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

    public static RuleSetDbModel ConvertToDbModel(string inputJson)
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

    [HttpPost("upload-ruleset")]
    public async Task<IActionResult> UploadRuleSet([FromForm] IList<IFormFile> files)
    {
        foreach (var file in files)
        {
            if (file == null || file.Length == 0) return BadRequest("Invalid file.");

            using var stream = file.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var json = ConvertExcelToJson(workbook);
            var dbModel = ConvertToDbModel(json);
            await _dbService.SaveJsonAsync(dbModel);
        }

        return Ok("RuleSet uploaded successfully.");
    }

    [HttpPost("apply-rules")]
    [SwaggerRequestExample(typeof(RuleApplicationRequest), typeof(RuleApplicationRequest))]
    public async Task<IActionResult> ApplyRules([FromBody] RuleApplicationRequest request)
    {
        // Fetch rules
        var ruleSets = await _dbService.GetRulesAsync(request.ClassName);
        if (!ruleSets.Any()) return Ok(request.Objects);

        var results = new List<object>();

        foreach (var ruleSet in ruleSets)
        {
            var filteredRules = ruleSet.Rules.Where(r => r.Index?.Contains("#") == false);

            foreach (var obj in request.Objects)
            {
                var jsonObject = JsonSerializer.Deserialize<Dictionary<string, object>>(obj.GetRawText());
                if (jsonObject == null) continue;

                foreach (var rule in filteredRules)
                {
                    if (EvaluateConditions(ruleSet, rule, jsonObject, obj))
                    {
                        ApplyActions(ruleSet, rule, jsonObject, obj);
                    }
                }

                results.Add(JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(jsonObject)));
            }
        }

        return Ok(results);
    }

    private bool EvaluateConditions(RuleSetDbModel ruleSet, Rule rule, Dictionary<string, object> jsonObject, JsonElement obj)
    {
        var conditionsOperator = ruleSet.Metadata.ConditionsOperator;
        var conditionMet = conditionsOperator == "AND";

        foreach (var condition in rule.Conditions)
        {
            var fieldName = GetRuleField(ruleSet, condition.Key, "#FieldName");
            if (string.IsNullOrWhiteSpace(fieldName) || !obj.TryGetProperty(fieldName, out var prop))
                continue;

            var operatorPhrase = GetRuleField(ruleSet, condition.Key, "#Operator");
            var conditionValue = condition.Value;

            conditionMet = conditionsOperator == "AND"
                ? conditionMet && EvaluateCondition(operatorPhrase, prop, conditionValue)
                : conditionMet || EvaluateCondition(operatorPhrase, prop, conditionValue);
        }

        return conditionMet;
    }

    private void ApplyActions(RuleSetDbModel ruleSet, Rule rule, Dictionary<string, object> jsonObject, JsonElement obj)
    {
        foreach (var action in rule.Actions)
        {
            var fieldName = GetRuleField(ruleSet, action.Key, "#FieldName");
            if (string.IsNullOrWhiteSpace(fieldName) || !obj.TryGetProperty(fieldName, out var prop))
                continue;

            var operatorPhrase = GetRuleField(ruleSet, action.Key, "#Operator");
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
        }
    }

    private static string GetRuleField(RuleSetDbModel ruleSet, string key, string index)
    {
        if (key.StartsWith("Condition_", StringComparison.InvariantCultureIgnoreCase))
        {
            return ruleSet.Rules.FirstOrDefault(r => r.Index == index)?.Conditions[key] ?? string.Empty;
        }
        if (key.StartsWith("Action_", StringComparison.InvariantCultureIgnoreCase))
        {
            return ruleSet.Rules.FirstOrDefault(r => r.Index == index)?.Actions[key] ?? string.Empty;
        }
        return string.Empty;
    }

    private static bool EvaluateCondition(string operatorPhrase, JsonElement prop, string conditionValue)
    {
        return operatorPhrase switch
        {
            "GreaterThan" => int.Parse(prop.GetRawText()) > int.Parse(conditionValue),
            "LowerThan" => int.Parse(prop.GetRawText()) < int.Parse(conditionValue),
            "Equals" => prop.ToString() == conditionValue,
            _ => false,
        };
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

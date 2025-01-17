using Swashbuckle.AspNetCore.Filters;
using System.Text.Json;

namespace RuleEngine.Models;

public class RuleApplicationRequest : IExamplesProvider<RuleApplicationRequest>
{

    public string ClassName { get; set; }
    public IList<JsonElement> Objects { get; set; }

    public RuleApplicationRequest GetExamples()
    {
        return new RuleApplicationRequest
        {
            ClassName = "AvailableFlight",
            Objects = new List<JsonElement>{
                {   JsonSerializer.Deserialize<JsonElement>(@"{
                        ""Origin"": ""THR"",
                        ""Destination"": ""MHD"",
                        ""MaxPrice"": null,
                        ""MaxPriceSet"": false
                    }")
                },
                {
                    JsonSerializer.Deserialize<JsonElement>(@"{
                        ""Origin"": ""THR"",
                        ""Destination"": ""ISF"",
                        ""MaxPrice"": null,
                        ""MaxPriceSet"": false
                    }")
                }
            }
        };
    }
}



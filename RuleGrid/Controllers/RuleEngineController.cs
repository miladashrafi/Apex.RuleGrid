using Microsoft.AspNetCore.Mvc;
using RuleGrid.Models;
using RuleGrid.Services;
using Swashbuckle.AspNetCore.Filters;

namespace RuleGrid.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RuleEngineController(RuleEngineService ruleEngineService) : ControllerBase
{
    [HttpPost("upload-ruleset")]
    public async Task<IActionResult> UploadRuleSet([FromForm] IList<IFormFile> files)
    {
        await ruleEngineService.UploadRuleSet(files);

        return Ok("RuleSet uploaded successfully.");
    }

    [HttpPost("apply-rules")]
    [SwaggerRequestExample(typeof(RuleApplicationRequest), typeof(RuleApplicationRequest))]
    public async Task<IActionResult> ApplyRules([FromBody] RuleApplicationRequest request)
    {
        return Ok(await ruleEngineService.ApplyRules(request));
    }
}

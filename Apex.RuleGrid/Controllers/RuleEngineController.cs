using Apex.RuleGrid.Models;
using Apex.RuleGrid.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;

namespace Apex.RuleGrid.Controllers;

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

    [HttpPost("apply-rules-rete")]
    [SwaggerRequestExample(typeof(RuleApplicationRequest), typeof(RuleApplicationRequest))]
    public async Task<IActionResult> ApplyRulesRete([FromBody] RuleApplicationRequest request)
    {
        return Ok(await ruleEngineService.ApplyRulesWithRete(request));
    }
}

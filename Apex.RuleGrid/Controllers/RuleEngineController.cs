using Apex.RuleGrid.Models;
using Apex.RuleGrid.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Filters;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Apex.RuleGrid.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RuleEngineController(RuleEngineService ruleEngineService, ILogger logger) : ControllerBase
{
    private readonly ILogger _logger = logger.ForContext<RuleEngineController>();

    [HttpPost("upload-ruleset")]
    public async Task<IActionResult> UploadRuleSet([FromForm] IList<IFormFile> files)
    {
        _logger.Information("Starting rule set upload for {FileCount} files", files.Count);
        
        try
        {
            await ruleEngineService.UploadRuleSet(files);
            
            _logger.Information("Successfully uploaded {FileCount} rule set files", files.Count);
            return Ok("RuleSet uploaded successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to upload rule set files. FileCount: {FileCount}", files.Count);
            throw;
        }
    }

    [HttpPost("apply-rules")]
    [SwaggerRequestExample(typeof(RuleApplicationRequest), typeof(RuleApplicationRequest))]
    public async Task<IActionResult> ApplyRules([FromBody] RuleApplicationRequest request)
    {
        _logger.Information("Applying rules for {ClassName} to {ObjectCount} objects", 
            request.ClassName, request.Objects.Count);
            
        try
        {
            var result = await ruleEngineService.ApplyRules(request);
            
            _logger.Information("Successfully applied rules for {ClassName}. Processed {ObjectCount} objects", 
                request.ClassName, request.Objects.Count);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply rules for {ClassName}. ObjectCount: {ObjectCount}", 
                request.ClassName, request.Objects.Count);
            throw;
        }
    }

    [HttpPost("apply-rules-rete")]
    [SwaggerRequestExample(typeof(RuleApplicationRequest), typeof(RuleApplicationRequest))]
    public async Task<IActionResult> ApplyRulesRete([FromBody] RuleApplicationRequest request)
    {
        _logger.Information("Applying rules using RETE algorithm for {ClassName} to {ObjectCount} objects", 
            request.ClassName, request.Objects.Count);
            
        try
        {
            var result = await ruleEngineService.ApplyRulesWithRete(request);
            
            _logger.Information("Successfully applied rules using RETE for {ClassName}. Processed {ObjectCount} objects", 
                request.ClassName, request.Objects.Count);
                
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to apply rules using RETE for {ClassName}. ObjectCount: {ObjectCount}", 
                request.ClassName, request.Objects.Count);
            throw;
        }
    }
}

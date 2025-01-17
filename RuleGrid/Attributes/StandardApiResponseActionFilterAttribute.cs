using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RuleGrid.Exceptions;
using RuleGrid.Models;
using System.Net;

namespace RuleGrid.Attributes;

public class StandardApiResponseActionFilterAttribute(ILogger<StandardApiResponseActionFilterAttribute> logger) : ActionFilterAttribute
{
    public override void OnActionExecuted(ActionExecutedContext context)
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var actionId = context.ActionDescriptor.Id;

        if (context.Result is ViewResult)
            return;

        if (context.Exception is not null)
            return;

        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new StandardApiModel
            {
                Status = (int)HttpStatusCode.BadRequest,
                TraceId = traceId,
                ValidationErrors = context.ModelState.ToDictionary(x => x.Key, x => x.Value?.Errors?.Select(s => s.ErrorMessage).ToArray() ?? []),
                ErrorType = ResponseErrorType.ValidationError,
                ActionId = actionId
            });
            context.ExceptionHandled = true;
            context.HttpContext.Response.Headers.Append("Result-Standardized", "true");
            base.OnActionExecuted(context);
            return;
        }

        if (context.Result is UnauthorizedResult || context.Result is UnauthorizedObjectResult)
        {
            context.Result = new UnauthorizedObjectResult(new StandardApiModel
            {
                Result = context.Result is ObjectResult oo ? oo.Value : null,
                Status = (int)HttpStatusCode.Unauthorized,
                TraceId = traceId,
                ActionId = actionId
            });
        }
        else
        {
            context.Result = new OkObjectResult(new StandardApiModel
            {
                Result = context.Result is ObjectResult or ? or.Value : null,
                Status = (int)HttpStatusCode.OK,
                TraceId = traceId,
                ActionId = actionId
            });
        }

        context.HttpContext.Response.Headers.Append("Result-Standardized", "true");

        base.OnActionExecuted(context);
    }

}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SmartGym.Api.Middleware;

namespace SmartGym.Api.Validators;

public static class ValidationInfrastructure
{
    public static IActionResult CreateValidationProblemResponse(ActionContext context)
    {
        var correlationId = context.HttpContext.Items[CorrelationIdMiddleware.CorrelationIdItemKey]?.ToString()
            ?? context.HttpContext.TraceIdentifier;

        var errors = context.ModelState
            .Where(x => x.Value != null && x.Value.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "One or more validation errors occurred.",
            Detail = "Please refer to the errors property for detailed validation failures.",
            Instance = context.HttpContext.Request.Path
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    }
}

public class ValidateModelAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = ValidationInfrastructure.CreateValidationProblemResponse(context);
        }
    }
}

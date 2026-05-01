using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Sstv.DomainExceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Sstv.Host;

public class SwaggerErrorCodesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is not ControllerActionDescriptor controllerAction)
        {
            return;
        }

        var controllerType = controllerAction.ControllerTypeInfo.FullName;
        var methodName = controllerAction.ActionName;
        var key = controllerType + "." + methodName;

        if (!ErrorCodeMethodCollector.ErrorCodesByMethod.TryGetValue(key, out var errorCodes) || errorCodes.Count == 0)
        {
            return;
        }

        var groupedByStatus = errorCodes
            .Select(c =>
            {
                var errorDesc = DomainExceptionSettings.Instance.ErrorCodesDescriptionSource?.GetDescription(c.Code);
                var statusCode = errorDesc != null
                    ? ErrorCodeMapping.MapToStatusCode(errorDesc)
                    : 500;
                return (c.Code, statusCode, errorDesc?.Description);
            })
            .GroupBy(x => x.statusCode)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kvp in groupedByStatus)
        {
            var statusCode = kvp.Key;
            var errors = kvp.Value;

            var description = string.Join("\n", errors.Select(t =>
                string.IsNullOrEmpty(t.Description)
                    ? $"- {t.Code}"
                    : $"- {t.Code}: {t.Description}"));

            var response = operation.Responses.TryGetValue(statusCode.ToString(), out var existing)
                ? existing
                : new OpenApiResponse();

            if (string.IsNullOrEmpty(response.Description))
            {
                response.Description = description;
            }
            else
            {
                response.Description = response.Description + "\n\n" + description;
            }

            operation.Responses[statusCode.ToString()] = response;
        }
    }
}
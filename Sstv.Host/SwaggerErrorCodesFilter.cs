using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Sstv.DomainExceptions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Sstv.Host;

internal sealed class SwaggerErrorCodesFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        if (!TryGetErrorCodes(context, operation, out var errorCodes) || errorCodes.Count == 0)
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

        foreach (var (statusCode, errors) in groupedByStatus)
        {
            var description = string.Join("\n", errors.Select(t =>
                string.IsNullOrEmpty(t.Description)
                    ? $"- {t.Code}"
                    : $"- {t.Code}: {t.Description}"));

            var response = operation.Responses.TryGetValue(statusCode.ToString(CultureInfo.InvariantCulture), out var existing)
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

            operation.Responses[statusCode.ToString(CultureInfo.InvariantCulture)] = response;
        }
    }

    private static bool TryGetErrorCodes(
        OperationFilterContext context,
        OpenApiOperation operation,
        [NotNullWhen(returnValue: true)] out HashSet<ErrorCodeSource>? errorCodes
    )
    {
        // Controller action: key = FullTypeName.ActionName
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor controllerAction)
        {
            var key = controllerAction.ControllerTypeInfo.FullName + "." + controllerAction.ActionName;
            if (ErrorCodeMethodCollector.ErrorCodesByMethod.TryGetValue(key, out errorCodes))
            {
                return true;
            }
        }

        // Minimal API endpoint: key = .WithName() value (operationId)
        if (!string.IsNullOrEmpty(operation.OperationId) &&
            ErrorCodeMethodCollector.ErrorCodesByMethod.TryGetValue(operation.OperationId, out errorCodes))
        {
            return true;
        }

        errorCodes = null;
        return false;
    }
}
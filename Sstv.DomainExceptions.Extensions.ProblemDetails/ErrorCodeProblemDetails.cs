using System.Collections;
using System.Text.Json.Serialization;

namespace Sstv.DomainExceptions.Extensions.ProblemDetails;

/// <summary>
/// Response in problem details format.
/// </summary>
public sealed class ErrorCodeProblemDetails : Microsoft.AspNetCore.Mvc.ProblemDetails
{
    /// <summary>
    /// Error code.
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Creates new instance of <see cref="ErrorCodeProblemDetails"/>.
    /// </summary>
    /// <param name="errorDescription">Description of error.</param>
    public ErrorCodeProblemDetails(ErrorDescription errorDescription)
    {
        ArgumentNullException.ThrowIfNull(errorDescription);

        Title = errorDescription.Description;
        Code = errorDescription.ErrorCode;
        Type = !string.IsNullOrWhiteSpace(errorDescription.HelpLink)
            ? errorDescription.HelpLink
            : null;
    }

    /// <summary>
    /// Creates new instance of <see cref="ErrorCodeProblemDetails"/>.
    /// </summary>
    /// <param name="domainException">Domain exception.</param>
    public ErrorCodeProblemDetails(DomainException domainException)
        : this(domainException?.GetDescription()!)
    {
        ArgumentNullException.ThrowIfNull(domainException);

        Detail = domainException.DetailedMessage;

        foreach (DictionaryEntry e in domainException.Data)
        {
            var stringKey = e.Key as string ?? e.Key.ToString();

            if (!string.IsNullOrWhiteSpace(stringKey) && e.Value is not null)
            {
                stringKey = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(stringKey);

                Extensions.TryAdd(stringKey, e.Value);
            }
        }
    }
}
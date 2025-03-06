using Sstv.DomainExceptions;
using System.Collections.Frozen;

namespace Sstv.Host;

public static class ErrorCodeMapping
{
    private static readonly FrozenDictionary<string, int> _statusCodeMap = new Dictionary<string, int>
    {
        // 2xx not an error
        [ErrorCodes.NotEnoughMoney.GetErrorCode()] = StatusCodes.Status200OK,

        // 4xx

        // 5xx
    }.ToFrozenDictionary();

    public static int MapToStatusCode(ErrorDescription errorDescription)
    {
        ArgumentNullException.ThrowIfNull(errorDescription);

        if (_statusCodeMap.TryGetValue(errorDescription.ErrorCode, out var statusCode))
        {
            return statusCode;
        }

        return errorDescription.Level == Level.NotError
            ? StatusCodes.Status200OK
            : StatusCodes.Status500InternalServerError;
    }
}
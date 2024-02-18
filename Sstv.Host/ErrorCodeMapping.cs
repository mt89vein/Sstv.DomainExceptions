using System.Collections.Frozen;

namespace Sstv.Host;

public static class ErrorCodeMapping
{
    private static readonly FrozenDictionary<string, int> _statusCodeMap = new Dictionary<string, int>
    {
        // 2xx not errror
        [ErrorCodes.NotEnoughMoney.GetErrorCode()] = StatusCodes.Status200OK,

        // 4xx

        // 5xx
    }.ToFrozenDictionary();

    public static int MapToStatusCode(string errorCode)
    {
        ArgumentNullException.ThrowIfNull(errorCode);

        return _statusCodeMap.GetValueOrDefault(errorCode, StatusCodes.Status500InternalServerError);
    }
}
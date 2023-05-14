using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.DomainExceptions.DebugViewer;

/// <summary>
/// Domain exception debug view model.
/// </summary>
[ExcludeFromCodeCoverage]
[DebuggerDisplay("{Code} {Message}")]
public class DomainExceptionCodeDebugVm
{
    /// <summary>
    /// Exception type info.
    /// </summary>
    public string ExceptionType { get; set; } = null!;

    /// <summary>
    /// Assembly, where the source code of exception.
    /// </summary>
    public string AssemblyName { get; set; } = null!;

    /// <summary>
    /// Domain exception enum type (if any).
    /// </summary>
    public string? ErrorCodesEnumType { get; set; }

    /// <summary>
    /// Full error code with prefix.
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Help link address.
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// Error message for user, without user-specific data.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Is error code obsolete.
    /// </summary>
    public bool IsObsolete { get; set; }

    /// <summary>
    /// Any additional data to error code.
    /// </summary>
    [SuppressMessage("Usage", "CA2227:CA2227: Collection properties should be read only")]
    public Dictionary<string, object>? AdditionalData { get; set; }
}
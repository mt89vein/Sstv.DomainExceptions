namespace Sstv.DomainExceptions;

/// <summary>
/// Error criticality levels.
/// </summary>
public enum Level
{
    /// <summary>
    /// No information.
    /// </summary>
    Undefined = 0,

    /// <summary>
    /// Not an error.
    /// </summary>
    NotError = 1,

    /// <summary>
    /// Low level.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Medium level.
    /// </summary>
    Medium = 3,

    /// <summary>
    /// High level.
    /// </summary>
    High = 4,

    /// <summary>
    /// Critical level.
    /// </summary>
    Critical = 5,

    /// <summary>
    /// Fatal level.
    /// </summary>
    Fatal = 6
}
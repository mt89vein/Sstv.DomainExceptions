using System.Diagnostics.CodeAnalysis;

namespace Sstv.DomainExceptions;

/// <summary>Source kind of error code.</summary>
public enum ErrorCodeSourceType
{
    /// <summary>Error code from enum.</summary>
    Enum,
    /// <summary>Error code from constant.</summary>
    Constant
}

/// <summary>Describes error code source.</summary>
public sealed class ErrorCodeSource : IEquatable<ErrorCodeSource>
{
    /// <summary>Error code value.</summary>
    public string Code { get; }

    /// <summary>Source type.</summary>
    public ErrorCodeSourceType SourceType { get; }

    /// <summary>Type that declares this error code.</summary>
    public Type? ErrorType { get; }

    /// <summary>Creates instance.</summary>
    public ErrorCodeSource(string code, ErrorCodeSourceType sourceType, Type? errorType = null)
    {
        Code = code;
        SourceType = sourceType;
        ErrorType = errorType;
    }

    /// <inheritdoc />
    public bool Equals(ErrorCodeSource? other) =>
        other != null && Code == other.Code && SourceType == other.SourceType;

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as ErrorCodeSource);

    /// <inheritdoc />
    [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
    public override int GetHashCode()
    {
        unchecked
        {
            return ((Code?.GetHashCode() ?? 0) * 397) ^ (int)SourceType;
        }
    }
}

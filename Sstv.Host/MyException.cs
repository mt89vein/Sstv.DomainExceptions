using Sstv.DomainExceptions;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.Host;

public static class DomainErrorCodes
{
    public const string DEFAULT = "SSTV.10000";
    public const string NOT_ENOUGH_MONEY = "SSTV.10004";
    public const string SOMETHING_BAD_HAPPEN = "SSTV.10005";
}

[SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class MyException : DomainException
{
    public MyException(string errorCode, Exception? innerException = null)
        : base(errorCode, innerException)
    {
    }
}
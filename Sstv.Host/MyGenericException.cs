using Sstv.DomainExceptions;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.Host;

[ErrorDescription(Prefix = "SSTV.", HelpLink = "https://help.myproject.ru/error-codes/{0}")]
public enum DomainErrorCodesEnum
{
    [ErrorDescription(
        Description = "Unhandled error code",
        HelpLink = "https://help.myproject.ru/error-codes/nothing-here")]
    Default = 0,

    [ErrorDescription(
        Description = "You have not enough money",
        HelpLink = "https://help.myproject.ru/error-codes/not-enough-money")]
    NotEnoughMoney = 10001,

    [ErrorDescription(Prefix = "DIF.", Description = "Another prefix example")]
    SomethingBadHappen = 10002,

    [Obsolete("Don't use this error code because it obsolete :)", true)]
    [ErrorDescription(Prefix = "DIF.", Description = "Obsolete error code in enum")]
    ObsoleteErrorCode = 10003,
}

[SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public sealed class MyGenericException : DomainException<DomainErrorCodesEnum>
{
    public MyGenericException(DomainErrorCodesEnum errorCode, Exception? innerException = null)
        : base(errorCode, innerException)
    {
    }
}
using Sstv.DomainExceptions;

namespace Sstv.Host;

[ErrorDescription(Prefix = "SSTV", HelpLink = "https://help.myproject.ru/error-codes/{0}")]
[ExceptionConfig(ClassName = "CoreException")]
public enum ErrorCodes
{
    [ErrorDescription(
        Description = "Unhandled error code",
        HelpLink = "https://help.myproject.ru/error-codes/nothing-here")]
    Default = 0,

    [ErrorDescription(
        Description = "You have not enough money",
        HelpLink = "https://help.myproject.ru/error-codes/not-enough-money")]
    NotEnoughMoney = 10001,

    [ErrorDescription(Prefix = "DIF", Description = "Another prefix example")]
    SomethingBadHappen = 10002,

    [Obsolete("Don't use this error code because it obsolete :)")]
    [ErrorDescription(Prefix = "DIF", Description = "Obsolete error code in enum")]
    ObsoleteErrorCode = 10003,

    [ErrorDescription(
        Description = "Help link with template in enum member attribute",
        HelpLink = "https://help.myproject.ru/{0}/error-code"
    )]
    WhateverElse = 10004,
}

[ErrorDescription(Prefix = "SDPV", HelpLink = "https://help.second-myproject.ru/error-codes/{0}")]
public enum SecondErrorCodes
{
    [ErrorDescription(
        Description = "Unhandled error code",
        HelpLink = "https://help.second-myproject.ru/error-codes/nothing-here")]
    Default = 0,

    [ErrorDescription(Description = "Something gets wrong")]
    SomethingGetsWrong = 10000,
}


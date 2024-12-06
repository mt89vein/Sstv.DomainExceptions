using Sstv.DomainExceptions;

namespace Sstv.Host;

[ErrorDescription(Prefix = "SSTV", HelpLink = "https://help.myproject.ru/error-codes/{0}", Level = Level.Critical)]
[ExceptionConfig(ClassName = "FirstException")]
public enum ErrorCodes
{
    [ErrorDescription(
        Description = "Unhandled error code",
        HelpLink = "https://help.myproject.ru/error-codes/nothing-here",
        Level = Level.Fatal)]
    Default = 0,

    [ErrorDescription(Description = "Invalid data", Level = Level.Critical)]
    InvalidData = 1,

    [ErrorDescription(
        Description = "You have not enough money",
        HelpLink = "https://help.myproject.ru/error-codes/not-enough-money",
        Level = Level.NotError)]
    NotEnoughMoney = 10001,

    [ErrorDescription(Prefix = "DIF", Description = "Another prefix example")]
    SomethingBadHappen = 10002,

    [ErrorDescription(
        Description = "Help link with template in enum member attribute",
        HelpLink = "https://help.myproject.ru/{0}/error-code"
    )]
    WhateverElse = 10003,
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


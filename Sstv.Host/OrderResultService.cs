using FluentResults;
using Sstv.Domain.Sample;

namespace Sstv.Host;

public class OrderResultService
{
    public object CreateOrderWithResult(string orderId)
    {
        return Result.Fail(new ErrorCodeResult(ErrorCodes.InvalidData));
    }

    public object CreateOrderWithConstant(string orderId)
    {
        return Result.Fail(new ErrorCodeResult(DomainErrorCodes.NOT_ENOUGH_MONEY));
    }

    public object CreateWithDifferentNames(string orderId)
    {
        return CustomResult.Failure(new MyError(ErrorCodes.SomethingBadHappen));
    }
}

public class CustomResult
{
    public static object Failure(MyError error) => new();
}

public class MyError
{
    public MyError(ErrorCodes code) { }
}

public class ErrorCodeResult : Error
{
    public ErrorCodeResult(string code) { }
    public ErrorCodeResult(ErrorCodes code) { }
}

public class Result
{
    public static Result Fail(ErrorCodeResult error) => new();
    public static Result<T> Ok<T>(T value) => new(value);
}

public class Result<T>
{
    public Result(T value) { }
}
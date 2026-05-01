using Sstv.DomainExceptions.Discovery;

namespace Sstv.Host;

public class OrderResultService
{
    [CollectErrorCodes]
    public object CreateOrderWithResult(string orderId)
    {
        return Result.Fail(new ErrorCodeResult(ErrorCodes.InvalidData));
    }

    [CollectErrorCodes]
    public object CreateOrderWithConstant(string orderId)
    {
        return Result.Fail(new ErrorCodeResult(DomainErrorCodes.NOT_ENOUGH_MONEY));
    }

    [CollectErrorCodes]
    public object CreateWithDifferentNames(string orderId)
    {
        return CustomResult.Failure(new MyError(ErrorCodes.SomethingBadHappen));
    }

    [CollectErrorCodes]
    public object CreateWithDifferentConstant(string orderId)
    {
        return SomeOtherResult.Fail(new Failure(DomainErrorCodes.DEFAULT));
    }
}

public class CustomResult
{
    public static object Failure(MyError error) => new();
}

public class MyError
{
    public MyError(ErrorCodes code) { }
    public MyError(string code) { }
}

public class SomeOtherResult
{
    public static object Fail(Failure error) => new();
}

public class Failure
{
    public Failure(string code) { }
}

public class ErrorCodeResult
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
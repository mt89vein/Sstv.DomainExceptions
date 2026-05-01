using Sstv.DomainExceptions.Discovery;

namespace Sstv.Host.Nested.Level1.Level2;

public class DeepNestedService
{
    private readonly DeepChildService _childService;

    public DeepNestedService(DeepChildService childService)
    {
        _childService = childService;
    }

    [CollectErrorCodes]
    public void ProcessDeep(string input)
    {
        _childService.ValidateDeep(input);
        _childService.ProcessChild(input);
    }

    [CollectErrorCodes]
    public void DirectThrow(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw ErrorCodes.InvalidData.ToException();
        }

        throw ErrorCodes.SomethingBadHappen.ToException();
    }
}

public class DeepChildService
{
    private readonly DeepGrandChildService _grandChild;

    public DeepChildService(DeepGrandChildService grandChild)
    {
        _grandChild = grandChild;
    }

    [CollectErrorCodes]
    public void ValidateDeep(string input)
    {
        if (input.Length < 3)
        {
            throw ErrorCodes.InvalidData.ToException();
        }

        _grandChild.CheckDeep(input);
    }

    [CollectErrorCodes]
    public void ProcessChild(string input)
    {
        throw new MyException(DomainErrorCodes.SOMETHING_BAD_HAPPEN);
    }
}

public class DeepGrandChildService
{
    [CollectErrorCodes]
    public void CheckDeep(string input)
    {
        if (input.Contains("error"))
        {
            throw ErrorCodes.Default.ToException();
        }

        throw ErrorCodes.SomethingBadHappen.ToException();
    }
}
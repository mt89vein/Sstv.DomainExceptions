using Sstv.Domain.Sample;

namespace Sstv.Host;

public class DeepNestedService
{
    private readonly DeepChildService _childService;

    public DeepNestedService(DeepChildService childService)
    {
        _childService = childService;
    }

    public void ProcessDeep(string input)
    {
        _childService.ValidateDeep(input);
        _childService.ProcessChild(input);
    }

    public void DirectThrow(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            throw SecondErrorCodes.SomethingGetsWrong.ToException();
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

    public void ValidateDeep(string input)
    {
        if (input.Length < 3)
        {
            throw ErrorCodes.InvalidData.ToException();
        }

        _grandChild.CheckDeep(input);
    }

    public void ProcessChild(string input)
    {
        throw new MyException(DomainErrorCodes.SOMETHING_BAD_HAPPEN);
    }
}

public class DeepGrandChildService
{
    public void CheckDeep(string input)
    {
        if (input.Contains("error"))
        {
            throw ErrorCodes.Default.ToException();
        }

        throw ErrorCodes.SomethingBadHappen.ToException();
    }
}
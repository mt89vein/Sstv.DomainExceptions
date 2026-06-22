namespace Sstv.Domain.Sample;

public class SampleService
{
    public void ThrowInAnotherAssembly()
    {
        throw new MyException("THROW_IN_ANOTHER_ASSEMBLY");
    }
}

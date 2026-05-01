using Microsoft.AspNetCore.Mvc;
using Sstv.DomainExceptions.Discovery;

namespace Sstv.Host;

public class MyController : ControllerBase
{
    [HttpGet("controller-example-1")]
    [CollectErrorCodes]
    public IActionResult Test()
    {
        return BadRequest("5");
    }

    [HttpGet("controller-example-2")]
    [CollectErrorCodes]
    public IActionResult OkNotTouched()
    {
        return Ok();
    }

    [HttpGet("controller-example-3")]
    [CollectErrorCodes]
    public IActionResult OkWithDataNotTouched()
    {
        return Ok(new { My = "Value" });
    }

    [HttpGet("controller-example-4")]
    [CollectErrorCodes]
    public IActionResult NoContentHasNoConvertion()
    {
        return NoContent();
    }

    [HttpGet("controller-example-5")]
    [CollectErrorCodes]
    public IActionResult UnauthorizedConverted()
    {
        return Unauthorized();
    }

    [HttpGet("controller-example-6")]
    [CollectErrorCodes]
    public IActionResult WithErrorCode()
    {
        throw new MyException(DomainErrorCodes.NOT_ENOUGH_MONEY)
            .WithErrorId()
            .WithDetailedMessage("DetailedError")
            .WithAdditionalData("123", 2);
    }

    [HttpGet("controller-example-7")]
    [CollectErrorCodes]
    public IActionResult WithGenericErrorCode()
    {
        throw ErrorCodes.SomethingBadHappen.ToException()
            .WithErrorId()
            .WithDetailedMessage("DetailedError")
            .WithAdditionalData("123", 2);
    }

    [HttpGet("controller-example-8")]
    [CollectErrorCodes]
    public IActionResult WithErrorPropagation()
    {
        throw new InvalidOperationException("DetailedError");
    }
}
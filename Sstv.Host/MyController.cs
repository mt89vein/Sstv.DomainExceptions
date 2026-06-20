using Microsoft.AspNetCore.Mvc;
using Sstv.Domain.Sample;

namespace Sstv.Host;

public class MyController : ControllerBase
{
    [HttpGet("controller-example-1")]
    public IActionResult Test()
    {
        return BadRequest("5");
    }

    [HttpGet("controller-example-2")]
    public IActionResult OkNotTouched()
    {
        return Ok();
    }

    [HttpGet("controller-example-3")]
    public IActionResult OkWithDataNotTouched()
    {
        return Ok(new { My = "Value" });
    }

    [HttpGet("controller-example-4")]
    public IActionResult NoContentHasNoConvertion()
    {
        return NoContent();
    }

    [HttpGet("controller-example-5")]
    public IActionResult UnauthorizedConverted()
    {
        return Unauthorized();
    }

    [HttpGet("controller-example-6")]
    public IActionResult WithErrorCode()
    {
        throw new MyException(DomainErrorCodes.NOT_ENOUGH_MONEY)
            .WithErrorId()
            .WithDetailedMessage("DetailedError")
            .WithAdditionalData("123", 2);
    }

    [HttpGet("controller-example-7")]
    public IActionResult WithGenericErrorCode()
    {
        throw ErrorCodes.SomethingBadHappen.ToException()
            .WithErrorId()
            .WithDetailedMessage("DetailedError")
            .WithAdditionalData("123", 2);
    }

    [HttpGet("controller-example-8")]
    public IActionResult WithErrorPropagation()
    {
        throw new InvalidOperationException("DetailedError");
    }

    [HttpGet("controller-example-9")]
    public IActionResult WithNamedArg()
    {
        throw new MyException(errorCode: "NAMED_ARG_CODE");
    }

    [HttpGet("controller-example-10")]
    public IActionResult WithNamedArgEnum()
    {
        throw new MyException(errorCode: DomainErrorCodes.SOMETHING_BAD_HAPPEN);
    }

    [HttpGet("controller-example-11")]
    public IActionResult WithLowercaseConst()
    {
        throw new MyException(DomainErrorCodes.notFound);
    }
}
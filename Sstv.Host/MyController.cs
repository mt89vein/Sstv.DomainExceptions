using Microsoft.AspNetCore.Mvc;

namespace Sstv.Host;

public class MyController : ControllerBase
{
    // BadRequest autocovertion enabled into problem details format
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
        // constants as error code
        throw new MyException(DomainErrorCodes.NOT_ENOUGH_MONEY)
            .WithErrorId()
            .WithDetailedMessage("DetailedError")
            .WithAdditionalData("123", 2);
    }

    [HttpGet("controller-example-7")]
    public IActionResult WithGenericErrorCode()
    {
        // enums as error code
        throw ErrorCodes.SomethingBadHappen.AsException()
            .WithErrorId()
            .WithDetailedMessage("DetailedError")
            .WithAdditionalData("123", 2);
    }

    [HttpGet("controller-example-8")]
    public IActionResult WithErrorPropagation()
    {
        throw new InvalidOperationException("DetailedError");
    }
}
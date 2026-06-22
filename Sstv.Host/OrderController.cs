using Microsoft.AspNetCore.Mvc;

namespace Sstv.Host.Controllers;

[Route("v1/orders")]
public class OrderController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly DeepNestedService _deepNestedService;

    public OrderController(IOrderService orderService, DeepNestedService deepNestedService)
    {
        _orderService = orderService;
        _deepNestedService = deepNestedService;
    }

    [HttpPost("{orderId}")]
    public IActionResult CreateOrder(string orderId)
    {
        _orderService.ProcessOrder(orderId);
        _deepNestedService.ProcessDeep(orderId);
        return Ok(new { OrderId = orderId, Status = "Processed" });
    }

    [HttpGet("test")]
    public IActionResult TestCollected()
    {
        _orderService.ProcessOrder("ORDER-123");
        return Ok();
    }

    [HttpGet("false-positive-uri-test1")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FalsePositiveTest1()
    {
        return Created(new Uri(Url.Action(
                action: "CreateOrder",
                values: new { orderId = "ORDER-123" }
            )!, UriKind.Relative),
            value: null
        );
    }

    [HttpGet("false-positive-uri-test2")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FalsePositiveTest2()
    {
        return new OkObjectResult(Guid.NewGuid());
    }

    [HttpGet("false-positive-obj-creation")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FalsePositiveObjectCreation()
    {
        var now = DateTime.UtcNow;
        return Ok(new Version(now.Year, now.Month, now.Day));
    }

    [HttpGet("false-positive-throw-arg")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FalsePositiveThrowArg()
    {
        var configMode = Environment.GetEnvironmentVariable("MODE");

        throw new InvalidOperationException(
            $"Invalid mode: {configMode}"
        );
    }

    [HttpGet("false-positive-mixed")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult FalsePositiveMixed()
    {
        return Ok(new UriBuilder(
            Uri.UriSchemeHttp,
            "localhost",
            8080
        ));
    }
}
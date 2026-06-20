using Microsoft.AspNetCore.Mvc;

namespace Sstv.Host.Controllers;

[Route("v1/orders")]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly DeepNestedService _deepNestedService;

    public OrderController(OrderService orderService, DeepNestedService deepNestedService)
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
}
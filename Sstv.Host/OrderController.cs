using Microsoft.AspNetCore.Mvc;
using Sstv.DomainExceptions.Discovery;
using Sstv.Host.Nested.Level1.Level2;

namespace Sstv.Host.Controllers;

public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly DeepNestedService _deepNestedService;

    public OrderController(OrderService orderService, DeepNestedService deepNestedService)
    {
        _orderService = orderService;
        _deepNestedService = deepNestedService;
    }

    [HttpPost("orders/{orderId}")]
    [CollectErrorCodes]
    public IActionResult CreateOrder(string orderId)
    {
        _orderService.ProcessOrder(orderId);
        _deepNestedService.ProcessDeep(orderId);
        return Ok(new { OrderId = orderId, Status = "Processed" });
    }

    [HttpGet("orders/test")]
    [CollectErrorCodes]
    public IActionResult TestCollected()
    {
        _orderService.ProcessOrder("ORDER-123");
        return Ok();
    }

    [HttpGet("orders/non-collected")]
    [CollectErrorCodes]
    public IActionResult TestNonCollected()
    {
        _orderService.NonCollectedMethod();
        return Ok();
    }
}
using Microsoft.AspNetCore.Mvc;

namespace Sstv.Host.Controllers;

[Route("v2/orders")]
public class OrderResultController : ControllerBase
{
    private readonly OrderResultService _orderService;

    public OrderResultController(OrderResultService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("{orderId}")]
    public IActionResult CreateOrder(string orderId)
    {
        _orderService.CreateOrderWithConstant(orderId);
        return Ok(new { OrderId = orderId, Status = "Processed" });
    }

    [HttpGet("test")]
    public IActionResult TestCollected(string orderId)
    {
        _orderService.CreateOrderWithResult(orderId);
        _orderService.CreateWithDifferentNames(orderId);
        return Ok();
    }
}
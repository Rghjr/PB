using Attractions.Data;
using Attractions.DTOs.Api;
using Attractions.Services;
using Microsoft.AspNetCore.Mvc;

namespace Attractions.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly OfferExtensionService _extensionService;

    public OrdersController(OrderService orderService, OfferExtensionService extensionService)
    {
        _orderService = orderService;
        _extensionService = extensionService;
    }

    [HttpGet]
    public ActionResult<List<OrderDto>> GetAllOrders()
    {
        lock (InMemoryData.SyncRoot)
        {
            var orders = _orderService.GetAllOrders()
                .Select(ApiMapper.ToDto)
                .ToList();

            return Ok(orders);
        }
    }

    [HttpGet("{id:int}")]
    public ActionResult<OrderDto> GetOrderById(int id)
    {
        lock (InMemoryData.SyncRoot)
        {
            var order = _orderService.GetOrderById(id);
            if (order is null)
            {
                return NotFound(new ApiErrorResponse($"Zamówienie o ID {id} nie istnieje."));
            }

            return Ok(ApiMapper.ToDto(order));
        }
    }

    [HttpPost]
    public ActionResult<OrderDto> PlaceOrder([FromBody] PlaceOrderApiRequest request)
    {
        lock (InMemoryData.SyncRoot)
        {
            var buildRes = ApiMapper.ReconstructConfiguration(request.Configuration, _extensionService);
            if (!buildRes.IsSuccess || buildRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(buildRes.Message));
            }

            var breakdown = ApiMapper.ToDomain(request.GroupBreakdown);
            var profile = request.Profile is not null ? ApiMapper.ToDomain(request.Profile) : null;

            var orderRes = _orderService.PlaceOrder(
                buildRes.Data,
                request.CustomerName,
                request.CustomerEmail,
                breakdown,
                request.PaymentMethod,
                profile);

            if (!orderRes.IsSuccess || orderRes.Data is null)
            {
                return BadRequest(new ApiErrorResponse(orderRes.Message));
            }

            var dto = ApiMapper.ToDto(orderRes.Data);
            return CreatedAtAction(nameof(GetOrderById), new { id = dto.Id }, dto);
        }
    }
}

using Bpn.ECommerce.Application.Features.Balance.Commands;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bpn.ECommerce.WebAPI.Abstractions;
using Bpn.ECommerce.Domain.Entities;
using FluentValidation;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Infrastructure.Services;
using Bpn.ECommerce.Application.Features.Balance.Notifications;

namespace Bpn.ECommerce.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ApiController
    {
        private readonly IOrderCalculationService _orderCalculationService;
        public OrdersController(IMediator mediator, IOrderCalculationService orderCalculationService) : base(mediator)
        {
            _orderCalculationService = orderCalculationService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePreOrder([FromBody] List<OrderItem> orderList, CancellationToken cancellationToken)
        {
            if (orderList == null)
            {
                return BadRequest("Order list can not be null");
            }

          
            //duplicatae check valditore taşınacak
            var productIdSet = new HashSet<Guid>();
            foreach (var item in orderList)
            {
                if (!productIdSet.Add(item.ProductId))
                {
                    return BadRequest($"Duplicate ProductId found: {item.ProductId}");
                }
            }
            decimal totalPrice = _orderCalculationService.CalculateTotalPrice(orderList);

            CreatePreOrderCommand command = new CreatePreOrderCommand
            {
                Amount = totalPrice,
                OrderId = Guid.NewGuid().ToString()
            };
            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            return StatusCode(result.StatusCode, result.ErrorMessages);
        }

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteOrder(string id, CancellationToken cancellationToken)
        {
            if (id != null)
            {
                return BadRequest("Order ID in the URL can not be null");
            }
            UpdatePreOrderCommand command = new UpdatePreOrderCommand
            {
                OrderId = id
            };

            var result = await _mediator.Send(command, cancellationToken);
            if (result.IsSuccessful)
            {
                return Ok(result);
            }
            //Complete order başarısız olması durumunda ödemeyi geri al
            await _mediator.Publish(new PaymentFailedNotification { OrderId = id }, cancellationToken);
            return StatusCode(result.StatusCode, result.ErrorMessages);
        }
    }
}
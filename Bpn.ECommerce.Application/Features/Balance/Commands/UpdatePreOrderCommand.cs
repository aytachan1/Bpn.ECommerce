using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Generic.Result;
using MediatR;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class UpdatePreOrderCommand : IRequest<Result<PreOrderResponse>>
    {
        public string? OrderId { get; set; }
    }
}

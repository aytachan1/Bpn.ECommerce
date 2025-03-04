using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Generic.Result;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class RemovePreOrderCommandHandler : IRequestHandler<RemovePreOrderCommand, Result<PreOrderResponse>>
    {
        private readonly IBalanceManagementService _balanceManagementService;

        public RemovePreOrderCommandHandler(IBalanceManagementService balanceManagementService)
        {
            _balanceManagementService = balanceManagementService;
        }

        public async Task<Result<PreOrderResponse>> Handle(RemovePreOrderCommand request, CancellationToken cancellationToken)
        {
            var removePreOrderRequest = new PreOrderRequest
            {
                OrderId = request.OrderId
            };

            return await _balanceManagementService.RemovePreOrderAsync(removePreOrderRequest);
        }
    }
}
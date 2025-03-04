using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Generic.Result;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class UpdatePreOrderCommandHandler : IRequestHandler<UpdatePreOrderCommand, Result<PreOrderResponse>>
    {
        private readonly IBalanceManagementService _balanceManagementService;

        public UpdatePreOrderCommandHandler(IBalanceManagementService balanceManagementService)
        {
            _balanceManagementService = balanceManagementService;
        }

        public async Task<Result<PreOrderResponse>> Handle(UpdatePreOrderCommand request, CancellationToken cancellationToken)
        {
            var updatePreOrderRequest = new PreOrderRequest
            {
                OrderId = request.OrderId
            };

            return await _balanceManagementService.UpdatePreOrderAsync(updatePreOrderRequest);
        }
    }
}
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Commands
{
    public class CreatePreOrderCommandHandler : IRequestHandler<CreatePreOrderCommand, Result<PreOrderResponse>>
    {
        private readonly IBalanceManagementService _balanceManagementService;

        public CreatePreOrderCommandHandler(IBalanceManagementService balanceManagementService)
        {
            _balanceManagementService = balanceManagementService;
        }

        public async Task<Result<PreOrderResponse>> Handle(CreatePreOrderCommand request, CancellationToken cancellationToken)
        {
            var preOrderRequest = new CreatePreOrderRequest
            {
                Amount = request.Amount,
                OrderId = request.OrderId
            };

            return await _balanceManagementService.CreatePreOrderAsync(preOrderRequest);
        }
    }
}

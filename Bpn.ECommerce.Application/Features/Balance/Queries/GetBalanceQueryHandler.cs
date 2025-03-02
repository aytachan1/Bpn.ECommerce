﻿using MediatR;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Queries
{
    public class GetBalanceQueryHandler : IRequestHandler<GetBalanceQuery, BalanceResponse>
    {
        private readonly IBalanceManagementService _balanceManagementService;

        public GetBalanceQueryHandler(IBalanceManagementService balanceManagementService)
        {
            _balanceManagementService = balanceManagementService;
        }

        public async Task<BalanceResponse> Handle(GetBalanceQuery request, CancellationToken cancellationToken)
        {
            var balance = await _balanceManagementService.GetBalanceAsync();
            return balance.Data ?? new BalanceResponse();
        }
    }
}
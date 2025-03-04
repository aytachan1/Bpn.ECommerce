using MediatR;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Product;

namespace Bpn.ECommerce.Application.Features.Product.Queries
{
    public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ProductResponse>
    {
        private readonly IBalanceManagementService _balanceManagementService;

        public GetProductsQueryHandler(IBalanceManagementService balanceManagementService)
        {
            _balanceManagementService = balanceManagementService;
        }

        public async Task<ProductResponse> Handle(GetProductsQuery request, CancellationToken cancellationToken)
        {
            var products = await _balanceManagementService.GetProductsAsync();
            return products?.Data ?? new ProductResponse();
        }
    }
}

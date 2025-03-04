using MediatR;
using Bpn.ECommerce.Domain.Entities.Product;

namespace Bpn.ECommerce.Application.Features.Product.Queries
{
    public class GetProductsQuery : IRequest<ProductResponse>
    {
    }
}

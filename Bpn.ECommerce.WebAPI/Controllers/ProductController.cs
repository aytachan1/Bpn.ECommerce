using Bpn.ECommerce.Application.Features.Product.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Bpn.ECommerce.WebAPI.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Bpn.ECommerce.Domain.Entities.Product;


namespace Bpn.ECommerce.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ApiController
    {
        public ProductsController(IMediator mediator) : base(mediator)
        {
        }

        [HttpGet]
        public async Task<ProductResponse> GetProducts(CancellationToken cancellationToken)
        {
            var query = new GetProductsQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (result.Success)
            {
                return result;
            }
            else
            {
                 throw new Exception("Error while fetching products");
            }
        }
    }
}

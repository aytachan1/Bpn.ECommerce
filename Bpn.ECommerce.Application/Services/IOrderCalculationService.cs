using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Services
{
    
    public interface IOrderCalculationService
    {
        Result<decimal> CalculateTotalPrice(List<OrderItem> orderList);
        Result<decimal> GetProductPriceById(Guid productId, int quantity);
        ProductEntity GetProductById(Guid productId);

    }
}

using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Infrastructure.Services
{
    public class OrderCalculationService : IOrderCalculationService
    {
        public decimal CalculateTotalPrice(List<OrderItem> orderList)
        {
            decimal totalPrice = 0;
            foreach (var item in orderList)
            {
                decimal productPrice = GetProductPriceById(item.ProductId, item.Quantity);
                totalPrice += productPrice;
            }
            return totalPrice;
        }

        private decimal GetProductPriceById(Guid productId, int quantity)
        {
            var product = GetProductById(productId);
            if (product == null)
            {
                throw new Exception("Product not found");
            }

            if (quantity > product.Stock)
            {
                throw new Exception("Requested quantity is not available in stock");
            }

            return product.Price * quantity;
        }

        private ProductEntity GetProductById(Guid productId)
        {
            // Implement this method to get the product by its ID
            // For example, you can query the database or call another service
            // Placeholder implementation
            return new ProductEntity
            {
                Id = productId,
                Price = 100, // Example price
                Stock = 10 // Example stock
            };
        }
    }

}

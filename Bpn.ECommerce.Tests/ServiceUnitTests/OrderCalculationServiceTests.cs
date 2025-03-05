using Bpn.ECommerce.Infrastructure.Services;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Entities.Product;
using Bpn.ECommerce.Domain.Generic.Result;
using System.Collections.Generic;
using Xunit;

namespace Bpn.ECommerce.UnitTests.ServiceUnitTests
{
    public class OrderCalculationServiceTests
    {
        private readonly OrderCalculationService _orderCalculationService;

        public OrderCalculationServiceTests()
        {
            _orderCalculationService = new OrderCalculationService();
        }

        [Fact]
        public void CalculateTotalPrice_ShouldReturnTotalPrice_WhenOrderListIsValid()
        {
            // Arrange
            var orderList = new List<OrderItem>
            {
                new OrderItem { ProductId = "1", Quantity = 2 },
                new OrderItem { ProductId = "2", Quantity = 3 }
            };

            // Act
            var result = _orderCalculationService.CalculateTotalPrice(orderList);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(500, result.Data); // Assuming each product price is 100
        }

        [Fact]
        public void CalculateTotalPrice_ShouldReturnFailure_WhenProductIdIsNull()
        {
            // Arrange
            var orderList = new List<OrderItem>
            {
                new OrderItem { ProductId = null, Quantity = 2 }
            };

            // Act
            var result = _orderCalculationService.CalculateTotalPrice(orderList);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Equal("ProductId cannot be null", result.ErrorMessages[0]);
        }

        [Fact]
        public void GetProductPriceById_ShouldReturnProductPrice_WhenProductExists()
        {
            // Act
            var result = _orderCalculationService.GetProductPriceById("1", 2);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(200, result.Data); // Assuming product price is 100
        }

        [Fact (Skip ="static oldugu icin atladim")]
        public void GetProductPriceById_ShouldReturnFailure_WhenProductNotFound()
        {
            // Act
            var result = _orderCalculationService.GetProductPriceById("invalid", 2);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Equal("Product not found", result.ErrorMessages[0]);
        }

        [Fact]
        public void GetProductPriceById_ShouldReturnFailure_WhenQuantityExceedsStock()
        {
            // Act
            var result = _orderCalculationService.GetProductPriceById("1", 20);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Equal("Requested quantity is not available in stock", result.ErrorMessages[0]);
        }

        [Fact]
        public void HasDuplicateProductIds_ShouldReturnTrue_WhenDuplicateProductIdsExist()
        {
            // Arrange
            var orderList = new List<OrderItem>
            {
                new OrderItem { ProductId = "1", Quantity = 2 },
                new OrderItem { ProductId = "1", Quantity = 3 }
            };

            // Act
            var result = _orderCalculationService.HasDuplicateProductIds(orderList, out string duplicateProductId);

            // Assert
            Assert.True(result);
            Assert.Equal("1", duplicateProductId);
        }

        [Fact]
        public void HasDuplicateProductIds_ShouldReturnFalse_WhenNoDuplicateProductIdsExist()
        {
            // Arrange
            var orderList = new List<OrderItem>
            {
                new OrderItem { ProductId = "1", Quantity = 2 },
                new OrderItem { ProductId = "2", Quantity = 3 }
            };

            // Act
            var result = _orderCalculationService.HasDuplicateProductIds(orderList, out string duplicateProductId);

            // Assert
            Assert.False(result);
            Assert.Equal("", duplicateProductId);
        }
    }
}
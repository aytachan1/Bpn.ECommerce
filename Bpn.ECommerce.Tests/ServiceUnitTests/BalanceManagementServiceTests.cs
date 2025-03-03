using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using Bpn.ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bpn.ECommerce.Tests.Unit.ServiceUnitTests
{
    public class BalanceManagementServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<ILogger<BalanceManagementService>> _loggerMock;
        private readonly HttpClient _httpClient;
        private readonly BalanceManagementService _service;

        public BalanceManagementServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<BalanceManagementService>>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://balance-management-pi44.onrender.com")
            };
            _service = new BalanceManagementService(_httpClient, _loggerMock.Object);
        }

        [Fact]
        public async Task GetProductsAsync_ShouldReturnProductResponse_WhenProductsAreRetrievedSuccessfully()
        {
            // Arrange
            var productResponse = new ProductResponse
            {
                Success = true,
                Data = new List<ProductEntity>
                {
                    new ProductEntity { Id = Guid.NewGuid(), Name = "Product1", Price = 100 },
                    new ProductEntity { Id = Guid.NewGuid(), Name = "Product2", Price = 200 }
                }
            };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(productResponse))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _service.GetProductsAsync();

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(productResponse.Data.Count, result.Data.Data.Count);
            Assert.Equal(productResponse.Data[0].Name, result.Data.Data[0].Name);
            Assert.Equal(productResponse.Data[1].Name, result.Data.Data[1].Name);
        }

        [Fact]
        public async Task GetProductsAsync_ShouldReturnFailure_WhenProductRetrievalFails()
        {
            // Arrange
            var errorMessage = "Failed to retrieve products";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { Message = errorMessage }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _service.GetProductsAsync();

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains(errorMessage, result.ErrorMessages);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnBalanceResponse_WhenBalanceIsRetrievedSuccessfully()
        {
            // Arrange
            var balanceResponse = new BalanceResponse
            {
                Success = true,
                Data = new BalanceData { UserId = "testUserId", TotalBalance = 1000, AvailableBalance = 800, BlockedBalance = 200, Currency = "USD", LastUpdated = DateTime.UtcNow }
            };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(balanceResponse))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(balanceResponse.Data.UserId, result.Data.Data.UserId);
            Assert.Equal(balanceResponse.Data.TotalBalance, result.Data.Data.TotalBalance);
            Assert.Equal(balanceResponse.Data.AvailableBalance, result.Data.Data.AvailableBalance);
            Assert.Equal(balanceResponse.Data.BlockedBalance, result.Data.Data.BlockedBalance);
            Assert.Equal(balanceResponse.Data.Currency, result.Data.Data.Currency);
            Assert.Equal(balanceResponse.Data.LastUpdated, result.Data.Data.LastUpdated);
        }

        [Fact]
        public async Task GetBalanceAsync_ShouldReturnFailure_WhenBalanceRetrievalFails()
        {
            // Arrange
            var errorMessage = "Failed to retrieve balance";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { Message = errorMessage }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            // Act
            var result = await _service.GetBalanceAsync();

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains(errorMessage, result.ErrorMessages);
        }

        [Fact]
        public async Task CreatePreOrderAsync_ShouldReturnPreOrderResponse_WhenPreOrderIsCreatedSuccessfully()
        {
            // Arrange
            PreOrder preOrder = new PreOrder
            {
                OrderId = "testOrderId",
                Amount = 100
            };
            var preOrderResponse = new PreOrderResponse
            {
                Success = true,
                Data = new PreOrderData { PreOrder = preOrder }
            };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(preOrderResponse))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var request = new CreatePreOrderRequest
            {
                Amount = 100,
                OrderId = "testOrderId"
            };

            // Act
            var result = await _service.CreatePreOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(preOrderResponse.Data.PreOrder.OrderId, result.Data.Data.PreOrder.OrderId);
            Assert.Equal(preOrderResponse.Data.PreOrder.Amount, result.Data.Data.PreOrder.Amount);
        }

        [Fact]
        public async Task CreatePreOrderAsync_ShouldReturnFailure_WhenPreOrderCreationFails()
        {
            // Arrange
            var errorMessage = "Failed to create pre-order";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { Message = errorMessage }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var request = new CreatePreOrderRequest
            {
                Amount = 100,
                OrderId = "testOrderId"
            };

            // Act
            var result = await _service.CreatePreOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains(errorMessage, result.ErrorMessages);
        }

        [Fact]
        public async Task UpdatePreOrderAsync_ShouldReturnPreOrderResponse_WhenPreOrderIsUpdatedSuccessfully()
        {
            // Arrange
            PreOrder preOrder = new PreOrder
            {
                OrderId = "testOrderId",
                Amount = 100
            };
            var preOrderResponse = new PreOrderResponse
            {
                Success = true,
                Data = new PreOrderData {PreOrder = preOrder }
            };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(preOrderResponse))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var request = new PreOrderRequest
            {
                OrderId = "testOrderId"
            };

            // Act
            var result = await _service.UpdatePreOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(preOrderResponse.Data.PreOrder.OrderId, result.Data.Data.PreOrder.OrderId);
            Assert.Equal(preOrderResponse.Data.PreOrder.Amount, result.Data.Data.PreOrder.Amount);
        }

        [Fact]
        public async Task UpdatePreOrderAsync_ShouldReturnFailure_WhenPreOrderUpdateFails()
        {
            // Arrange
            var errorMessage = "Failed to update pre-order";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { Message = errorMessage }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var request = new PreOrderRequest
            {
                OrderId = "testOrderId"
            };

            // Act
            var result = await _service.UpdatePreOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains(errorMessage, result.ErrorMessages);
        }

        [Fact]
        public async Task RemovePreOrderAsync_ShouldReturnPreOrderResponse_WhenPreOrderIsRemovedSuccessfully()
        {
            // Arrange
            PreOrder preOrder = new PreOrder
            {
                OrderId = "testOrderId",
                Amount = 100
            };
            var preOrderResponse = new PreOrderResponse
            {
                Success = true,
                Data = new PreOrderData { PreOrder = preOrder }
            };
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(preOrderResponse))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var request = new PreOrderRequest
            {
                OrderId = "testOrderId"
            };

            // Act
            var result = await _service.RemovePreOrderAsync(request);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(preOrderResponse.Data.PreOrder.OrderId, result.Data.Data.PreOrder.OrderId);
            Assert.Equal(preOrderResponse.Data.PreOrder.Amount, result.Data.Data.PreOrder.Amount);
        }

        [Fact]
        public async Task RemovePreOrderAsync_ShouldReturnFailure_WhenPreOrderRemovalFails()
        {
            // Arrange
            var errorMessage = "Failed to remove pre-order";
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent(System.Text.Json.JsonSerializer.Serialize(new { Message = errorMessage }))
            };
            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponseMessage);

            var request = new PreOrderRequest
            {
                OrderId = "testOrderId"
            };

            // Act
            var result = await _service.RemovePreOrderAsync(request);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains(errorMessage, result.ErrorMessages);
        }
    }

}
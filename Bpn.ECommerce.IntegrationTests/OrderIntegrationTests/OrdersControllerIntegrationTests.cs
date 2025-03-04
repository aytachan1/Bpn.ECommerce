using Bpn.ECommerce.Application.Features.Balance.Queries;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Net.Http.Json;
using Xunit;

namespace Bpn.ECommerce.IntegrationTests.OrderIntegrationTests
{

    public class OrdersControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IBalanceManagementService> _balanceManagementServiceMock;
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<IOrderCalculationService> _orderCalculationServiceMock;

        public OrdersControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _balanceManagementServiceMock = new Mock<IBalanceManagementService>();
            _mediatorMock = new Mock<IMediator>();
            _orderCalculationServiceMock = new Mock<IOrderCalculationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton(_balanceManagementServiceMock.Object);
                    services.AddSingleton(_mediatorMock.Object);
                    services.AddSingleton(_orderCalculationServiceMock.Object);
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnSuccess()
        {
            // Arrange
            var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1 }
        };

            var preOrderResponse = Result<PreOrderResponse>.Succeed(new PreOrderResponse { Success = true });
            _balanceManagementServiceMock.Setup(service => service.CreatePreOrderAsync(It.IsAny<CreatePreOrderRequest>()))
                .ReturnsAsync(preOrderResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<PreOrderResponse>>();
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnBadRequestForEmptyOrderItems()
        {
            // Arrange
            var orderItems = new List<OrderItem>();

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnServiceFailure()
        {
            // Arrange
            var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1 }
        };

            var preOrderResponse = Result<PreOrderResponse>.Failure("Service error");
            _balanceManagementServiceMock.Setup(service => service.CreatePreOrderAsync(It.IsAny<CreatePreOrderRequest>()))
                .ReturnsAsync(preOrderResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            var result = await response.Content.ReadFromJsonAsync<Result<PreOrderResponse>>();
            Assert.False(result.IsSuccessful);
        }

        [Fact(Skip = "Bu gibi testler  ürünleri orders tablosuna kaydettiğim senaryoda ve bir kısmının kaydedilip bir kısmının kaydedilmediği senaryolar için. şuan db kullanmıyorum o yüzden geç")]
        public async Task CreateOrder_ShouldReturnPartialSuccess()
        {
            // Arrange
            var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1 },
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = 2 }
        };

            var preOrderResponse = Result<PreOrderResponse>.Succeed(new PreOrderResponse { Success = true });
            _balanceManagementServiceMock.SetupSequence(service => service.CreatePreOrderAsync(It.IsAny<CreatePreOrderRequest>()))
                .ReturnsAsync(preOrderResponse)
                .ReturnsAsync(Result<PreOrderResponse>.Failure("Partial failure"));

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<PreOrderResponse>>();
            Assert.True(result.IsSuccessful);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        public async Task CreateOrder_ShouldReturnSuccess_WithDifferentQuantities(int quantity)
        {
            // Arrange
            var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = quantity }
        };

            var preOrderResponse = Result<PreOrderResponse>.Succeed(new PreOrderResponse { Success = true });
            _balanceManagementServiceMock.Setup(service => service.CreatePreOrderAsync(It.IsAny<CreatePreOrderRequest>()))
                .ReturnsAsync(preOrderResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<Result<PreOrderResponse>>();
            Assert.True(result.IsSuccessful);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnServiceFailure_whenInsufficientBalance()
        {
            // Arrange
            var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = 1 }
        };

            var balanceResponse = new BalanceResponse
            {
                Success = true,
                Data = new BalanceData
                {
                    AvailableBalance = 0 // Insufficient balance
                }
            };

            _mediatorMock.Setup(m => m.Send(It.IsAny<GetBalanceQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(balanceResponse);

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains("Insufficient balance to create pre-order.", result);
        }

        [Fact]
        public async Task CreateOrder_ShouldReturnServiceFailure_whenInsufficientStock()
        {
            // Arrange
            var orderItems = new List<OrderItem>
        {
            new OrderItem { ProductId = Guid.NewGuid(), Quantity = 10 }
        };

            var insufficientStockResult = Result<decimal>.Failure("Insufficient stock");
            _orderCalculationServiceMock.Setup(service => service.CalculateTotalPrice(It.IsAny<List<OrderItem>>()))
                .Returns(insufficientStockResult);

            // Act
            var response = await _client.PostAsJsonAsync("/api/orders/create", orderItems);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
            var result = await response.Content.ReadAsStringAsync();
            Assert.Contains("Insufficient stock", result);
        }
    }
}
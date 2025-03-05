using Bpn.ECommerce.Domain.Entities.Product;
using Bpn.ECommerce.Domain.Generic.Result;
using Bpn.ECommerce.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Polly;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bpn.ECommerce.UnitTests.ServiceUnitTests
{
    public class BalanceManagementServiceTests
    {
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly Mock<ILogger<BalanceManagementService>> _loggerMock;
        private readonly IMemoryCache _memoryCache;
        private readonly BalanceManagementService _service;

        public BalanceManagementServiceTests()
        {
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _loggerMock = new Mock<ILogger<BalanceManagementService>>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());

            var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("https://balance-management-pi44.onrender.com")
            };

            _service = new BalanceManagementService(
                httpClient,
                _loggerMock.Object,
                _memoryCache
            );
        }

        [Fact]
        public async Task GetProductsAsync_ReturnsCachedProducts_WhenCacheIsAvailable()
        {
            // Arrange
            var cacheKey = "products";
            var cachedProducts = Result<ProductResponse>.Succeed(new ProductResponse { Success = true });
            _memoryCache.Set(cacheKey, cachedProducts);

            // Act
            var result = await _service.GetProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsSuccessful);
            Assert.Equal(cachedProducts, result);
        }

        [Fact (Skip ="sonra bakilacak")]
        public async Task GetProductsAsync_ReturnsProductsFromApi_WhenCacheIsNotAvailable()
        {
            // Arrange
            var cacheKey = "products";
            List<ProductEntity> data = new List<ProductEntity>();
            data.Add(new ProductEntity { Id = "1", Name = "Product 1", Price = 100 });
            data.Add(new ProductEntity { Id = "2", Name = "Product 2", Price = 200 });
            var productResponse = new ProductResponse { Success = true,Data = data };
            var result = Result<ProductResponse>.Succeed(productResponse);

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(productResponse)
            };

            _httpMessageHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsNull<HttpRequestMessage>(),
                    ItExpr.IsNull<CancellationToken>()
                )
                .ReturnsAsync(responseMessage);

            // Act
            var actualResult = await _service.GetProductsAsync();

            // Assert
            Assert.NotNull(actualResult);
            Assert.True(actualResult.IsSuccessful);
            Assert.Equal(result.Data, actualResult.Data);
            Assert.True(_memoryCache.TryGetValue(cacheKey, out var cachedResult));
            Assert.Equal(result, cachedResult);
        }

      
    }
}
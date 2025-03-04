using Bpn.ECommerce.Application.Features.Product.Queries;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Product;
using Bpn.ECommerce.Domain.Generic.Result;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bpn.ECommerce.UnitTests.HandlerUnitTests
{
    public class GetProductsQueryHandlerTests
    {
        private readonly Mock<IBalanceManagementService> _balanceManagementServiceMock;
        private readonly GetProductsQueryHandler _handler;

        public GetProductsQueryHandlerTests()
        {
            _balanceManagementServiceMock = new Mock<IBalanceManagementService>();
            _handler = new GetProductsQueryHandler(_balanceManagementServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnProductResponse_WhenProductsAreRetrievedSuccessfully()
        {
            // Arrange
            var productList = new List<ProductEntity>
            {
                new ProductEntity { Id = "test123", Name = "Product1", Price = 100 },
                new ProductEntity { Id = "test12", Name = "Product2", Price = 200 }
            };
            var productResponse = new ProductResponse
            {
                Success = true,
                Data = productList
            };
            _balanceManagementServiceMock.Setup(service => service.GetProductsAsync())
                .ReturnsAsync(Result<ProductResponse>.Succeed(productResponse));

            var query = new GetProductsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(productList.Count, result.Data.Count);
            Assert.Equal(productList[0].Name, result.Data[0].Name);
            Assert.Equal(productList[1].Name, result.Data[1].Name);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyProductResponse_WhenNoProductsAreRetrieved()
        {
            // Arrange
            _balanceManagementServiceMock.Setup(service => service.GetProductsAsync())
                .ReturnsAsync(Result<ProductResponse>.Succeed(new ProductResponse { Success = true, Data = new List<ProductEntity>() }));

            var query = new GetProductsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Empty(result.Data);
        }

        [Fact(Skip ="Bu kisim icin balancemanagmentservice icinde test yapilcak yada queryhandler metodunu guncellemem lazzim exception donebilirim.")]
        public async Task Handle_ShouldReturnFailure_WhenProductRetrievalFails()
        {
            // Arrange
            var errorMessage = "Failed to retrieve products";
            _balanceManagementServiceMock.Setup(service => service.GetProductsAsync())
                .ReturnsAsync(Result<ProductResponse>.Failure(errorMessage));

            var query = new GetProductsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Failed to retrieve products",result.Data.ToString());
        }
    }
}
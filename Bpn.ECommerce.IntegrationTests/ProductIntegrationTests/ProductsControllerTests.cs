using System.Threading;
using System.Threading.Tasks;
using Bpn.ECommerce.WebAPI.Controllers;
using Bpn.ECommerce.Application.Features.Product.Queries;
using Bpn.ECommerce.Domain.Entities.Product;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Bpn.ECommerce.IntegrationTests.ProductIntegrationTests
{
    public class ProductsControllerTests
    {
        private readonly IMediator _mediator;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mediator = Substitute.For<IMediator>();
            _controller = new ProductsController(_mediator);
        }

        [Fact]
        public async Task GetProducts_ReturnsProductResponse_WhenSuccessful()
        {
            // Arrange
            var expectedResponse = new ProductResponse { Success = true };
            _mediator.Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(expectedResponse));

            // Act
            var result = await _controller.GetProducts(CancellationToken.None);

            // Assert
            Assert.Equal(expectedResponse, result);
        }

        [Fact]
        public async Task GetProducts_ThrowsException_WhenNotSuccessful()
        {
            // Arrange
            var expectedResponse = new ProductResponse { Success = false };
            _mediator.Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(expectedResponse));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _controller.GetProducts(CancellationToken.None));
        }
    }
}
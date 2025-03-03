using Bpn.ECommerce.Application.Features.Balance.Commands;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bpn.ECommerce.Tests.Unit.HandlerUnitTests
{
    public class RemovePreOrderCommandHandlerTests
    {
        private readonly Mock<IBalanceManagementService> _balanceManagementServiceMock;
        private readonly RemovePreOrderCommandHandler _handler;

        public RemovePreOrderCommandHandlerTests()
        {
            _balanceManagementServiceMock = new Mock<IBalanceManagementService>();
            _handler = new RemovePreOrderCommandHandler(_balanceManagementServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnPreOrderResponse_WhenPreOrderIsRemovedSuccessfully()
        {
            // Arrange
            var preOrder = new PreOrder
            {
                OrderId = "testOrderId",
                Amount = 100
            };
            var preOrderResponse = new PreOrderResponse
            {
                Success = true,
                Data = new PreOrderData { PreOrder = preOrder }
            };
            _balanceManagementServiceMock.Setup(service => service.RemovePreOrderAsync(It.IsAny<PreOrderRequest>()))
                .ReturnsAsync(Result<PreOrderResponse>.Succeed(preOrderResponse));

            var command = new RemovePreOrderCommand
            {
                OrderId = "testOrderId"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.True(result.IsSuccessful);
            Assert.Equal(preOrderResponse.Data.PreOrder.OrderId, result.Data.Data.PreOrder.OrderId);
            Assert.Equal(preOrderResponse.Data.PreOrder.Amount, result.Data.Data.PreOrder.Amount);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenPreOrderRemovalFails()
        {
            // Arrange
            _balanceManagementServiceMock.Setup(service => service.RemovePreOrderAsync(It.IsAny<PreOrderRequest>()))
                .ReturnsAsync(Result<PreOrderResponse>.Failure("Failed to remove pre-order"));

            var command = new RemovePreOrderCommand
            {
                OrderId = "testOrderId"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains("Failed to remove pre-order", result.ErrorMessages);
        }
    }
}
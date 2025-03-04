using Bpn.ECommerce.Application.Features.Balance.Commands;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Generic.Result;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.UnitTests.HandlerUnitTests
{
    public class CreatePreOrderCommandHandlerTests
    {
        private readonly Mock<IBalanceManagementService> _balanceManagementServiceMock;
        private readonly CreatePreOrderCommandHandler _handler;

        public CreatePreOrderCommandHandlerTests()
        {
            _balanceManagementServiceMock = new Mock<IBalanceManagementService>();
            _handler = new CreatePreOrderCommandHandler(_balanceManagementServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnPreOrderResponse_WhenPreOrderIsCreatedSuccessfully()
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
                Data = new PreOrderData { PreOrder=preOrder }
            };
            _balanceManagementServiceMock.Setup(service => service.CreatePreOrderAsync(It.IsAny<CreatePreOrderRequest>()))
                .ReturnsAsync(Result<PreOrderResponse>.Succeed(preOrderResponse));

            var command = new CreatePreOrderCommand
            {
                Amount = 100,
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
        public async Task Handle_ShouldReturnFailure_WhenPreOrderCreationFails()
        {
            // Arrange
            _balanceManagementServiceMock.Setup(service => service.CreatePreOrderAsync(It.IsAny<CreatePreOrderRequest>()))
                .ReturnsAsync(Result<PreOrderResponse>.Failure("Failed to create pre-order"));

            var command = new CreatePreOrderCommand
            {
                Amount = 100,
                OrderId = "testOrderId"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.False(result.IsSuccessful);
            Assert.Contains("Failed to create pre-order", result.ErrorMessages);
        }
    }
}

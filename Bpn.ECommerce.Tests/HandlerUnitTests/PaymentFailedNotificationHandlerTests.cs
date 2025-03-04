using Bpn.ECommerce.Application.Features.Balance.Notifications;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bpn.ECommerce.UnitTests.HandlerUnitTests
{
    public class PaymentFailedNotificationHandlerTests
    {
        private readonly Mock<IBalanceManagementService> _balanceManagementServiceMock;
        private readonly PaymentFailedNotificationHandler _handler;

        public PaymentFailedNotificationHandlerTests()
        {
            _balanceManagementServiceMock = new Mock<IBalanceManagementService>();
            _handler = new PaymentFailedNotificationHandler(_balanceManagementServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldRemovePreOrder_WhenPaymentFails()
        {
            // Arrange
            var preOrder = new PreOrder
            {
                OrderId = "testOrderId",
            };
            var preOrderResponse = new PreOrderResponse
            {
                Success = true,
                Data = new PreOrderData { PreOrder = preOrder }
            };
            _balanceManagementServiceMock.Setup(service => service.RemovePreOrderAsync(It.IsAny<PreOrderRequest>()))
                .ReturnsAsync(Result<PreOrderResponse>.Succeed(preOrderResponse));

            var notification = new PaymentFailedNotification
            {
                OrderId = "testOrderId"
            };

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _balanceManagementServiceMock.Verify(service => service.RemovePreOrderAsync(It.Is<PreOrderRequest>(r => r.OrderId == "testOrderId")), Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldLogError_WhenPreOrderRemovalFails()
        {
            // Arrange
            _balanceManagementServiceMock.Setup(service => service.RemovePreOrderAsync(It.IsAny<PreOrderRequest>()))
                .ReturnsAsync(Result<PreOrderResponse>.Failure("Failed to remove pre-order"));

            var notification = new PaymentFailedNotification
            {
                OrderId = "testOrderId"
            };

            // Act
            await _handler.Handle(notification, CancellationToken.None);

            // Assert
            _balanceManagementServiceMock.Verify(service => service.RemovePreOrderAsync(It.Is<PreOrderRequest>(r => r.OrderId == "testOrderId")), Times.Once);
            // Here you can add additional assertions to verify logging or other actions taken on failure
        }
    }
}
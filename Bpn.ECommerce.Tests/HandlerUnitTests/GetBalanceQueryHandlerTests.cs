using Bpn.ECommerce.Application.Features.Balance.Queries;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Balance;
using Bpn.ECommerce.Domain.Generic.Result;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Bpn.ECommerce.UnitTests.HandlerUnitTests
{
    public class GetBalanceQueryHandlerTests
    {
        private readonly Mock<IBalanceManagementService> _balanceManagementServiceMock;
        private readonly GetBalanceQueryHandler _handler;

        public GetBalanceQueryHandlerTests()
        {
            _balanceManagementServiceMock = new Mock<IBalanceManagementService>();
            _handler = new GetBalanceQueryHandler(_balanceManagementServiceMock.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnBalanceResponse_WhenBalanceIsRetrievedSuccessfully()
        {
            // Arrange
            var balanceData = new BalanceData
            {
                UserId = "testUserId",
                TotalBalance = 1000,
                AvailableBalance = 800,
                BlockedBalance = 200,
                Currency = "USD",
                LastUpdated = DateTime.UtcNow
            };
            var balanceResponse = new BalanceResponse
            {
                Success = true,
                Data = balanceData
            };
            _balanceManagementServiceMock.Setup(service => service.GetBalanceAsync())
                .ReturnsAsync(Result<BalanceResponse>.Succeed(balanceResponse));

            var query = new GetBalanceQuery ();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(balanceData.UserId, result.Data.UserId);
            Assert.Equal(balanceData.TotalBalance, result.Data.TotalBalance);
            Assert.Equal(balanceData.AvailableBalance, result.Data.AvailableBalance);
            Assert.Equal(balanceData.BlockedBalance, result.Data.BlockedBalance);
            Assert.Equal(balanceData.Currency, result.Data.Currency);
            Assert.Equal(balanceData.LastUpdated, result.Data.LastUpdated);
        }

        [Fact]
        public async Task Handle_ShouldReturnFailure_WhenBalanceRetrievalFails()
        {
            // Arrange
            _balanceManagementServiceMock.Setup(service => service.GetBalanceAsync())
                .ReturnsAsync(Result<BalanceResponse>.Failure("Failed to retrieve balance"));

            var query = new GetBalanceQuery ();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.False(result.Success);
            Assert.Null(result.Data);
        }
    }
}


using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bpn.ECommerce.Application.Features.Balance.Commands;
using Bpn.ECommerce.Application.Features.Balance.Queries;
using Bpn.ECommerce.Application.Features.Balance.Notifications;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Infrastructure.Services;
using Bpn.ECommerce.WebAPI.Controllers;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Balance;
using Bpn.ECommerce.Domain.Generic.Result;

namespace Bpn.ECommerce.IntegrationTests.OrderIntegrationTests
{
    public class OrdersControllerTests
    {
        private readonly IMediator _mediator;
        private readonly IOrderCalculationService _orderCalculationService;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mediator = Substitute.For<IMediator>();
            _orderCalculationService = Substitute.For<IOrderCalculationService>();
            _controller = new OrdersController(_mediator, _orderCalculationService);
        }

        [Fact]
        public async Task CreatePreOrder_ReturnsBadRequest_WhenOrderListIsNull()
        {
            // Act
            var result = await _controller.CreatePreOrder(null, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Order list can not be null", badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePreOrder_ReturnsBadRequest_WhenDuplicateProductIdFound()
        {
            // Arrange
            var orderList = new List<OrderItem> { new OrderItem { ProductId = "1" }, new OrderItem { ProductId = "1" } };
            _orderCalculationService.HasDuplicateProductIds(orderList, out Arg.Any<string>()).Returns(true);

            // Act
            var result = await _controller.CreatePreOrder(orderList, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Duplicate ProductId found:", badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePreOrder_ReturnsBadRequest_WhenTotalPriceCalculationFails()
        {
            // Arrange
            var orderList = new List<OrderItem> { new OrderItem { ProductId = "1" } };
            _orderCalculationService.CalculateTotalPrice(orderList).Returns(new Result<decimal> { IsSuccessful = false, ErrorMessages = new List<string> { "Error" } });

            // Act
            var result = await _controller.CreatePreOrder(orderList, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(new List<string> { "Error" }, badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePreOrder_ReturnsStatusCode500_WhenBalanceRetrievalFails()
        {
            // Arrange
            var orderList = new List<OrderItem> { new OrderItem { ProductId = "1" } };
            _orderCalculationService.CalculateTotalPrice(orderList).Returns(new Result<decimal> { IsSuccessful = true, Data = 100 });
            _mediator.Send(Arg.Any<GetBalanceQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new BalanceResponse { Success = false }));

            // Act
            var result = await _controller.CreatePreOrder(orderList, CancellationToken.None);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("Failed to retrieve balance.", statusCodeResult.Value);
        }

        [Fact]
        public async Task CreatePreOrder_ReturnsBadRequest_WhenInsufficientBalance()
        {
            // Arrange
            var orderList = new List<OrderItem> { new OrderItem { ProductId = "1" } };
            _orderCalculationService.CalculateTotalPrice(orderList).Returns(new Result<decimal> { IsSuccessful = true, Data = 100 });
            _mediator.Send(Arg.Any<GetBalanceQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new BalanceResponse { Success = true, Data = new BalanceData { AvailableBalance = 50 } }));

            // Act
            var result = await _controller.CreatePreOrder(orderList, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Insufficient balance to create pre-order.", badRequestResult.Value);
        }

        [Fact]
        public async Task CreatePreOrder_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var orderList = new List<OrderItem> { new OrderItem { ProductId = "1" } };
            _orderCalculationService.CalculateTotalPrice(orderList).Returns(new Result<decimal> { IsSuccessful = true, Data = 100 });
            _mediator.Send(Arg.Any<GetBalanceQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new BalanceResponse { Success = true, Data = new BalanceData { AvailableBalance = 150 } }));
            _mediator.Send(Arg.Any<CreatePreOrderCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Result<PreOrderResponse> { IsSuccessful = true }));

            // Act
            var result = await _controller.CreatePreOrder(orderList, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(((Result<PreOrderResponse>)okResult.Value).IsSuccessful);
        }

        [Fact]
        public async Task CompleteOrder_ReturnsBadRequest_WhenOrderIdIsNull()
        {
            // Act
            var result = await _controller.CompleteOrder(null, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Order ID in the URL can not be null", badRequestResult.Value);
        }

        [Fact]
        public async Task CompleteOrder_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            _mediator.Send(Arg.Any<UpdatePreOrderCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Result<PreOrderResponse> { IsSuccessful = true }));

            // Act
            var result = await _controller.CompleteOrder("orderId", CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(((Result<PreOrderResponse>)okResult.Value).IsSuccessful);
        }

        [Fact]
        public async Task CompleteOrder_PublishesPaymentFailedNotification_WhenNotSuccessful()
        {
            // Arrange
            _mediator.Send(Arg.Any<UpdatePreOrderCommand>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(new Result<PreOrderResponse> { IsSuccessful = false }));

            // Act
            var result = await _controller.CompleteOrder("orderId", CancellationToken.None);

            // Assert
            await _mediator.Received().Publish(Arg.Any<PaymentFailedNotification>(), Arg.Any<CancellationToken>());
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            var resultValue = statusCodeResult.Value as Result<PreOrderResponse>;
            Assert.Null(resultValue);
           
            //Assert.False(((Result<PreOrderResponse>)statusCodeResult.Value).IsSuccessful);
        }
    }
}
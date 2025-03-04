using MediatR;
using Bpn.ECommerce.Application.Services;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Bpn.ECommerce.Domain.Entities;

namespace Bpn.ECommerce.Application.Features.Balance.Notifications
{
    public class PaymentFailedNotificationHandler : INotificationHandler<PaymentFailedNotification>
    {
        private readonly IBalanceManagementService _balanceManagementService;
        private readonly ILogger<PaymentFailedNotificationHandler> _logger;

        public PaymentFailedNotificationHandler(IBalanceManagementService balanceManagementService, ILogger<PaymentFailedNotificationHandler> logger)
        {
            _balanceManagementService = balanceManagementService;
            _logger = logger;
        }

        public async Task Handle(PaymentFailedNotification notification, CancellationToken cancellationToken)
        {
            var request = new PreOrderRequest { OrderId = notification.OrderId };
            var result = await _balanceManagementService.RemovePreOrderAsync(request);
            if (result.IsSuccessful)
            {
                _logger.LogInformation("Successfully removed pre-order with OrderId: {OrderId}", notification.OrderId);
                // Update the order status in the database
            }
            else
            {
                _logger.LogCritical("Failed to remove pre-order with OrderId: {OrderId}. Error: {ErrorMessages}", notification.OrderId, string.Join(", ", result.ErrorMessages ?? new List<string>() {"Hem ödemesi tamamlanmadı hemde bakiyedeki bloke kaldırılamadı. kritik bir hata olarak loglandı."}));
                // Burada tekrardan ödeme tamamlama işlemine yönlendirme yapılabilir.
            }
        }
    }
}
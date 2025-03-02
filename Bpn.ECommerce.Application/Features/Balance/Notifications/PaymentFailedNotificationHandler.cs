using MediatR;
using Bpn.ECommerce.Application.Services;
using System.Threading;
using System.Threading.Tasks;
using Bpn.ECommerce.Domain.Entities;

namespace Bpn.ECommerce.Application.Features.Balance.Notifications
{
    public class PaymentFailedNotificationHandler : INotificationHandler<PaymentFailedNotification>
    {
        private readonly IBalanceManagementService _balanceManagementService;

        public PaymentFailedNotificationHandler(IBalanceManagementService balanceManagementService)
        {
            _balanceManagementService = balanceManagementService;
        }

        public async Task Handle(PaymentFailedNotification notification, CancellationToken cancellationToken)
        {
            var request = new PreOrderRequest { OrderId = notification.OrderId };
           var result = await _balanceManagementService.RemovePreOrderAsync(request);
            if (result.IsSuccessful)
            {
                // Burada işlemi başarılı bir şekilde gerçekleştirdiğimizi loglamamız gerekiyor. ve database içinde orderın durumunu güncelememiz gerekiyor.
            }else
            { 
                // Burada işlemi başarısız bir şekilde gerçekleştirdiğimizi loglamamız gerekiyor. ayrıca işlem açıkta kaldığı için müşterinin tekrar deneme senaryosu ve açıkta kalan işlemin kuyuruklanması gibi işlemleri yapmamız gerekiyor.
            }
        }
        }
}
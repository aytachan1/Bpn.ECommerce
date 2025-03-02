using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Features.Balance.Notifications
{
    public class PaymentFailedNotification : INotification
    {
        public string? OrderId { get; set; }
    }
}

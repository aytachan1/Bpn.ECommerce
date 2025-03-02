using Bpn.ECommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Services
{
    public interface IOrderCalculationService
    {
        decimal CalculateTotalPrice(List<OrderItem> orderList);
    }
}

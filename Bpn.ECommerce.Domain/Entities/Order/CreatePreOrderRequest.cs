using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Domain.Entities.Order
{
    public class CreatePreOrderRequest
    {
        public decimal Amount { get; set; }
        public string? OrderId { get; set; }
    }
}

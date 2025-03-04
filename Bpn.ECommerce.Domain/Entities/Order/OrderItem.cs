using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Domain.Entities.Order
{

    public class OrderItem
    {
        public string? ProductId { get; set; }
        public int Quantity { get; set; }
    }
}

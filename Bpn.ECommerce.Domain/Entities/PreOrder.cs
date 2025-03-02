using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Domain.Entities
{
    public class PreOrder
    {
        public string? OrderId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
    }
}

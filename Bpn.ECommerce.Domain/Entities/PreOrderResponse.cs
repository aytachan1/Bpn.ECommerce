using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Domain.Entities
{
    public class PreOrderResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public PreOrderData? Data { get; set; }
    }

    public class PreOrderData
    {
        public PreOrder? PreOrder { get; set; }
        public BalanceData? UpdatedBalance { get; set; }
    }

   
}

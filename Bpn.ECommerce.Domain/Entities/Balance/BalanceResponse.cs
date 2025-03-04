using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Domain.Entities.Balance
{

    public class BalanceResponse
    {
        public bool Success { get; set; }
        public BalanceData? Data { get; set; }
    }

    public class BalanceData
    {
        public string? UserId { get; set; }
        public decimal TotalBalance { get; set; }
        public decimal AvailableBalance { get; set; }
        public decimal BlockedBalance { get; set; }
        public string? Currency { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}

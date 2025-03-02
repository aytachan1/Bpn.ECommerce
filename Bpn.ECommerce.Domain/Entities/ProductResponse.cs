using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Domain.Entities
{
    public class ProductResponse
    {
        public bool Success { get; set; }
        public List<ProductEntity> Data { get; set; } = new List<ProductEntity>();
    }
}

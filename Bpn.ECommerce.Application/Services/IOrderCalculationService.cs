﻿using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Entities.Product;
using Bpn.ECommerce.Domain.Generic.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Application.Services
{

    public interface IOrderCalculationService
    {
        Result<decimal> CalculateTotalPrice(List<OrderItem> orderList);
        Result<decimal> GetProductPriceById(string productId, int quantity);
        ProductEntity GetProductById(string productId);

        public bool HasDuplicateProductIds(List<OrderItem> orderList, out string duplicateProductId);

    }
}

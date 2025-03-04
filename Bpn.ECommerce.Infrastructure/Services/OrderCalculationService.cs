﻿using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Entities.Product;
using Bpn.ECommerce.Domain.Generic.Result;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bpn.ECommerce.Infrastructure.Services
{
    public class OrderCalculationService : IOrderCalculationService
    {
        public OrderCalculationService()
        {
                
        }
        public Result<decimal> CalculateTotalPrice(List<OrderItem> orderList)
        {
            decimal totalPrice = 0;
            foreach (var item in orderList)
            {
                if (item.ProductId == null)
                {
                    return Result<decimal>.Failure("ProductId cannot be null");
                }
                var productPriceResult = GetProductPriceById(item.ProductId, item.Quantity);
                if (!productPriceResult.IsSuccessful)
                {
                    return Result<decimal>.Failure(productPriceResult.StatusCode, productPriceResult.ErrorMessages ?? new List<string>() { "GetProductPriceById has error" });
                }
                totalPrice += productPriceResult.Data;
            }
            return Result<decimal>.Succeed(totalPrice);
        }

        public Result<decimal> GetProductPriceById(string productId, int quantity)
        {
            var product = GetProductById(productId);
            if (product == null)
            {
                return Result<decimal>.Failure("Product not found");
            }

            if (quantity > product.Stock)
            {
                return Result<decimal>.Failure("Requested quantity is not available in stock");
            }

            return Result<decimal>.Succeed(product.Price * quantity);
        }

        public ProductEntity GetProductById(string productId)
        {
            //Burada datayı birşekilde database e yükleyip ordan alıyoruz gibi düşünebiliriz.
            return new ProductEntity
            {
                Id = productId,
                Price = 100, // Example price
                Stock = 10 // Example stock
            };
        }

        public bool HasDuplicateProductIds(List<OrderItem> orderList, out string duplicateProductId)
        {
            var productIdSet = new HashSet<string>();
            foreach (var item in orderList)
            {
                if (item.ProductId == null)
                {
                    duplicateProductId = "";
                    return false;
                }
                if (!productIdSet.Add(item.ProductId))
                {
                    duplicateProductId = item.ProductId;
                    return true;
                }
            }
            duplicateProductId = "";
            return false;
        }
    }

}

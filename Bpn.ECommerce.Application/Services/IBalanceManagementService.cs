using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace Bpn.ECommerce.Application.Services
{
    public interface IBalanceManagementService
    {
        Task<Result<ProductResponse>> GetProductsAsync();
        Task<Result<BalanceResponse>> GetBalanceAsync();
        Task<Result<PreOrderResponse>> CreatePreOrderAsync(CreatePreOrderRequest request);
        Task<Result<PreOrderResponse>> UpdatePreOrderAsync(PreOrderRequest request);
        Task<Result<PreOrderResponse>> RemovePreOrderAsync(PreOrderRequest request);
    }
}

using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net.Http.Json;

namespace Bpn.ECommerce.Infrastructure.Services
{
    public class BalanceManagementService: IBalanceManagementService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BalanceManagementService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public BalanceManagementService(HttpClient httpClient, ILogger<BalanceManagementService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                    });
        }

        public async Task<Result<ProductResponse>> GetProductsAsync()
        {
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                var response = await _httpClient.GetAsync("https://balance-management-pi44.onrender.com/api/products");
                if (response.IsSuccessStatusCode)
                {
                    var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>();
                    if (productResponse != null)
                    {
                        return Result<ProductResponse>.Succeed(productResponse);
                    }
                    else
                    {
                        return Result<ProductResponse>.Failure("Product response is null");
                    }
                }
                else
                {
                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    return Result<ProductResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                }
            }).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "An unexpected error occurred while getting products");
                    return Result<ProductResponse>.Failure("An unexpected error occurred");
                }
                return task.Result;
            });
        }
    }
}

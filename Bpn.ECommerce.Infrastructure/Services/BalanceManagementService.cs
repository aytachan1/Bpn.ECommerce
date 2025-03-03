using Bpn.ECommerce.Application.Features.Balance.Queries;
using Bpn.ECommerce.Application.Services;
using Bpn.ECommerce.Domain.Entities;
using Bpn.ECommerce.Domain.Generic.Result;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Bulkhead;
using Polly.CircuitBreaker;
using Polly.Fallback;
using Polly.Retry;
using Polly.Timeout;
using System.Net.Http.Json;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using System.Diagnostics.Metrics;
using System.Diagnostics;

namespace Bpn.ECommerce.Infrastructure.Services
{
    public class BalanceManagementService : IBalanceManagementService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BalanceManagementService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncBulkheadPolicy _bulkheadPolicy;
        private readonly AsyncTimeoutPolicy _timeoutPolicy;
        // private readonly AsyncFallbackPolicy<Result<ProductResponse>> _fallbackPolicy; simdilik eklemiyorum belki dagitik yapıda kullanılabilir
        private readonly IMemoryCache _cache;
        private static readonly ActivitySource ActivitySource = new("Bpn.ECommerce.Infrastructure.Services.BalanceManagementService");
        private static readonly Meter Meter = new("Bpn.ECommerce.Infrastructure.Services.BalanceManagementService");
        private static readonly Counter<int> RequestCounter = Meter.CreateCounter<int>("balance_management_requests");
        private static readonly Histogram<double> ResponseTimeHistogram = Meter.CreateHistogram<double>("balance_management_response_time", "ms", "Response time in milliseconds");

        public BalanceManagementService(HttpClient httpClient, ILogger<BalanceManagementService> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;

            _retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} encountered an error: {exception.Message}. Waiting {timeSpan} before next retry.");
                    });

            _circuitBreakerPolicy = Policy
                .Handle<HttpRequestException>()
                .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1),
                    onBreak: (exception, duration) =>
                    {
                        _logger.LogWarning($"Circuit breaker opened for {duration.TotalSeconds} seconds due to: {exception.Message}");
                    },
                    onReset: () => _logger.LogInformation("Circuit breaker reset"),
                    onHalfOpen: () => _logger.LogInformation("Circuit breaker half-open"));

            _bulkheadPolicy = Policy
                .BulkheadAsync(10, 20,
                    onBulkheadRejectedAsync: context =>
                    {
                        _logger.LogWarning("Bulkhead limit reached, request rejected");
                        return Task.CompletedTask;
                    });

            _timeoutPolicy = Policy
               .TimeoutAsync(10, TimeoutStrategy.Pessimistic, onTimeoutAsync: (context, timespan, task) =>
               {
                   _logger.LogWarning($"Timeout after {timespan.TotalSeconds} seconds");
                   return Task.CompletedTask;
               });

        }

        public async Task<Result<ProductResponse>> GetProductsAsync()
        {
            using var activity = ActivitySource.StartActivity("GetProductsAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("GetProductsAsync started");

            var cacheKey = "products";
            if (_cache.TryGetValue(cacheKey, out var cachedProductsObj) && cachedProductsObj is Result<ProductResponse> cachedProducts)
            {
                stopwatch.Stop();
                ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("GetProductsAsync completed from cache");
                return cachedProducts;
            }
            return await _bulkheadPolicy.ExecuteAsync(() =>
                     _circuitBreakerPolicy.ExecuteAsync(() =>
                         _retryPolicy.ExecuteAsync(() =>
                             _timeoutPolicy.ExecuteAsync(async () =>
                             {
                                 _logger.LogInformation("Fetching products from API : GetSessionInfo()");
                                 var response = await _httpClient.GetAsync("https://balance-management-pi44.onrender.com/api/products");
                                 if (response.IsSuccessStatusCode)
                                 {
                                     var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>();
                                     if (productResponse != null)
                                     {
                                         var result = Result<ProductResponse>.Succeed(productResponse);
                                         _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
                                         _logger.LogInformation("Products fetched and cached successfully");
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         _logger.LogInformation("GetProductsAsync completed successfully");
                                         return result;
                                     }
                                     else
                                     {
                                         _logger.LogWarning("Product response is null");
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         return Result<ProductResponse>.Failure("Product response is null");
                                     }
                                 }
                                 else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     _logger.LogError("Internal server error: {Message}", errorResponse?.Message);
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<ProductResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error");
                                 }
                                 else
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     _logger.LogError("Error occurred: {Message}", errorResponse?.Message);
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<ProductResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                                 }
                             }).ContinueWith(task =>
                             {
                                 if (task.IsFaulted)
                                 {
                                     _logger.LogError(task.Exception, "An unexpected error occurred while getting products");
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<ProductResponse>.Failure("An unexpected error occurred");
                                 }
                                 stopwatch.Stop();
                                 ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                 _logger.LogInformation("GetProductsAsync completed");
                                 return task.Result;
                             }))));

        }

        public async Task<Result<BalanceResponse>> GetBalanceAsync()
        {
            using var activity = ActivitySource.StartActivity("GetBalanceAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("GetBalanceAsync started");

            var cacheKey = "user_balance";
            if (_cache.TryGetValue(cacheKey, out var cachedBalanceObj) && cachedBalanceObj is Result<BalanceResponse> cachedBalance)
            {
                stopwatch.Stop();
                ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                _logger.LogInformation("GetBalanceAsync completed from cache");
                return cachedBalance;
            }
            return await _bulkheadPolicy.ExecuteAsync(() =>
                     _circuitBreakerPolicy.ExecuteAsync(() =>
                         _retryPolicy.ExecuteAsync(() =>
                             _timeoutPolicy.ExecuteAsync(async () =>
                             {
                                 _logger.LogInformation("Fetching balance from API : GetSessionInfo()");
                                 var response = await _httpClient.GetAsync("https://balance-management-pi44.onrender.com/api/balance");
                                 if (response.IsSuccessStatusCode)
                                 {
                                     var balanceResponse = await response.Content.ReadFromJsonAsync<BalanceResponse>();
                                     if (balanceResponse != null)
                                     {
                                         var result = Result<BalanceResponse>.Succeed(balanceResponse);
                                         _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30)); // 30 dk cache tut
                                         _logger.LogInformation("Balance fetched and cached successfully");
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         _logger.LogInformation("GetBalanceAsync completed successfully");
                                         return result;
                                     }
                                     else
                                     {
                                         _logger.LogInformation("Balance response is null");
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         return Result<BalanceResponse>.Failure("Balance response is null");
                                     }
                                 }
                                 else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     _logger.LogError("Internal server error: {Message}", errorResponse?.Message);
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<BalanceResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error");
                                 }
                                 else
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     _logger.LogError("Error occurred: {Message}", errorResponse?.Message);
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<BalanceResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                                 }
                             }).ContinueWith(task =>
                             {
                                 if (task.IsFaulted)
                                 {
                                     _logger.LogError(task.Exception, "An unexpected error occurred while getting balance");
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<BalanceResponse>.Failure("An unexpected error occurred");
                                 }
                                 stopwatch.Stop();
                                 ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                 _logger.LogInformation("GetBalanceAsync completed");
                                 return task.Result;
                             }))));

        }

        public async Task<Result<PreOrderResponse>> CreatePreOrderAsync(CreatePreOrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("CreatePreOrderAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("CreatePreOrderAsync started");

            return await _bulkheadPolicy.ExecuteAsync(() =>
                     _circuitBreakerPolicy.ExecuteAsync(() =>
                         _retryPolicy.ExecuteAsync(() =>
                             _timeoutPolicy.ExecuteAsync(async () =>
                             {
                                 var response = await _httpClient.PostAsJsonAsync("https://balance-management-pi44.onrender.com/api/balance/preorder", request);
                                 if (response.IsSuccessStatusCode)
                                 {
                                     var preOrderResponse = await response.Content.ReadFromJsonAsync<PreOrderResponse>();
                                     if (preOrderResponse != null)
                                     {
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         _logger.LogInformation("CreatePreOrderAsync completed successfully");
                                         return Result<PreOrderResponse>.Succeed(preOrderResponse);
                                     }
                                     else
                                     {
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         _logger.LogWarning("PreOrder response is null");
                                         return Result<PreOrderResponse>.Failure("PreOrder response is null");
                                     }
                                 }
                                 else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Bad request");
                                 }
                                 else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error");
                                 }
                                 else
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                                 }
                             }).ContinueWith(task =>
                             {
                                 if (task.IsFaulted)
                                 {
                                     _logger.LogError(task.Exception, "An unexpected error occurred while creating pre-order");
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     return Result<PreOrderResponse>.Failure("An unexpected error occurred");
                                 }
                                 stopwatch.Stop();
                                 ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                 _logger.LogInformation("CreatePreOrderAsync completed");
                                 return task.Result;
                             }))));

        }

        public async Task<Result<PreOrderResponse>> UpdatePreOrderAsync(PreOrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("UpdatePreOrderAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("UpdatePreOrderAsync started");

            return await _bulkheadPolicy.ExecuteAsync(() =>
                    _circuitBreakerPolicy.ExecuteAsync(() =>
                        _retryPolicy.ExecuteAsync(() =>
                            _timeoutPolicy.ExecuteAsync(async () =>
                            {
                                var response = await _httpClient.PostAsJsonAsync("https://balance-management-pi44.onrender.com/api/balance/complete", request);
                                if (response.IsSuccessStatusCode)
                                {
                                    var updatePreOrderResponse = await response.Content.ReadFromJsonAsync<PreOrderResponse>();
                                    if (updatePreOrderResponse != null)
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogInformation("UpdatePreOrderAsync completed successfully");
                                        return Result<PreOrderResponse>.Succeed(updatePreOrderResponse);
                                    }
                                    else
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogWarning("UpdatePreOrder response is null");
                                        return Result<PreOrderResponse>.Failure("UpdatePreOrder response is null");
                                    }
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Bad request");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Page Not Found");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error");
                                }
                                else
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                                }
                            }).ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    _logger.LogError(task.Exception, "An unexpected error occurred while updating pre-order");
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure("An unexpected error occurred");
                                }
                                stopwatch.Stop();
                                ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                _logger.LogInformation("UpdatePreOrderAsync completed");
                                return task.Result;
                            }))));

        }

        public async Task<Result<PreOrderResponse>> RemovePreOrderAsync(PreOrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("RemovePreOrderAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("RemovePreOrderAsync started");

            return await _bulkheadPolicy.ExecuteAsync(() =>
                    _circuitBreakerPolicy.ExecuteAsync(() =>
                        _retryPolicy.ExecuteAsync(() =>
                            _timeoutPolicy.ExecuteAsync(async () =>
                            {
                                var response = await _httpClient.PostAsJsonAsync("https://balance-management-pi44.onrender.com/api/balance/cancel", request);
                                if (response.IsSuccessStatusCode)
                                {
                                    var removePreOrderResponse = await response.Content.ReadFromJsonAsync<PreOrderResponse>();
                                    if (removePreOrderResponse != null)
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogInformation("RemovePreOrderAsync completed successfully");
                                        return Result<PreOrderResponse>.Succeed(removePreOrderResponse);
                                    }
                                    else
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogWarning("RemovePreOrder response is null");
                                        return Result<PreOrderResponse>.Failure("RemovePreOrder response is null");
                                    }
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Bad request");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Page Not Found");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error");
                                }
                                else
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                                }
                            }).ContinueWith(task =>
                            {
                                if (task.IsFaulted)
                                {
                                    _logger.LogError(task.Exception, "An unexpected error occurred while removing pre-order");
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure("An unexpected error occurred");
                                }
                                stopwatch.Stop();
                                ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                _logger.LogInformation("RemovePreOrderAsync completed");
                                return task.Result;
                            }))));

        }
    }
}
using Bpn.ECommerce.Application.Features.Balance.Queries;
using Bpn.ECommerce.Application.Services;
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
using Azure.Core.Pipeline;
using System.Text.Json.Serialization;
using System.Text.Json;
using Bpn.ECommerce.Domain.Entities.Product;
using Bpn.ECommerce.Domain.Entities.Order;
using Bpn.ECommerce.Domain.Entities.Balance;
using Bpn.ECommerce.Domain.Entities.Error;

namespace Bpn.ECommerce.Infrastructure.Services
{
    public class BalanceManagementService : IBalanceManagementService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BalanceManagementService> _logger;
        private readonly AsyncRetryPolicy<Result<ProductResponse>> _retryPolicyForProduct;
        private readonly AsyncRetryPolicy<Result<PreOrderResponse>> _retryPolicyForPreOrder;
        private readonly AsyncRetryPolicy<Result<BalanceResponse>> _retryPolicyForBalance;
        private readonly AsyncCircuitBreakerPolicy<Result<ProductResponse>> _circuitBreakerPolicyForProduct;
        private readonly AsyncCircuitBreakerPolicy<Result<PreOrderResponse>> _circuitBreakerPolicyForPreOrder;
        private readonly AsyncCircuitBreakerPolicy<Result<BalanceResponse>> _circuitBreakerPolicyForBalance;

        private readonly AsyncBulkheadPolicy _bulkheadPolicy;
        private readonly AsyncTimeoutPolicy _timeoutPolicy;

        // private readonly AsyncFallbackPolicy<Result<ProductResponse>> _fallbackPolicy; 
        private readonly IMemoryCache _cache;
        private static readonly ActivitySource ActivitySource = new("Bpn.ECommerce.Infrastructure.Services.BalanceManagementService");
        private static readonly Meter Meter = new("Bpn.ECommerce.Infrastructure.Services.BalanceManagementService");
        private static readonly Counter<int> RequestCounter = Meter.CreateCounter<int>("balance_management_requests");
        private static readonly Histogram<double> ResponseTimeHistogram = Meter.CreateHistogram<double>("balance_management_response_time", "ms", "Response time in milliseconds");

        private const string ProductsApiUrl = "https://balance-management-pi44.onrender.com/api/products";
        private const string BalanceApiUrl = "https://balance-management-pi44.onrender.com/api/balance";
        private const string PreOrderApiUrl = "https://balance-management-pi44.onrender.com/api/balance/preorder";
        private const string CompleteOrderApiUrl = "https://balance-management-pi44.onrender.com/api/balance/complete";
        private const string CancelOrderApiUrl = "https://balance-management-pi44.onrender.com/api/balance/cancel";


        public BalanceManagementService(HttpClient httpClient, ILogger<BalanceManagementService> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _logger = logger;
            _cache = cache;

            _httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.AcceptCharset.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("utf-8"));


            _retryPolicyForProduct = CreateRetryPolicy<ProductResponse>(3);
            _retryPolicyForPreOrder = CreateRetryPolicy<PreOrderResponse>(5);
            _retryPolicyForBalance = CreateRetryPolicy<BalanceResponse>(3);

            _circuitBreakerPolicyForProduct = CreateAsyncCircuitBreakerPolicy<ProductResponse>();
            _circuitBreakerPolicyForPreOrder = CreateAsyncCircuitBreakerPolicy<PreOrderResponse>();
            _circuitBreakerPolicyForBalance = CreateAsyncCircuitBreakerPolicy<BalanceResponse>();


            _bulkheadPolicy = Policy
                .BulkheadAsync(10, 20,
                    onBulkheadRejectedAsync: context =>
                    {
                        _logger.LogWarning("Bulkhead limit reached, request rejected");
                        return Task.CompletedTask;
                    });

            _timeoutPolicy = Policy
               .TimeoutAsync(10, TimeoutStrategy.Optimistic, onTimeoutAsync: (context, timespan, task) =>
               {
                   _logger.LogWarning($"Timeout after {timespan.TotalSeconds} seconds");
                   return Task.CompletedTask;
               });

        }

        private AsyncRetryPolicy<Result<T>> CreateRetryPolicy<T>(int retryCount)
        {
            return Policy<Result<T>>
                .HandleResult(result => !result.IsSuccessful)
                .Or<HttpRequestException>()
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Retry {retryCount} encountered an error: {result.Exception?.Message ?? result.Result?.ErrorMessages?.FirstOrDefault()}. Waiting {timeSpan} before next retry.");
                    });
        }

        private AsyncCircuitBreakerPolicy<Result<T>> CreateAsyncCircuitBreakerPolicy<T>()
        {
            return Policy<Result<T>>
                .HandleResult(result => !result.IsSuccessful)
                .Or<HttpRequestException>()
                .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1),
                    onBreak: (result, duration) =>
                    {
                        if (result.Exception != null)
                        {
                            _logger.LogWarning($"Circuit breaker opened for {duration.TotalSeconds} seconds due to: {result.Exception.Message}");
                        }
                        else
                        {
                            _logger.LogWarning($"Circuit breaker opened for {duration.TotalSeconds} seconds due to unsuccessful result: {result.Result?.ErrorMessages?.FirstOrDefault()}");
                        }
                    },
                    onReset: () => _logger.LogInformation("Circuit breaker reset"),
                    onHalfOpen: () => _logger.LogInformation("Circuit breaker half-open"));
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
                     _circuitBreakerPolicyForProduct.ExecuteAsync(() =>
                         _retryPolicyForProduct.ExecuteAsync(() =>
                          _timeoutPolicy.ExecuteAsync(async () =>
                          {
                                 _logger.LogInformation("Fetching products from API : GetSessionInfo()");
                                 var response = await _httpClient.GetAsync(ProductsApiUrl);
                                 if (response.IsSuccessStatusCode)
                                 {

                                     var productResponse = await response.Content.ReadFromJsonAsync<ProductResponse>();
                                     if (productResponse != null)
                                     {
                                         var result = Result<ProductResponse>.Succeed(productResponse);
                                         _cache.Set(cacheKey, result, TimeSpan.FromMinutes(60)); // Cache for 60 minutes
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
                     _circuitBreakerPolicyForBalance.ExecuteAsync(() =>
                         _retryPolicyForBalance.ExecuteAsync(() =>
                             _timeoutPolicy.ExecuteAsync(async () =>
                             {
                                 _logger.LogInformation("Fetching balance from API : GetSessionInfo()");
                                 var response = await _httpClient.GetAsync(BalanceApiUrl);
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
            _logger.LogInformation("CreatePreOrderAsync started for  OrderId: {OrderId}", request.OrderId);

            return await _bulkheadPolicy.ExecuteAsync(() =>
                     _circuitBreakerPolicyForPreOrder.ExecuteAsync(() =>
                         _retryPolicyForPreOrder.ExecuteAsync(() =>
                             _timeoutPolicy.ExecuteAsync(async () =>
                             {
                                 var response = await _httpClient.PostAsJsonAsync(PreOrderApiUrl, request);
                                 if (response.IsSuccessStatusCode)
                                 {
                                     var preOrderResponse = await response.Content.ReadFromJsonAsync<PreOrderResponse>();
                                     if (preOrderResponse != null)
                                     {
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         _logger.LogInformation("CreatePreOrderAsync completed successfully for  OrderId: {OrderId}", request.OrderId);
                                         return Result<PreOrderResponse>.Succeed(preOrderResponse);
                                     }
                                     else
                                     {
                                         stopwatch.Stop();
                                         ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                         _logger.LogWarning("PreOrder response is null for  OrderId: {OrderId}", request.OrderId);
                                         return Result<PreOrderResponse>.Failure("PreOrder response is null");
                                     }
                                 }
                                 else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     _logger.LogWarning("CreatePreOrderAsync Bad request for  OrderId: {OrderId}", request.OrderId);
                                     return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Bad request for " + request.OrderId );
                                 }
                                 else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     _logger.LogWarning("CreatePreOrderAsync Internal server error for  OrderId: {OrderId}", request.OrderId);
                                     return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error for " + request.OrderId);
                                 }
                                 else
                                 {
                                     var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                     stopwatch.Stop();
                                     ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                     _logger.LogWarning("CreatePreOrderAsync An error occurred for  OrderId: {OrderId}", request.OrderId);
                                     return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred for " + request.OrderId );
                                 }
                             }))));

        }

        public async Task<Result<PreOrderResponse>> UpdatePreOrderAsync(PreOrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("UpdatePreOrderAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("UpdatePreOrderAsync started for " + request.OrderId);

            return await _bulkheadPolicy.ExecuteAsync(() =>
                    _circuitBreakerPolicyForPreOrder.ExecuteAsync(() =>
                        _retryPolicyForPreOrder.ExecuteAsync(() =>
                            _timeoutPolicy.ExecuteAsync(async () =>
                            {
                                var response = await _httpClient.PostAsJsonAsync(CompleteOrderApiUrl, request);
                                if (response.IsSuccessStatusCode)
                                {
                                    var updatePreOrderResponse = await response.Content.ReadFromJsonAsync<PreOrderResponse>();
                                    if (updatePreOrderResponse != null)
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogInformation("UpdatePreOrderAsync completed successfully for " + request.OrderId );
                                        return Result<PreOrderResponse>.Succeed(updatePreOrderResponse);
                                    }
                                    else
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogWarning("UpdatePreOrder response is null");
                                        return Result<PreOrderResponse>.Failure("UpdatePreOrder response is null for " + request.OrderId );
                                    }
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    _logger.LogWarning("UpdatePreOrderAsync Bad request for " + request.OrderId);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Bad request for " + request.OrderId);
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Page Not Found for " + request.OrderId);
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error for " + request.OrderId);
                                }
                                else
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred for " + request.OrderId);
                                }
                            }))));

        }

        public async Task<Result<PreOrderResponse>> RemovePreOrderAsync(PreOrderRequest request)
        {
            using var activity = ActivitySource.StartActivity("RemovePreOrderAsync");
            RequestCounter.Add(1);
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation("RemovePreOrderAsync started for " + request.OrderId);

            return await _bulkheadPolicy.ExecuteAsync(() =>
                    _circuitBreakerPolicyForPreOrder.ExecuteAsync(() =>
                        _retryPolicyForPreOrder.ExecuteAsync(() =>
                            _timeoutPolicy.ExecuteAsync(async () =>
                            {
                                var response = await _httpClient.PostAsJsonAsync(CancelOrderApiUrl, request);
                                if (response.IsSuccessStatusCode)
                                {
                                    var removePreOrderResponse = await response.Content.ReadFromJsonAsync<PreOrderResponse>();
                                    if (removePreOrderResponse != null)
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogInformation("RemovePreOrderAsync completed successfully for " + request.OrderId);
                                        return Result<PreOrderResponse>.Succeed(removePreOrderResponse);
                                    }
                                    else
                                    {
                                        stopwatch.Stop();
                                        ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                        _logger.LogWarning("RemovePreOrder response is null for " + request.OrderId);
                                        return Result<PreOrderResponse>.Failure("RemovePreOrder response is null");
                                    }
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    _logger.LogWarning("Bad request for " + request.OrderId);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Bad request");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    _logger.LogWarning("Page Not Found for " + request.OrderId);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Page Not Found");
                                }
                                else if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    _logger.LogWarning("Internal server error for " + request.OrderId);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "Internal server error");
                                }
                                else
                                {
                                    var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                                    stopwatch.Stop();
                                    ResponseTimeHistogram.Record(stopwatch.ElapsedMilliseconds);
                                    _logger.LogWarning("An error occurred for " + request.OrderId);
                                    return Result<PreOrderResponse>.Failure((int)response.StatusCode, errorResponse?.Message ?? "An error occurred");
                                }
                            }))));

        }
    }
}
using PottaKDS.Models;
using PottaKDS.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PottaKDS.Services
{
    public class KdsApiService : IKdsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsService _settingsService;
        private readonly JsonSerializerOptions _jsonOptions;

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    ConnectionStatusChanged?.Invoke(this, value);
                }
            }
        }

        public string CurrentBaseUrl => _settingsService.CurrentSettings.BaseUrl;
        public string? LastErrorMessage { get; private set; }

        public event EventHandler<bool>? ConnectionStatusChanged;

        public KdsApiService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(6)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<bool> TestConnectionAsync(string baseUrl)
        {
            try
            {
                var cleanUrl = baseUrl.Trim().TrimEnd('/');
                if (!cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanUrl = $"http://{cleanUrl}";
                }

                // 1. Try health check endpoint
                try
                {
                    using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                    var response = await _httpClient.GetAsync($"{cleanUrl}/health", cts1.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        LastErrorMessage = null;
                        return true;
                    }
                }
                catch (Exception ex) when (ex is not SocketException)
                {
                    // Fall through to next check
                }

                // 2. Try orders waiting endpoint
                try
                {
                    using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var fallbackResponse = await _httpClient.GetAsync($"{cleanUrl}/api/orders/waiting", cts2.Token);
                    if (fallbackResponse.IsSuccessStatusCode)
                    {
                        LastErrorMessage = null;
                        return true;
                    }
                }
                catch (Exception ex) when (ex is not SocketException)
                {
                    // Fall through to next check
                }

                // 3. Try swagger documentation endpoint
                try
                {
                    using var cts3 = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var swaggerResponse = await _httpClient.GetAsync($"{cleanUrl}/swagger", cts3.Token);
                    if (swaggerResponse.IsSuccessStatusCode || ((int)swaggerResponse.StatusCode >= 300 && (int)swaggerResponse.StatusCode < 400))
                    {
                        LastErrorMessage = null;
                        return true;
                    }
                }
                catch (Exception ex) when (ex is not SocketException)
                {
                    // Fall through
                }

                LastErrorMessage = "Server responded but endpoint returned an error status.";
                return false;
            }
            catch (Exception ex)
            {
                var innermost = ex;
                while (innermost.InnerException != null)
                {
                    innermost = innermost.InnerException;
                }

                if (innermost is System.Net.Sockets.SocketException sockEx)
                {
                    LastErrorMessage = sockEx.SocketErrorCode switch
                    {
                        System.Net.Sockets.SocketError.ConnectionRefused => "Connection refused. Potta POS server is not running on this IP/port.",
                        System.Net.Sockets.SocketError.TimedOut => "Connection timed out. Check IP and ensure firewall allows port 5001.",
                        System.Net.Sockets.SocketError.HostNotFound or System.Net.Sockets.SocketError.HostUnreachable => "Host unreachable. Verify computers are on the same network.",
                        _ => $"Network error: {sockEx.Message}"
                    };
                }
                else if (ex is TaskCanceledException or OperationCanceledException)
                {
                    LastErrorMessage = "Connection timed out. Host did not respond.";
                }
                else
                {
                    LastErrorMessage = innermost.Message;
                }

                return false;
            }
        }

        public async Task<List<WaitingTransactionDto>> GetWaitingTransactionsAsync()
        {
            var baseUrl = CurrentBaseUrl;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var url = $"{baseUrl}/api/orders/waiting";

                var response = await _httpClient.GetAsync(url, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ApiResponseDto<List<WaitingTransactionDto>>>(_jsonOptions, cts.Token);
                    IsConnected = true;
                    LastErrorMessage = null;

                    return result?.Data ?? new List<WaitingTransactionDto>();
                }
                else
                {
                    IsConnected = false;
                    LastErrorMessage = $"API returned {response.StatusCode}";
                    return new List<WaitingTransactionDto>();
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastErrorMessage = $"Connection error: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[KdsApiService] Error fetching waiting transactions: {ex.Message}");
                return new List<WaitingTransactionDto>();
            }
        }

        public async Task<bool> UpdateOrderStatusAsync(string transactionId, string status)
        {
            var baseUrl = CurrentBaseUrl;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var url = $"{baseUrl}/api/orders/waiting/{Uri.EscapeDataString(transactionId)}/status";

                var payload = new { status };
                var response = await _httpClient.PutAsJsonAsync(url, payload, _jsonOptions, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    IsConnected = true;
                    return true;
                }

                LastErrorMessage = $"Failed to update status: {response.StatusCode}";
                return false;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> UpdateOrderItemsAsync(string transactionId, List<WaitingTransactionItemDto> items)
        {
            var baseUrl = CurrentBaseUrl;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var url = $"{baseUrl}/api/orders/waiting/{Uri.EscapeDataString(transactionId)}/items";

                var payload = new 
                { 
                    items = items,
                    staffId = (int?)null
                };

                var response = await _httpClient.PutAsJsonAsync(url, payload, _jsonOptions, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    IsConnected = true;
                    return true;
                }

                LastErrorMessage = $"Failed to update items: {response.StatusCode}";
                return false;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastErrorMessage = ex.Message;
                return false;
            }
        }

        public async Task<bool> CompleteOrderAsync(string transactionId)
        {
            var baseUrl = CurrentBaseUrl;
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(6));
                var url = $"{baseUrl}/api/orders/waiting/{Uri.EscapeDataString(transactionId)}";

                var response = await _httpClient.DeleteAsync(url, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    IsConnected = true;
                    return true;
                }

                LastErrorMessage = $"Failed to complete order: {response.StatusCode}";
                return false;
            }
            catch (Exception ex)
            {
                IsConnected = false;
                LastErrorMessage = ex.Message;
                return false;
            }
        }
    }
}

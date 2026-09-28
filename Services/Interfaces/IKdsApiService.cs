using PottaKDS.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PottaKDS.Services.Interfaces
{
    public interface IKdsApiService
    {
        bool IsConnected { get; }
        string CurrentBaseUrl { get; }
        string? LastErrorMessage { get; }
        event EventHandler<bool>? ConnectionStatusChanged;

        Task<bool> TestConnectionAsync(string baseUrl);
        Task<List<WaitingTransactionDto>> GetWaitingTransactionsAsync();
        Task<bool> UpdateOrderStatusAsync(string transactionId, string status);
        Task<bool> UpdateOrderItemsAsync(string transactionId, List<WaitingTransactionItemDto> items);
        Task<bool> CompleteOrderAsync(string transactionId);
    }
}

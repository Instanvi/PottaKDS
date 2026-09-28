using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PottaKDS.Models
{
    public class WaitingTransactionDto
    {
        [JsonPropertyName("transactionId")]
        public string TransactionId { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("tableId")]
        public string? TableId { get; set; }

        [JsonPropertyName("tableNumber")]
        public int? TableNumber { get; set; }

        [JsonPropertyName("tableName")]
        public string? TableName { get; set; }

        [JsonPropertyName("staffId")]
        public int? StaffId { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "Pending";

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("seatIds")]
        public string? SeatIds { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("modifiedDate")]
        public DateTime ModifiedDate { get; set; }

        // Refire properties
        [JsonPropertyName("isRefired")]
        public bool IsRefired { get; set; }

        [JsonPropertyName("refireReason")]
        public string? RefireReason { get; set; }

        [JsonPropertyName("refiredAt")]
        public DateTime? RefiredAt { get; set; }

        [JsonPropertyName("refiredByStaffId")]
        public int? RefiredByStaffId { get; set; }

        [JsonPropertyName("refiredByStaffName")]
        public string? RefiredByStaffName { get; set; }

        [JsonPropertyName("refiredItemIndices")]
        public string? RefiredItemIndices { get; set; }

        [JsonPropertyName("items")]
        public List<WaitingTransactionItemDto> Items { get; set; } = new();

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
    }
}

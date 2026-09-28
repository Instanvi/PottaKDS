using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PottaKDS.Models
{
    public class WaitingTransactionItemDto
    {
        [JsonPropertyName("productId")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        [JsonPropertyName("discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("taxId")]
        public string? TaxId { get; set; }

        [JsonPropertyName("taxable")]
        public bool Taxable { get; set; } = true;

        [JsonPropertyName("staffId")]
        public int? StaffId { get; set; }

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; } = false;

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [JsonPropertyName("appliedModifiers")]
        public List<AppliedModifierDto>? AppliedModifiers { get; set; }

        [JsonPropertyName("unitType")]
        public string UnitType { get; set; } = "Base";

        [JsonPropertyName("unitsPerPackage")]
        public decimal UnitsPerPackage { get; set; } = 1;

        [JsonPropertyName("isBundle")]
        public bool IsBundle { get; set; } = false;

        [JsonPropertyName("isRecipe")]
        public bool IsRecipe { get; set; } = false;

        [JsonPropertyName("subTotal")]
        public decimal SubTotal { get; set; }

        [JsonPropertyName("total")]
        public decimal Total { get; set; }

        [JsonPropertyName("taxAmount")]
        public decimal TaxAmount { get; set; }
    }
}

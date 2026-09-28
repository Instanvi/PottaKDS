using System.Text.Json.Serialization;

namespace PottaKDS.Models
{
    public class AppliedModifierDto
    {
        [JsonPropertyName("modifierId")]
        public string? ModifierId { get; set; }

        [JsonPropertyName("modifierName")]
        public string? ModifierName { get; set; }

        [JsonPropertyName("priceChange")]
        public decimal PriceChange { get; set; }

        [JsonPropertyName("recipeId")]
        public string? RecipeId { get; set; }
    }
}

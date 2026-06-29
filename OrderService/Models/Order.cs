using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OrderService.Models;

public class Order
{
	[JsonPropertyName("id")]          // For Web Browser (System.Text.Json)
	[JsonProperty(PropertyName = "id")] // For Cosmos DB Database (Newtonsoft.Json)
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("userId")]
	[JsonProperty(PropertyName = "UserId")]
	public string UserId { get; set; } = string.Empty; // Partition Key

	[JsonPropertyName("totalAmount")]
	[JsonProperty(PropertyName = "totalAmount")]
	public decimal TotalAmount { get; set; }

	[JsonPropertyName("createdAt")]
	[JsonProperty(PropertyName = "createdAt")]
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

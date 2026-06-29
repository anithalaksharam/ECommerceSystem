using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace CatalogService.Models;

public class Product
{
	[JsonProperty(PropertyName = "id")]
	public string Id { get; set; } = string.Empty;

	[JsonProperty(PropertyName = "categoryId")]//partition ley
	public string CategoryId { get; set; } = string.Empty;

	[JsonProperty(PropertyName = "name")]
	public string Name { get; set; } = string.Empty;

	[JsonProperty(PropertyName = "price")]
	public decimal Price { get; set; }
}

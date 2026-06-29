using Microsoft.Azure.Cosmos;
using CatalogService.Models;

var builder = WebApplication.CreateBuilder(args);

// Register a Thread-Safe CosmosClient Instance
builder.Services.AddSingleton<CosmosClient>(sp =>
{
	var connectionString = builder.Configuration.GetConnectionString("CosmosDb");

	var clientOptions = new CosmosClientOptions
	{
		ConnectionMode = ConnectionMode.Gateway,
		LimitToEndpoint = true,
		HttpClientFactory = () =>
		{
			return new HttpClient(new HttpClientHandler
			{
				ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
			});
		}
	};

	var endpoint = "https://host." + "docker.internal:8081/";

	var secretKey = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM" +
				   "+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZn" +
				   "qyMsEcaGQy67XIw/Jw==";

	return new CosmosClient(endpoint, secretKey, clientOptions);
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapPost("/api/catalog", async (Product product, CosmosClient client) =>
{
	var container = client.GetContainer("ECommerceDb", "Catalog");
	product.Id = Guid.NewGuid().ToString();

	// Pass object and explicit PartitionKey for high throughput performance
	var response = await container.CreateItemAsync(product, new PartitionKey(product.CategoryId));
	return Results.Created($"/api/catalog/{product.Id}", response.Resource);
});

app.MapGet("/api/catalog/{categoryId}", async (string categoryId, CosmosClient client) =>
{
	var container = client.GetContainer("ECommerceDb", "Catalog");
	var queryDefinition = new QueryDefinition("SELECT * FROM c WHERE c.categoryId = @catId")
		.WithParameter("@catId", categoryId);

	var queryIterator = container.GetItemQueryIterator<Product>(queryDefinition,
		requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(categoryId) });

	var items = new List<Product>();
	while (queryIterator.HasMoreResults)
	{
		var response = await queryIterator.ReadNextAsync();
		items.AddRange(response);
	}
	return Results.Ok(items);
});

app.Run();

using Microsoft.Azure.Cosmos;
using OrderService.Models;

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

// Automatically initialize the database and container layout
using (var scope = app.Services.CreateScope())
{
	var client = scope.ServiceProvider.GetRequiredService<CosmosClient>();
	Database database = await client.CreateDatabaseIfNotExistsAsync("ECommerceDb");
	await database.CreateContainerIfNotExistsAsync("Orders", "/UserId");
}

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// POST Endpoint targeting Cosmos DB
app.MapPost("/api/orders", async (Order order, CosmosClient client) =>
{
	var container = client.GetContainer("ECommerceDb", "Orders");
	order.Id = Guid.NewGuid().ToString();

	var response = await container.CreateItemAsync(order, new PartitionKey(order.UserId));
	return Results.Created($"/api/orders/{order.Id}", response.Resource);
});

app.Run();

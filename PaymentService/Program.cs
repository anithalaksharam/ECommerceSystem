using Microsoft.EntityFrameworkCore;
using PaymentService.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Swapped PostgreSQL to Azure SQL Server + Added Cloud Resiliency Strategy
builder.Services.AddDbContext<PaymentDbContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("PaymentsDbConnection"), sqlOptions =>
	{
		// Vital for AKS deployment: Handles temporary Azure SQL network/failover dropouts
		sqlOptions.EnableRetryOnFailure(
			maxRetryCount: 5,
			maxRetryDelay: TimeSpan.FromSeconds(10),
			errorNumbersToAdd: null);
	}));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

// 2. Updated ACID Protected Payment Processing Endpoint
app.MapPost("/api/payments/process", async (PaymentRecordInput input, PaymentDbContext db) =>
{
	// Fetch the SQL Server resilient execution strategy
	var strategy = db.Database.CreateExecutionStrategy();

	try
	{
		// Execute the operations inside the strategy delegate
		var result = await strategy.ExecuteAsync(async () =>
		{
			// Open the transaction safely inside the retry bubble
			using var transaction = await db.Database.BeginTransactionAsync();
			try
			{
				var paymentId = Guid.NewGuid();

				var payment = new PaymentRecord
				{
					Id = paymentId,
					OrderId = input.OrderId,
					Amount = input.Amount,
					Status = PaymentStatus.Completed,
					CreatedAtUtc = DateTime.UtcNow
				};

				var ledgerEntry = new PaymentLedger
				{
					Id = Guid.NewGuid(),
					PaymentRecordId = paymentId,
					Description = $"Payment processed successfully for Order {input.OrderId}",
					BalanceChange = input.Amount,
					TimestampUtc = DateTime.UtcNow
				};

				db.Payments.Add(payment);
				db.Ledgers.Add(ledgerEntry);

				await db.SaveChangesAsync();
				await transaction.CommitAsync();

				return new { PaymentId = paymentId, Status = payment.Status.ToString(), OrderId = payment.OrderId };
			}
			catch
			{
				await transaction.RollbackAsync();
				throw; // Rethrow to let the execution strategy handle retry tracking
			}
		});

		return Results.Ok(result);
	}
	catch (Exception ex)
	{
		// Real production fallback log
		return Results.Json(new
		{
			error = "Transaction aborted due to persistence exception.",
			details = ex.Message
		}, statusCode: 500);
	}
});



// 3. Automated Database Migration on Startup 
using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
	await dbContext.Database.MigrateAsync();
}

app.Run();

// 4. Data Transfer Object (DTO) for cleaner endpoint input contracts
public record PaymentRecordInput(string OrderId, decimal Amount);

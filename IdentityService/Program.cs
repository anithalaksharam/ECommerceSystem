using System.Security.Cryptography;
using IdentityService.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
	options.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection"), sqlOptions =>
	{
		sqlOptions.EnableRetryOnFailure(
			maxRetryCount: 5,
			maxRetryDelay: TimeSpan.FromSeconds(30),
			errorNumbersToAdd: null);
	});
	options.UseOpenIddict();
});

// 2. Explicitly Typed ASP.NET Core Identity Core Config
builder.Services.AddIdentityCore<ApplicationUser>(options =>
{
	options.Password.RequireDigit = true;
	options.Password.RequiredLength = 10;
	options.Password.RequireNonAlphanumeric = true;
	options.Password.RequireUppercase = true;
	options.User.RequireUniqueEmail = true;
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
	options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>() //Ensure it references your ApplicationDbContext exactly
.AddDefaultTokenProviders();


// 3. OpenIddict OIDC Engine Config
builder.Services.AddOpenIddict()
	.AddCore(options =>
	{
		options.UseEntityFrameworkCore()
			   .UseDbContext<ApplicationDbContext>();
	})
	.AddServer(options =>
	{
		options.SetAuthorizationEndpointUris("/connect/authorize")
			   .SetTokenEndpointUris("/connect/token")
			   .SetUserInfoEndpointUris("/connect/userinfo");

		options.AllowAuthorizationCodeFlow()
			   .AllowRefreshTokenFlow();

		if (builder.Environment.IsDevelopment())
		{
			options.AddDevelopmentEncryptionCertificate()
				   .AddDevelopmentSigningCertificate();
		}
		else
		{
			options.AddEphemeralEncryptionKey()
				   .AddEphemeralSigningKey();
		}

		options.UseAspNetCore()
			   .EnableTokenEndpointPassthrough()
			   .EnableAuthorizationEndpointPassthrough()
			   .EnableUserInfoEndpointPassthrough();
	})
	.AddValidation(options =>
	{
		options.UseLocalServer();
		options.UseAspNetCore();
	});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// --- Add this database initialization block ---
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	var context = services.GetRequiredService<ApplicationDbContext>();

	// This tells SQL Server to explicitly create "IdentityDb" if it's missing
	await context.Database.EnsureCreatedAsync();
}
// -----------------------------------------------
app.Run();

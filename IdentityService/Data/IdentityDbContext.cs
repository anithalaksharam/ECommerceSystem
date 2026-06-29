using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Data;

public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser
{
	// Custom audit fields or profiles go here
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
		: base(options)
	{
	}

	protected override void OnModelCreating(ModelBuilder builder)
	{
		base.OnModelCreating(builder);

		// This injects OpenIddict tables (Applications, Authorizations, Scopes, Tokens)
		// alongside your standard ASP.NET Core Identity tables.
		builder.UseOpenIddict();
	}
}
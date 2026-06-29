using Microsoft.EntityFrameworkCore;

namespace PaymentService.Data;

public enum PaymentStatus
{
	Pending,
	Completed,
	Failed,
	Refunded
}

public class PaymentRecord
{
	public Guid Id { get; set; }
	public string OrderId { get; set; } = string.Empty;
	public decimal Amount { get; set; }
	public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
	public DateTime CreatedAtUtc { get; set; }

	// Navigation property for audit/ledger tracking
	public ICollection<PaymentLedger> LedgerEntries { get; set; } = new List<PaymentLedger>();
}

public class PaymentLedger
{
	public Guid Id { get; set; }
	public Guid PaymentRecordId { get; set; }
	public string Description { get; set; } = string.Empty;
	public decimal BalanceChange { get; set; }
	public DateTime TimestampUtc { get; set; }

	public PaymentRecord PaymentRecord { get; set; } = null!;
}

public class PaymentDbContext : DbContext
{
	public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
	{
	}

	public DbSet<PaymentRecord> Payments => Set<PaymentRecord>();
	public DbSet<PaymentLedger> Ledgers => Set<PaymentLedger>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Configure PaymentRecord
		modelBuilder.Entity<PaymentRecord>(entity =>
		{
			entity.HasKey(e => e.Id);

			// Critical for scale: index OrderId for fast queries
			entity.HasIndex(e => e.OrderId);

			// Strict constraints for Relational databases
			entity.Property(e => e.OrderId).IsRequired().HasMaxLength(50);

			// Force SQL decimal precision to avoid truncation/rounding bugs
			entity.Property(e => e.Amount).HasColumnType("decimal(18,4)");

			// Store Enum as a string in database for readability and safe extensions
			entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
		});

		// Configure PaymentLedger (Audit Trail)
		modelBuilder.Entity<PaymentLedger>(entity =>
		{
			entity.HasKey(e => e.Id);
			entity.Property(e => e.BalanceChange).HasColumnType("decimal(18,4)");
			entity.Property(e => e.Description).HasMaxLength(250);

			// Set up strict foreign key constraint
			entity.HasOne(l => l.PaymentRecord)
				  .WithMany(p => p.LedgerEntries)
				  .HasForeignKey(l => l.PaymentRecordId)
				  .OnDelete(DeleteBehavior.Cascade);
		});
	}
}

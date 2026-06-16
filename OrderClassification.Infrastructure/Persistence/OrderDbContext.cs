using Microsoft.EntityFrameworkCore;
using OrderClassification.Domain.Orders;

namespace OrderClassification.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the OrderClassification bounded context.
///
/// Python analogy: a SQLAlchemy Session-backed unit-of-work that knows how
/// to materialize and persist the Order aggregate.
/// </summary>
public sealed class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    public DbSet<ClassificationReadModel> ClassificationReadModels => Set<ClassificationReadModel>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(builder =>
        {
            builder.ToTable("Orders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.ReferenceNumber).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.ReferenceNumber).IsUnique();
            builder.Property(x => x.Status).IsRequired();
            builder.Property(x => x.Classification).HasMaxLength(128);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.ClassifiedAt);
        });

        modelBuilder.Entity<ClassificationReadModel>(builder =>
        {
            builder.ToTable("ClassificationReadModels");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.OrderId).IsRequired();
            builder.Property(x => x.Classification).IsRequired().HasMaxLength(128);
            builder.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(256);
            builder.HasIndex(x => x.IdempotencyKey).IsUnique();
            builder.Property(x => x.PublishedAt).IsRequired();
            builder.Property(x => x.ProcessedAt).IsRequired();
        });

        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.ToTable("OutboxMessages");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.EventType).IsRequired().HasMaxLength(256);
            builder.Property(x => x.Payload).IsRequired();
            builder.Property(x => x.CorrelationId).HasMaxLength(128);
            builder.Property(x => x.Status).IsRequired().HasMaxLength(32);
            builder.Property(x => x.OccurredAt).IsRequired();
            builder.Property(x => x.DispatchedAt);
            builder.Property(x => x.Error).HasMaxLength(1024);
        });
    }

}


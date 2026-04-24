using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Abstractions;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;
using TaskManagement.Domain.Users;

namespace TaskManagement.Application.UnitTests.Common;

// Single-project-level shim around an EF Core InMemory DbContext so handler tests
// can run against a fast, in-process store that still exercises the real LINQ
// translation (unlike NSubstituting the DbSet, which fakes too much).
public sealed class InMemoryApplicationDbContext : DbContext, IApplicationDbContext
{
    public InMemoryApplicationDbContext(DbContextOptions<InMemoryApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var task = modelBuilder.Entity<TaskItem>();
        task.HasKey(t => t.Id);
        task.Property(t => t.Id)
            .HasConversion(v => v.Value, v => new TaskId(v));
        task.Property(t => t.Status)
            .HasConversion(s => s.Name, name => TaskItemStatus.FromName(name));
        task.Property(t => t.Priority)
            .HasConversion(p => p.Name, name => TaskPriority.FromName(name));
        task.Property(t => t.Title).HasMaxLength(TaskItem.MaxTitleLength).IsRequired();
        task.Property(t => t.Description).HasMaxLength(TaskItem.MaxDescriptionLength);
        task.Property(t => t.OrderKey).HasPrecision(38, 18);
        task.PrimitiveCollection<List<Guid>>("_tagIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
        task.Ignore(t => t.TagIds);
        task.Ignore(t => t.DomainEvents);

        var tag = modelBuilder.Entity<Tag>();
        tag.HasKey(t => t.Id);
        tag.Property(t => t.Id).HasConversion(v => v.Value, v => new TagId(v));
        tag.Property(t => t.Name).IsRequired().HasMaxLength(Tag.MaxNameLength);
        tag.Ignore(t => t.DomainEvents);

        var refresh = modelBuilder.Entity<RefreshToken>();
        refresh.HasKey(t => t.Id);
        refresh.Property(t => t.Id)
            .HasConversion(v => v.Value, v => new RefreshTokenId(v));
        refresh.Property(t => t.TokenHash).IsRequired();
        refresh.Ignore(t => t.DomainEvents);
    }

    public static InMemoryApplicationDbContext CreateNew()
    {
        var options = new DbContextOptionsBuilder<InMemoryApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new InMemoryApplicationDbContext(options);
    }
}

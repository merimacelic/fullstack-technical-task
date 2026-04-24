using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Tasks;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

internal sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(v => v.Value, v => new TaskId(v))
            .ValueGeneratedNever();

        builder.Property(t => t.OwnerId)
            .IsRequired();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(TaskItem.MaxTitleLength);

        builder.Property(t => t.Description)
            .HasMaxLength(TaskItem.MaxDescriptionLength);

        builder.Property(t => t.Status)
            .IsRequired()
            .HasMaxLength(32)
            .HasConversion(
                s => s.Name,
                name => TaskItemStatus.FromName(name));

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasMaxLength(32)
            .HasConversion(
                p => p.Name,
                name => TaskPriority.FromName(name));

        builder.Property(t => t.DueDateUtc);
        builder.Property(t => t.CreatedAtUtc).IsRequired();
        builder.Property(t => t.UpdatedAtUtc).IsRequired();
        builder.Property(t => t.CompletedAtUtc);

        builder.Property(t => t.OrderKey)
            .IsRequired()
            .HasPrecision(38, 18);

        // Tag associations stored as a JSON array of Guids via EF 8 primitive collection.
        // Referential integrity is enforced in the application layer — `DeleteTag`
        // sweeps associated task rows before removing the Tag aggregate.
        builder
            .PrimitiveCollection<List<Guid>>("_tagIds")
            .HasColumnName("TagIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(t => t.TagIds);
        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => t.OwnerId);
        builder.HasIndex(t => t.Status);
        builder.HasIndex(t => t.Priority);
        builder.HasIndex(t => t.DueDateUtc);
        builder.HasIndex(t => t.CreatedAtUtc);
        builder.HasIndex(t => new { t.OwnerId, t.OrderKey });
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Tasks.Tags;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

internal sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasConversion(v => v.Value, v => new TagId(v))
            .ValueGeneratedNever();

        builder.Property(t => t.OwnerId).IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(Tag.MaxNameLength);

        builder.Property(t => t.CreatedAtUtc).IsRequired();

        builder.Ignore(t => t.DomainEvents);

        builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique();
    }
}

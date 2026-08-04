using Employee360.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Employee360.Infrastructure.Persistence.Configurations;

/// <summary>EF Core mapping for <see cref="EmailTemplate"/>.</summary>
public sealed class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .HasMaxLength(16)
            .IsRequired();

        builder.HasIndex(t => t.Code)
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_Code");

        builder.Property(t => t.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.Subject)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.BodyHtml)
            .IsRequired();

        builder.Ignore(t => t.DomainEvents);
    }
}

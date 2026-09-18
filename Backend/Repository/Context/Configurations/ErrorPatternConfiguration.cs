using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repository.EFEntities;

namespace Repository.Context.Configurations;

public class ErrorPatternConfiguration : IEntityTypeConfiguration<ErrorPattern>
{
    public void Configure(EntityTypeBuilder<ErrorPattern> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ErrorCategory)
            .IsRequired()
            .HasMaxLength(Constants.DefaultStringMaxLength);

        builder.Property(p => p.RuleId)
            .IsRequired()
            .HasMaxLength(Constants.DefaultStringMaxLength);

        builder.Property(p => p.Count)
            .IsRequired();

        // One unique pattern entry per user per rule
        builder.HasIndex(p => new { p.UserId, p.RuleId })
            .IsUnique();

        builder.HasOne(p => p.User)
            .WithMany(u => u.ErrorPatterns)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repository.EFEntities;

namespace Repository.Context.Configurations;

public class SubmissionErrorConfiguration : IEntityTypeConfiguration<SubmissionError>
{
    public void Configure(EntityTypeBuilder<SubmissionError> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.ErrorCategory)
            .IsRequired()
            .HasMaxLength(Constants.DefaultStringMaxLength);

        builder.Property(e => e.RuleId)
            .IsRequired(false)
            .HasMaxLength(Constants.DefaultStringMaxLength);

        builder.Property(e => e.Message)
            .IsRequired();

        builder.Property(e => e.BadText)
            .IsRequired(false);

        builder.Property(e => e.SuggestionsJson)
            .IsRequired()
            .HasColumnType("text");
    }
}

using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repository.EFEntities;

namespace Repository.Context.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.TextContent)
            .IsRequired()
            .HasMaxLength(Constants.MaxTextLength);

        builder.Property(s => s.PredictedCefr)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(s => s.ConfidenceJson)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(s => s.AiFeedback)
            .IsRequired(false);

        builder.Property(s => s.SubmittedAt)
            .IsRequired();

        builder.HasOne(s => s.User)
            .WithMany(u => u.Submissions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(s => s.Errors)
            .WithOne(e => e.Submission)
            .HasForeignKey(e => e.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

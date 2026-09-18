using Domain.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repository.EFEntities;

namespace Repository.Context.Configurations;

public class WritingPromptConfiguration : IEntityTypeConfiguration<WritingPrompt>
{
    public void Configure(EntityTypeBuilder<WritingPrompt> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.CefrLevel)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(p => p.PromptDe).IsRequired();
        builder.Property(p => p.PromptEn).IsRequired();

        builder.Property(p => p.TopicTag)
            .IsRequired(false)
            .HasMaxLength(Constants.DefaultStringMaxLength);
    }
}

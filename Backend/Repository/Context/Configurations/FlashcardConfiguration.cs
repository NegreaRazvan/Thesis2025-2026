using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repository.EFEntities;

namespace Repository.Context.Configurations;

public class FlashcardConfiguration : IEntityTypeConfiguration<Flashcard>
{
    public void Configure(EntityTypeBuilder<Flashcard> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Front).IsRequired();
        builder.Property(f => f.Back).IsRequired();
        builder.Property(f => f.EaseFactor).IsRequired();
        builder.Property(f => f.IntervalDays).IsRequired();
        builder.Property(f => f.Repetitions).IsRequired();
        builder.Property(f => f.NextReview).IsRequired();

        builder.HasOne(f => f.User)
            .WithMany(u => u.Flashcards)
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

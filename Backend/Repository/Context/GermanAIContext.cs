using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Repository.EFEntities;
using System.Reflection;

namespace Repository.Context;

public class GermanAIContext(DbContextOptions<GermanAIContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<Submission> Submissions { get; set; }
    public DbSet<SubmissionError> SubmissionErrors { get; set; }
    public DbSet<ErrorPattern> ErrorPatterns { get; set; }
    public DbSet<Flashcard> Flashcards { get; set; }
    public DbSet<WritingPrompt> WritingPrompts { get; set; }

    public DbSet<FlashcardSet> FlashcardSets { get; set; }
    public DbSet<FlashcardCard> FlashcardCards { get; set; }

    public DbSet<UserAchievement> UserAchievements { get; set; }
    public DbSet<VocabGameSession> VocabGameSessions { get; set; }
    public DbSet<CoachSession> CoachSessions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<FlashcardSet>(e =>
        {
            e.HasKey(s => s.Id);
            e.ToTable("FlashcardSets");
            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(s => s.Cards)
             .WithOne(c => c.FlashcardSet)
             .HasForeignKey(c => c.FlashcardSetId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FlashcardCard>(e =>
        {
            e.HasKey(c => c.Id);
            e.ToTable("FlashcardCards");
        });

        modelBuilder.Entity<UserAchievement>(e =>
        {
            e.HasKey(a => a.Id);
            e.ToTable("UserAchievements");
            e.HasIndex(a => new { a.UserId, a.AchievementKey }).IsUnique();
            e.Property(a => a.AchievementKey).HasMaxLength(50);
            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VocabGameSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.ToTable("VocabGameSessions");
            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CoachSession>(e =>
        {
            e.HasKey(s => s.Id);
            e.ToTable("CoachSessions");
            e.HasOne(s => s.User)
             .WithMany()
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

/*
 * MIGRATION � run these commands after updating entities:
 *
 *   Add-Migration RenamePackagesToFlashcardSets -StartupProject Backend
 *   Update-Database -StartupProject Backend
 *
 * Or via dotnet CLI:
 *   dotnet ef migrations add RenamePackagesToFlashcardSets --project Repository --startup-project Backend
 *   dotnet ef database update --project Repository --startup-project Backend
 *
 * The migration will automatically create the new FlashcardSets and FlashcardCards tables.
 * If you want to preserve old data, manually add these lines to the migration Up() method:
 *
 *   migrationBuilder.Sql("INSERT INTO \"FlashcardSets\" SELECT * FROM \"FlashcardPackages\"");
 *   migrationBuilder.Sql("INSERT INTO \"FlashcardCards\" (\"Id\", \"FlashcardSetId\", \"Front\", \"Back\") SELECT \"Id\", \"PackageId\", \"Front\", \"Back\" FROM \"PackageCards\"");
 *   migrationBuilder.DropTable("PackageCards");
 *   migrationBuilder.DropTable("FlashcardPackages");
 */
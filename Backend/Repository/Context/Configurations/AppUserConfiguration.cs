using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Repository.EFEntities;

namespace Repository.Context.Configurations;

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.LastLogin).IsRequired(false);

        builder.Property(u => u.DisplayName).HasMaxLength(80).IsRequired(false);
        builder.Property(u => u.Bio).HasMaxLength(500).IsRequired(false);
        builder.Property(u => u.TargetCefrLevel).HasMaxLength(2).IsRequired(false);
        builder.Property(u => u.NativeLanguage).HasMaxLength(50).IsRequired(false);
        builder.Property(u => u.LearningSince).IsRequired(false);
        builder.Property(u => u.AvatarBytes).IsRequired(false);
        builder.Property(u => u.AvatarContentType).HasMaxLength(50).IsRequired(false);
    }
}

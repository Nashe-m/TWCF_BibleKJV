using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Domain.Enums;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class UserSettingConfiguration : IEntityTypeConfiguration<UserSettingEntity>
    {
        public void Configure(EntityTypeBuilder<UserSettingEntity> builder)
        {
            builder.ToTable("UserSettings");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FontScale).IsRequired();
            builder.Property(x => x.Theme).IsRequired();
            builder.Property(x => x.LastTranslationId).IsRequired().HasDefaultValue(1);
            builder.Property(x => x.ShowWordsOfJesus).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.LineSpacing).IsRequired().HasDefaultValue(1.45);
            builder.Property(x => x.ShowVerseNumbers).IsRequired().HasDefaultValue(true);
            builder.Property(x => x.ProjectorTheme).IsRequired().HasDefaultValue(ProjectorTheme.SameAsApp);
            builder.Property(x => x.ProjectorMargins).IsRequired().HasDefaultValue(ProjectorMargins.Normal);
            builder.Property(x => x.ShowAnnotationsWhenProjecting).IsRequired().HasDefaultValue(false);
            builder.Property(x => x.MarkJumpWhenProjecting).IsRequired().HasDefaultValue(true);
        }
    }
}

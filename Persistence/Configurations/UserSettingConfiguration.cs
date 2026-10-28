using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
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
        }
    }
}

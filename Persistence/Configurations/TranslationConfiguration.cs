using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class TranslationConfiguration : IEntityTypeConfiguration<TranslationEntity>
    {
        public void Configure(EntityTypeBuilder<TranslationEntity> builder)
        {
            builder.ToTable("Translations");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(10);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        }
    }
}

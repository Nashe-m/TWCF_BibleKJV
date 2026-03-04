using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class HighlightConfiguration : IEntityTypeConfiguration<HighlightEntity>
    {
        public void Configure(EntityTypeBuilder<HighlightEntity> builder)
        {
            builder.ToTable("Highlights");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.BookId).IsRequired();
            builder.Property(x => x.ChapterNumber).IsRequired();
            builder.Property(x => x.StartVerseNumber).IsRequired();
            builder.Property(x => x.EndVerseNumber).IsRequired();
            builder.Property(x => x.ColorHex).IsRequired().HasMaxLength(20);
            builder.Property(x => x.CreatedUtc).IsRequired();

            builder.HasIndex(x => new { x.BookId, x.ChapterNumber });
        }
    }
}

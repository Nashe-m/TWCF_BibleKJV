using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class VerseConfiguration : IEntityTypeConfiguration<VerseEntity>
    {
        public void Configure(EntityTypeBuilder<VerseEntity> builder)
        {
            builder.ToTable("Verses");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TranslationId).IsRequired();
            builder.Property(x => x.BookId).IsRequired();
            builder.Property(x => x.ChapterNumber).IsRequired();
            builder.Property(x => x.VerseNumber).IsRequired();
            builder.Property(x => x.Text).IsRequired();

            builder.HasIndex(x => new { x.BookId, x.ChapterNumber });
            builder.HasIndex(x => new { x.BookId, x.ChapterNumber, x.VerseNumber });

            builder.HasOne(v => v.Book).WithMany().HasForeignKey(v => v.BookId);
        }


    }
}

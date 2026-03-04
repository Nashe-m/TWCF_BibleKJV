using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class ChapterConfiguration : IEntityTypeConfiguration<ChapterEntity>
    {
        public void Configure(EntityTypeBuilder<ChapterEntity> builder)
        {
            builder.ToTable("Chapters");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.BookId).IsRequired();
            builder.Property(x => x.Number).IsRequired();
            builder.Property(x => x.VerseCount).IsRequired();

            builder.HasIndex(x => new { x.BookId, x.Number }).IsUnique();
        }
    }
}

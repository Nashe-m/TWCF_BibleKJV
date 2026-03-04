using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class NoteConfiguration : IEntityTypeConfiguration<NoteEntity>
    {
        public void Configure(EntityTypeBuilder<NoteEntity> builder)
        {
            builder.ToTable("Notes");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.BookId).IsRequired();
            builder.Property(x => x.ChapterNumber).IsRequired();
            builder.Property(x => x.StartVerseNumber).IsRequired();
            builder.Property(x => x.Text).IsRequired();
            builder.Property(x => x.CreatedUtc).IsRequired();

            builder.HasIndex(x => new { x.BookId, x.ChapterNumber });
        }
    }
}

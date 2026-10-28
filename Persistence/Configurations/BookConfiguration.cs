using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TWCF_BibleApp.Domain.Enums;
using TWCF_BibleApp.Persistence.Entities;

namespace TWCF_BibleApp.Persistence.Configurations
{
    public sealed class BookConfiguration : IEntityTypeConfiguration<BookEntity>
    {
        public void Configure(EntityTypeBuilder<BookEntity> builder)
        {
            builder.ToTable("Books");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
            builder.Property(x => x.OrderNumber).IsRequired();
            // bible_data.db stores the testament as 'OT' / 'NT'.
            builder.Property(x => x.Testament).IsRequired()
                .HasConversion(t => t == Testament.New ? "NT" : "OT", s => s == "NT" ? Testament.New : Testament.Old);
        }
    }
}

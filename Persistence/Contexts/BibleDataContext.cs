using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace TWCF_BibleApp.Persistence.Contexts
{
    public sealed class BibleDataContext : DbContext
    {
        public BibleDataContext(DbContextOptions<BibleDataContext> options) : base(options)
        {
        }

        // DbSets will be defined using explicit entity types in Persistence/Configurations
        public DbSet<Persistence.Entities.TranslationEntity> Translations => Set<Persistence.Entities.TranslationEntity>();
        public DbSet<Persistence.Entities.BookEntity> Books => Set<Persistence.Entities.BookEntity>();
        public DbSet<Persistence.Entities.ChapterEntity> Chapters => Set<Persistence.Entities.ChapterEntity>();
        public DbSet<Persistence.Entities.VerseEntity> Verses => Set<Persistence.Entities.VerseEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // load configurations from assembly
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(modelBuilder);
        }
    }
}

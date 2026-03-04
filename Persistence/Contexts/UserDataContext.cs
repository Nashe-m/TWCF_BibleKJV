
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace TWCF_BibleApp.Persistence.Contexts
{
    public sealed class UserDataContext : DbContext
    {
        public UserDataContext(DbContextOptions<UserDataContext> options) : base(options)
        {
        }

        public DbSet<Persistence.Entities.UserSettingEntity> UserSettings => Set<Persistence.Entities.UserSettingEntity>();
        public DbSet<Persistence.Entities.NoteEntity> Notes => Set<Persistence.Entities.NoteEntity>();
        public DbSet<Persistence.Entities.HighlightEntity> Highlights => Set<Persistence.Entities.HighlightEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace TWCF_BibleApp.Persistence.Contexts
{
    // Design-time factory for EF tools
    public class MigrationsDbContextFactory : IDesignTimeDbContextFactory<UserDataContext>
    {
        public UserDataContext CreateDbContext(string[] args)
        {
            var folder = Directory.GetCurrentDirectory();
            var dbPath = Path.Combine(folder, "user_data.db");
            var options = new DbContextOptionsBuilder<UserDataContext>()
                .UseSqlite($"Data Source={dbPath}")
                .Options;

            return new UserDataContext(options);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TWCF_BibleApp.Persistence.Contexts;
using TWCF_BibleApp.Persistence.Repositories;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Presentation.ViewModels;
using TWCF_BibleApp.Presentation.Views;
using System.Diagnostics;

namespace TWCF_BibleApp
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; }
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("SegoeUI.ttf", "SegoeUI");
                });

            // Configure databases
            var bibleDbPath = Path.Combine(FileSystem.AppDataDirectory, "bible_data.db");
            var userDbPath = Path.Combine(FileSystem.AppDataDirectory, "user_data.db");



            // If bundled bible_data.db exists in app package (Resources/Raw), copy it to AppDataDirectory on first run
            try
            {
                var bundled = Path.Combine(FileSystem.AppDataDirectory, "bible_data.db");
                // On MAUI the bundled resource will be available via FileSystem.OpenAppPackageFileAsync
                Task.Run(async () =>
                {
                    if (!File.Exists(bibleDbPath))
                    {
                        try
                        {
                            using var stream = await FileSystem.OpenAppPackageFileAsync("bible_data.db");
                            using var fs = File.Create(bibleDbPath);
                            await stream.CopyToAsync(fs);
                        }
                        catch
                        {
                            // ignore if no bundled DB present
                        }
                    }
                }).GetAwaiter().GetResult();
            }
            catch { }

            // BibleDataContext (read-only) - use SQLite file copied to AppDataDirectory beforehand
            builder.Services.AddDbContext<BibleDataContext>(options =>
            {
                options.UseSqlite($"Data Source={bibleDbPath};Mode=ReadOnly");
            });

            builder.Services.AddDbContext<UserDataContext>(options =>
            {
                options.UseSqlite(
                    $"Data Source={userDbPath}",
                    b => b.MigrationsAssembly("TWCF_BibleApp"));
            });

            // Repositories
            builder.Services.AddScoped<IBibleRepository, BibleRepository>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Application services
            builder.Services.AddSingleton<BibleService>();
            builder.Services.AddSingleton<UserService>();

            // ViewModels and Pages
            builder.Services.AddTransient<ReaderViewModel>();
            builder.Services.AddTransient<ReaderPage>();

            builder.Services.AddTransient<SearchViewModel>();
            builder.Services.AddTransient<SearchPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            var app = builder.Build();

            // Ensure user database created
            using (var scope = app.Services.CreateScope())
            {
                var userDb = scope.ServiceProvider.GetRequiredService<UserDataContext>();
                userDb.Database.Migrate();
            }

            return app;
        }
    }
}

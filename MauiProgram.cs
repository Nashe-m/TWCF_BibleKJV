using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TWCF_BibleApp.Persistence.Contexts;
using TWCF_BibleApp.Persistence.Repositories;
using TWCF_BibleApp.Application.Interfaces;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Presentation.Services;
using TWCF_BibleApp.Presentation.ViewModels;
using TWCF_BibleApp.Presentation.Views;
using System.Diagnostics;

namespace TWCF_BibleApp
{
    public static class MauiProgram
    {
        // Bump whenever Resources/Raw/bible_data.db is regenerated (see Tools/PrepareBibleData.md).
        private const int BibleDataVersion = 3;
        private const string BibleDataVersionKey = "BibleDataVersion";

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



            // Copy the bundled bible_data.db (Resources/Raw) to AppDataDirectory on first run,
            // and again whenever BibleDataVersion is bumped so new translations reach existing installs.
            Task.Run(() => CopyBundledBibleDataAsync(bibleDbPath)).GetAwaiter().GetResult();

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
            builder.Services.AddSingleton<AppSettings>();

            // ViewModels and Pages
            builder.Services.AddTransient<ReaderViewModel>();
            builder.Services.AddTransient<ReaderPage>();

            builder.Services.AddTransient<SearchViewModel>();
            builder.Services.AddTransient<SearchPage>();

            builder.Services.AddTransient<NotesViewModel>();
            builder.Services.AddTransient<NotesPage>();
            builder.Services.AddTransient<HighlightsViewModel>();
            builder.Services.AddTransient<HighlightsPage>();

            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<SettingsPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

#if WINDOWS
            // WinUI switches reserve room for an "On"/"Off" label we never show; drop it so they
            // sit tight against the right edge of settings rows.
            Microsoft.Maui.Handlers.SwitchHandler.Mapper.AppendToMapping("NoOnOffText", (handler, _) =>
            {
                handler.PlatformView.OnContent = null;
                handler.PlatformView.OffContent = null;
                handler.PlatformView.MinWidth = 0;
            });
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

        private static async Task CopyBundledBibleDataAsync(string bibleDbPath)
        {
            // An empty file means an earlier copy was interrupted; copy it again.
            if (File.Exists(bibleDbPath) && new FileInfo(bibleDbPath).Length > 0
                && Preferences.Default.Get(BibleDataVersionKey, 0) >= BibleDataVersion)
                return;

            try
            {
                // Copy to a temp file first so a failed copy never leaves a half-written database.
                var tempPath = bibleDbPath + ".tmp";
                using (var stream = await FileSystem.OpenAppPackageFileAsync("bible_data.db"))
                using (var fs = File.Create(tempPath))
                {
                    await stream.CopyToAsync(fs);
                }

                File.Move(tempPath, bibleDbPath, overwrite: true);
                Preferences.Default.Set(BibleDataVersionKey, BibleDataVersion);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to copy bundled bible_data.db: {ex}");
            }
        }
    }
}

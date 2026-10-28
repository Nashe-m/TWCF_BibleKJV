using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain;
using TWCF_BibleApp.Domain.Enums;
using TWCF_BibleApp.Persistence.Contexts;
using TWCF_BibleApp.Persistence.Repositories;
using Xunit;

namespace TWCF_BibleApp.Tests;

/// <summary>Notes, highlights and settings against a fresh, fully migrated user database.</summary>
public sealed class UserDataTests : IDisposable
{
    private const int John = 43;
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"twcf_user_{Guid.NewGuid():N}.db");
    private readonly UserDataContext _db;
    private readonly UserService _service;

    public UserDataTests()
    {
        _db = new UserDataContext(new DbContextOptionsBuilder<UserDataContext>().UseSqlite($"Data Source={_path}").Options);
        _db.Database.Migrate();
        _service = new UserService(new UserRepository(_db));
    }

    public void Dispose()
    {
        _db.Dispose();
        SqliteConnection.ClearAllPools();
        File.Delete(_path);
    }

    private static string Show(IEnumerable<Highlight> highlights) =>
        string.Join(" ", highlights.OrderBy(h => h.StartVerseNumber).Select(h => $"{h.StartVerseNumber}-{h.EndVerseNumber}:{h.ColorHex}"));

    [Fact]
    public async Task Highlighting_part_of_a_highlight_splits_it()
    {
        await _service.SetHighlightAsync(John, 3, new[] { 16, 17, 18, 19, 20 }, "Y");
        var after = await _service.SetHighlightAsync(John, 3, new[] { 18 }, "G");

        Assert.Equal("16-17:Y 18-18:G 19-20:Y", Show(after));
    }

    [Fact]
    public async Task Removing_a_highlight_keeps_the_rest()
    {
        await _service.SetHighlightAsync(John, 3, new[] { 16, 17, 18 }, "Y");
        var after = await _service.SetHighlightAsync(John, 3, new[] { 17 }, null);

        Assert.Equal("16-16:Y 18-18:Y", Show(after));
    }

    [Fact]
    public async Task Separate_verses_become_separate_highlights()
    {
        var after = await _service.SetHighlightAsync(John, 3, new[] { 1, 2, 5 }, "B");

        Assert.Equal("1-2:B 5-5:B", Show(after));
    }

    [Fact]
    public async Task Highlights_stay_in_their_chapter()
    {
        await _service.SetHighlightAsync(John, 3, new[] { 16 }, "Y");

        Assert.Empty(await _service.GetHighlightsAsync(John, 4));
        Assert.Single(await _service.GetAllHighlightsAsync());
    }

    [Fact]
    public async Task Saved_dates_survive_reloading()
    {
        var note = await _service.AddNoteAsync(new Note(0, John, 3, 16, 18, "God's love"));
        await Task.Delay(50);
        var reloaded = (await _service.GetNotesAsync(John, 3)).Single();

        Assert.True(note.Id > 0);
        Assert.Equal(note.CreatedUtc, reloaded.CreatedUtc);
        Assert.Equal(18, reloaded.EndVerseNumber);
    }

    [Fact]
    public async Task Notes_can_be_edited_and_deleted()
    {
        var note = await _service.AddNoteAsync(new Note(0, John, 3, 16, null, "first"));
        await _service.UpdateNoteAsync(new Note { Id = note.Id, Text = "edited", ModifiedUtc = DateTime.UtcNow });

        var edited = (await _service.GetAllNotesAsync()).Single();
        Assert.Equal("edited", edited.Text);
        Assert.NotNull(edited.ModifiedUtc);

        await _service.DeleteNoteAsync(note.Id);
        Assert.Empty(await _service.GetAllNotesAsync());
    }

    [Fact]
    public async Task Settings_round_trip_including_display_options()
    {
        await _service.SaveUserSettingAsync(new UserSetting(0, John, 3, 2.0, ThemePreference.Dark, 2, false)
        {
            LineSpacing = 1.7,
            ShowVerseNumbers = false,
            ProjectorTheme = ProjectorTheme.Dark,
            ProjectorMargins = ProjectorMargins.Wide,
            ShowAnnotationsWhenProjecting = true,
            MarkJumpWhenProjecting = false,
        });
        // Saving again updates the single row rather than adding one.
        await _service.SaveUserSettingAsync(new UserSetting(0, John, 4, 2.0, ThemePreference.Dark, 2, false)
        {
            LineSpacing = 1.7, ShowVerseNumbers = false, ProjectorTheme = ProjectorTheme.Dark,
            ProjectorMargins = ProjectorMargins.Wide, ShowAnnotationsWhenProjecting = true, MarkJumpWhenProjecting = false,
        });

        var s = await _service.GetUserSettingAsync();
        Assert.NotNull(s);
        Assert.Equal(1, await _db.UserSettings.CountAsync());
        Assert.Equal((John, 4, ThemePreference.Dark, 2, false), (s!.LastBookId, s.LastChapterNumber, s.Theme, s.LastTranslationId, s.ShowWordsOfJesus));
        Assert.Equal((1.7, false, ProjectorTheme.Dark, ProjectorMargins.Wide, true, false),
            (s.LineSpacing, s.ShowVerseNumbers, s.ProjectorTheme, s.ProjectorMargins, s.ShowAnnotationsWhenProjecting, s.MarkJumpWhenProjecting));
    }

    [Fact]
    public async Task Calls_from_several_pages_at_once_do_not_collide()
    {
        // The reader, Notes and Highlights pages share one context through UserService.
        var work = Enumerable.Range(1, 20).Select(i => i % 2 == 0
            ? (Task)_service.AddNoteAsync(new Note(0, John, 3, i, null, $"note {i}"))
            : _service.GetAllHighlightsAsync());
        await Task.WhenAll(work);

        Assert.Equal(10, (await _service.GetAllNotesAsync()).Count());
    }
}

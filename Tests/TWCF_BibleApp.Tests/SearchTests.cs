using Microsoft.EntityFrameworkCore;
using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Domain.Enums;
using TWCF_BibleApp.Persistence.Contexts;
using TWCF_BibleApp.Persistence.Repositories;
using Xunit;

namespace TWCF_BibleApp.Tests;

public class MatchExpressionTests
{
    [Theory]
    [InlineData("love", false, false, "\"love\"")]
    [InlineData("love one another", false, false, "\"love\" \"one\" \"another\"")]
    [InlineData("\"the lord is\" shep", false, false, "\"the lord is\" \"shep\"")]
    [InlineData("shep", true, false, "\"shep\"*")]
    [InlineData("shep ", true, false, "\"shep\"")]                 // a finished word isn't extended
    [InlineData("\"the lord\"", true, false, "\"the lord\"")]      // nor is a phrase
    [InlineData("love god", false, true, "\"love\"* \"god\"*")]
    [InlineData("\"my shepherd\" love", false, true, "\"my shepherd\" \"love\"*")]
    [InlineData("Lord's", false, false, "\"Lord s\"")]
    public void Builds_fts_queries(string input, bool asYouType, bool wordForms, string expected)
    {
        Assert.Equal(expected, BibleRepository.BuildMatchExpression(input, asYouType, wordForms));
    }

    // Anything FTS5 would treat as syntax is dropped, so user input can never break the query.
    [Theory]
    [InlineData("a\"b(c)*")]
    [InlineData("NEAR OR AND -x ^y")]
    [InlineData("col:value")]
    public void Neutralises_fts_syntax(string input)
    {
        var match = BibleRepository.BuildMatchExpression(input, asYouType: false);

        Assert.NotNull(match);
        Assert.DoesNotContain("(", match);
        Assert.DoesNotContain(":", match);
        // Only quoted terms (words or phrases), each optionally followed by *.
        Assert.Matches("^\"[^\"]+\"\\*?( \"[^\"]+\"\\*?)*$", match!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!?,.")]
    public void Nothing_to_search_gives_null(string input)
    {
        Assert.Null(BibleRepository.BuildMatchExpression(input, asYouType: true));
    }
}

/// <summary>Runs searches against the bundled bible_data.db (copied next to the test assembly).</summary>
public sealed class SearchDatabaseTests : IDisposable
{
    private const int Kjv = 1, Web = 2;
    private readonly BibleDataContext _db;
    private readonly BibleRepository _repo;

    public SearchDatabaseTests()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "bible_data.db");
        _db = new BibleDataContext(new DbContextOptionsBuilder<BibleDataContext>().UseSqlite($"Data Source={path};Mode=ReadOnly").Options);
        _repo = new BibleRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Books_are_in_order_with_correct_testaments()
    {
        var books = (await _repo.GetBooksAsync()).ToList();

        Assert.Equal(66, books.Count);
        Assert.Equal(Enumerable.Range(1, 66), books.Select(b => b.Order));
        Assert.All(books.Take(39), b => Assert.Equal(Testament.Old, b.Testament));
        Assert.All(books.Skip(39), b => Assert.Equal(Testament.New, b.Testament));
        Assert.Equal("Matthew", books[39].Name);
    }

    [Fact]
    public async Task Finds_and_highlights_words()
    {
        var results = await _repo.SearchVersesAsync(new SearchRequest("love one another", Kjv));

        Assert.Equal(19, results.TotalCount);
        var first = results.Items[0];
        Assert.Equal("John 13:34", first.Reference);
        Assert.Contains("<m>love</m> <m>one</m> <m>another</m>", first.HighlightedText);
    }

    [Fact]
    public async Task Bible_order_and_best_match_differ()
    {
        var bible = await _repo.SearchVersesAsync(new SearchRequest("love one another", Kjv));
        var best = await _repo.SearchVersesAsync(new SearchRequest("love one another", Kjv) { Order = SearchOrder.BestMatch });

        Assert.Equal(bible.TotalCount, best.TotalCount);
        // John 13:35 ("love one to another") lacks the exact phrase, so best match moves it down.
        Assert.Equal("John 13:35", bible.Items[1].Reference);
        Assert.NotEqual("John 13:35", best.Items[1].Reference);
        Assert.Contains("love one another", best.Items[1].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Word_forms_find_more()
    {
        var exact = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv));
        var forms = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv) { WordForms = true });

        Assert.True(forms.TotalCount > exact.TotalCount);
    }

    [Fact]
    public async Task Scope_limits_results()
    {
        var all = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv));
        var nt = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv) { Scope = new SearchScope(Testament.New), Take = 500 });
        var john1 = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv) { Scope = new SearchScope(BookId: 62), Take = 500 });

        Assert.InRange(nt.TotalCount, 1, all.TotalCount - 1);
        Assert.All(nt.Items, r => Assert.True(r.BookId >= 40));
        Assert.All(john1.Items, r => Assert.Equal(62, r.BookId));
    }

    [Fact]
    public async Task Paging_continues_where_it_left_off()
    {
        var first = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv) { Take = 10 });
        var second = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv) { Skip = 10, Take = 10 });

        Assert.Equal(first.TotalCount, second.TotalCount);
        Assert.Empty(first.Items.Select(r => r.Reference).Intersect(second.Items.Select(r => r.Reference)));
    }

    [Fact]
    public async Task Counts_per_book_add_up_to_the_total()
    {
        var request = new SearchRequest("love one another", Kjv);
        var results = await _repo.SearchVersesAsync(request);
        var counts = await _repo.CountMatchesByBookAsync(request);

        Assert.Equal(results.TotalCount, counts.Sum(c => c.Count));
        Assert.Equal(43, counts[0].BookId); // John comes first in Bible order
    }

    [Fact]
    public async Task Each_version_is_searched_separately()
    {
        var kjv = await _repo.SearchVersesAsync(new SearchRequest("love", Kjv));
        var web = await _repo.SearchVersesAsync(new SearchRequest("love", Web));

        Assert.NotEqual(kjv.TotalCount, web.TotalCount);
    }

    [Fact]
    public async Task Hostile_input_returns_nothing_instead_of_failing()
    {
        var results = await _repo.SearchVersesAsync(new SearchRequest("a\"b(c)*", Kjv));

        Assert.Equal(0, results.TotalCount);
    }
}

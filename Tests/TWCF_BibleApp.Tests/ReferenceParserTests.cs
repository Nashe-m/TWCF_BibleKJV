using TWCF_BibleApp.Application.DTOs;
using TWCF_BibleApp.Application.Services;
using TWCF_BibleApp.Domain.Enums;
using Xunit;

namespace TWCF_BibleApp.Tests;

public class ReferenceParserTests
{
    // Names as they are stored in bible_data.db.
    private static readonly BookDto[] Books =
    {
        new() { Id = 1, Name = "Genesis", Order = 1, Testament = Testament.Old },
        new() { Id = 9, Name = "I Samuel", Order = 9, Testament = Testament.Old },
        new() { Id = 12, Name = "II Kings", Order = 12, Testament = Testament.Old },
        new() { Id = 19, Name = "Psalms", Order = 19, Testament = Testament.Old },
        new() { Id = 22, Name = "Song of Solomon", Order = 22, Testament = Testament.Old },
        new() { Id = 23, Name = "Isaiah", Order = 23, Testament = Testament.Old },
        new() { Id = 41, Name = "Mark", Order = 41, Testament = Testament.New },
        new() { Id = 43, Name = "John", Order = 43, Testament = Testament.New },
        new() { Id = 45, Name = "Romans", Order = 45, Testament = Testament.New },
        new() { Id = 46, Name = "I Corinthians", Order = 46, Testament = Testament.New },
        new() { Id = 62, Name = "I John", Order = 62, Testament = Testament.New },
        new() { Id = 66, Name = "Revelation of John", Order = 66, Testament = Testament.New },
    };

    [Theory]
    [InlineData("Jn 3:16", "John 3:16")]
    [InlineData("john 3.16", "John 3:16")]
    [InlineData("Jn 3:16-18", "John 3:16–18")]
    [InlineData("Jn 3:18-16", "John 3:16–18")]
    [InlineData("Rom 8:28, 38-39", "Romans 8:28, 38–39")]
    [InlineData("1 Cor 13", "1 Corinthians 13")]
    [InlineData("I Cor 13", "1 Corinthians 13")]
    [InlineData("Psalm 23", "Psalm 23")]
    [InlineData("ps 23:1", "Psalm 23:1")]
    [InlineData("II Kings 2.11", "2 Kings 2:11")]
    [InlineData("1 jn 4:8", "1 John 4:8")]
    [InlineData("rev 22", "Revelation of John 22")]
    [InlineData("song 2", "Song of Solomon 2")]
    public void Parses_references(string input, string expected)
    {
        var reference = ReferenceParser.Parse(input, Books);

        Assert.NotNull(reference);
        Assert.Equal(expected, reference!.Display);
        Assert.True(reference.IsExplicit);
    }

    [Theory]
    [InlineData("John")]
    [InlineData("mark")]
    [InlineData("Psalms")]
    [InlineData("ps")]
    public void Whole_book_names_and_abbreviations_are_explicit(string input)
    {
        var reference = ReferenceParser.Parse(input, Books);

        Assert.NotNull(reference);
        Assert.True(reference!.IsExplicit);
        Assert.Equal(1, reference.Chapter);
        Assert.Empty(reference.Verses);
    }

    // Partial book names are more likely word searches: Enter searches, the dropdown still offers the book.
    [Theory]
    [InlineData("son", "Song of Solomon")]
    [InlineData("is", "Isaiah")]
    [InlineData("gen", "Genesis")]
    public void Partial_book_names_are_not_explicit(string input, string book)
    {
        var reference = ReferenceParser.Parse(input, Books);

        Assert.NotNull(reference);
        Assert.Equal(book, reference!.Book.Name);
        Assert.False(reference.IsExplicit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("love one another")]
    [InlineData("x 3:16")]
    [InlineData("John 0")]
    [InlineData("John 3:0")]
    public void Rejects_non_references(string input)
    {
        Assert.Null(ReferenceParser.Parse(input, Books));
    }

    [Fact]
    public void Includes_checks_every_range()
    {
        var reference = ReferenceParser.Parse("Rom 8:28, 38-39", Books)!;

        Assert.True(reference.Includes(28));
        Assert.True(reference.Includes(39));
        Assert.False(reference.Includes(30));
        Assert.Equal(28, reference.FirstVerse);
    }

    [Theory]
    [InlineData("I Samuel", "1 Samuel")]
    [InlineData("II Kings", "2 Kings")]
    [InlineData("III John", "3 John")]
    [InlineData("Isaiah", "Isaiah")]
    public void Display_names_use_digits(string name, string expected)
    {
        Assert.Equal(expected, ReferenceParser.DisplayBookName(name));
    }

    [Fact]
    public void Psalms_reads_as_psalm_before_a_chapter()
    {
        Assert.Equal("Psalm", ReferenceParser.ReferenceBookName("Psalms"));
        Assert.Equal("1 Samuel", ReferenceParser.ReferenceBookName("I Samuel"));
    }
}

public class VerseRangeTests
{
    [Fact]
    public void Groups_verses_into_runs()
    {
        var runs = VerseRange.FromVerses(new[] { 20, 16, 17, 18, 18, 1 });

        Assert.Equal(new[] { new VerseRange(1, 1), new VerseRange(16, 18), new VerseRange(20, 20) }, runs);
    }

    [Fact]
    public void Empty_input_gives_no_runs()
    {
        Assert.Empty(VerseRange.FromVerses(Array.Empty<int>()));
    }

    [Theory]
    [InlineData(16, 16, "16")]
    [InlineData(16, 18, "16–18")]
    public void Formats_with_an_en_dash(int start, int end, string expected)
    {
        Assert.Equal(expected, new VerseRange(start, end).ToString());
    }
}

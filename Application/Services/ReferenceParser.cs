using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TWCF_BibleApp.Application.DTOs;

namespace TWCF_BibleApp.Application.Services
{
    /// <summary>An inclusive run of verses, e.g. 16-18. Start == End for a single verse.</summary>
    public sealed record VerseRange(int Start, int End)
    {
        public bool Contains(int verse) => verse >= Start && verse <= End;

        /// <summary>Groups verse numbers into runs: 16, 17, 18, 20 -> 16-18, 20.</summary>
        public static IReadOnlyList<VerseRange> FromVerses(IEnumerable<int> verses)
        {
            var runs = new List<VerseRange>();
            foreach (var v in verses.Distinct().OrderBy(v => v))
            {
                if (runs.Count > 0 && runs[^1].End == v - 1) runs[^1] = runs[^1] with { End = v };
                else runs.Add(new VerseRange(v, v));
            }
            return runs;
        }
        public override string ToString() => Start == End ? $"{Start}" : $"{Start}–{End}";
    }

    /// <summary>
    /// A parsed reference. Verses is empty for a whole chapter.
    /// IsExplicit is false when the input was only a partial book name ("son", "is"), which is
    /// more likely a word search than a request for Song of Solomon or Isaiah.
    /// </summary>
    public sealed record BibleReference(BookDto Book, int Chapter, IReadOnlyList<VerseRange> Verses, bool IsExplicit)
    {
        public int? FirstVerse => Verses.Count == 0 ? null : Verses[0].Start;
        public bool Includes(int verse) => Verses.Any(r => r.Contains(verse));

        public string Display
        {
            get
            {
                var book = ReferenceParser.ReferenceBookName(Book.Name);
                return Verses.Count == 0 ? $"{book} {Chapter}" : $"{book} {Chapter}:{string.Join(", ", Verses)}";
            }
        }
    }

    /// <summary>
    /// Parses typed references such as "Jn 3:16", "Jn 3:16-18", "Rom 8:28, 38-39", "1 Cor 13",
    /// "Psalm 23" or "II Kings 2.11".
    /// </summary>
    public static class ReferenceParser
    {
        private static readonly Regex ReferenceRegex = new(
            @"^\s*(?<book>(?:[1-3]|i{1,3})?\s*[a-z][a-z .]*?)\s*(?<chapter>\d+)?" +
            @"(?:\s*[:.]\s*(?<verses>\d+(?:\s*[-–—]\s*\d+)?(?:\s*,\s*\d+(?:\s*[-–—]\s*\d+)?)*))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex RangeRegex = new(@"(?<start>\d+)(?:\s*[-–—]\s*(?<end>\d+))?", RegexOptions.Compiled);

        // Common abbreviations that are not simple prefixes of the book name.
        private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["gn"] = "genesis", ["dt"] = "deuteronomy", ["jdg"] = "judges", ["jdgs"] = "judges",
            ["ps"] = "psalms", ["psa"] = "psalms", ["pss"] = "psalms", ["psalm"] = "psalms", ["prv"] = "proverbs",
            ["sos"] = "songofsolomon", ["sng"] = "songofsolomon", ["song"] = "songofsolomon",
            ["ezk"] = "ezekiel", ["jl"] = "joel", ["nah"] = "nahum",
            ["mt"] = "matthew", ["mk"] = "mark", ["mrk"] = "mark", ["lk"] = "luke", ["luk"] = "luke",
            ["jn"] = "john", ["jhn"] = "john", ["php"] = "philippians", ["phm"] = "philemon",
            ["jas"] = "james", ["jude"] = "jude", ["rev"] = "revelationofjohn", ["revelation"] = "revelationofjohn",
            ["1jn"] = "1john", ["2jn"] = "2john", ["3jn"] = "3john",
        };

        public static BibleReference? Parse(string? input, IEnumerable<BookDto> books)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var match = ReferenceRegex.Match(input);
            if (!match.Success) return null;

            var token = NormaliseBookName(match.Groups["book"].Value);
            if (token.Length < 2) return null;
            var isAbbreviation = Abbreviations.TryGetValue(token, out var full);
            if (isAbbreviation) token = full!;

            var book = books
                .OrderBy(b => b.Order)
                .FirstOrDefault(b => NormaliseBookName(b.Name).StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (book == null) return null;

            var hasChapter = match.Groups["chapter"].Success;
            var chapter = hasChapter ? int.Parse(match.Groups["chapter"].Value) : 1;
            if (chapter < 1) return null;

            var verses = new List<VerseRange>();
            if (match.Groups["verses"].Success)
            {
                foreach (Match r in RangeRegex.Matches(match.Groups["verses"].Value))
                {
                    var start = int.Parse(r.Groups["start"].Value);
                    var end = r.Groups["end"].Success ? int.Parse(r.Groups["end"].Value) : start;
                    if (start < 1) return null;
                    verses.Add(new VerseRange(Math.Min(start, end), Math.Max(start, end)));
                }
            }

            var isExplicit = hasChapter || isAbbreviation
                || string.Equals(NormaliseBookName(book.Name), token, StringComparison.OrdinalIgnoreCase);
            return new BibleReference(book, chapter, verses, isExplicit);
        }

        /// <summary>"I Samuel" -> "1samuel", "Song of Solomon" -> "songofsolomon".</summary>
        public static string NormaliseBookName(string name)
        {
            var s = Regex.Replace(name.Trim().ToLowerInvariant(), @"^(iii|ii|i)(\s+|(?=\d)|$)", m => m.Groups[1].Value.Length + " ");
            return Regex.Replace(s, @"[\s.]", "");
        }

        /// <summary>"I Samuel" -> "1 Samuel" for display.</summary>
        public static string DisplayBookName(string name) =>
            Regex.Replace(name, @"^(III|II|I)\s+", m => m.Groups[1].Value.Length + " ");

        /// <summary>The book name as written before a chapter: "Psalms" -> "Psalm" (Psalm 23).</summary>
        public static string ReferenceBookName(string name) => name == "Psalms" ? "Psalm" : DisplayBookName(name);
    }
}

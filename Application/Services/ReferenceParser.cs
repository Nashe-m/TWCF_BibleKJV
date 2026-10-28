using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TWCF_BibleApp.Application.DTOs;

namespace TWCF_BibleApp.Application.Services
{
    public sealed record BibleReference(BookDto Book, int Chapter, int? Verse);

    /// <summary>
    /// Parses typed references such as "Jn 3:16", "1 Cor 13", "Psalm 23" or "II Kings 2.11".
    /// </summary>
    public static class ReferenceParser
    {
        private static readonly Regex ReferenceRegex = new(
            @"^\s*(?<book>(?:[1-3]|i{1,3})?\s*[a-z][a-z .]*?)\s*(?<chapter>\d+)?(?:\s*[:.]\s*(?<verse>\d+))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Common abbreviations that are not simple prefixes of the book name.
        private static readonly Dictionary<string, string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
        {
            ["gn"] = "genesis", ["dt"] = "deuteronomy", ["jdg"] = "judges", ["jdgs"] = "judges",
            ["ps"] = "psalms", ["psa"] = "psalms", ["pss"] = "psalms", ["prv"] = "proverbs",
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
            if (Abbreviations.TryGetValue(token, out var full)) token = full;

            var book = books
                .OrderBy(b => b.Order)
                .FirstOrDefault(b => NormaliseBookName(b.Name).StartsWith(token, StringComparison.OrdinalIgnoreCase));
            if (book == null) return null;

            var chapter = match.Groups["chapter"].Success ? int.Parse(match.Groups["chapter"].Value) : 1;
            int? verse = match.Groups["verse"].Success ? int.Parse(match.Groups["verse"].Value) : null;
            return chapter < 1 ? null : new BibleReference(book, chapter, verse);
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
    }
}

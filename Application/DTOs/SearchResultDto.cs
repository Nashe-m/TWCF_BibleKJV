using System;
using System.Collections.Generic;
using TWCF_BibleApp.Domain.Enums;

namespace TWCF_BibleApp.Application.DTOs
{
    public sealed class SearchResultDto
    {
        public int BookId { get; set; }
        public int Chapter { get; set; }
        public int Verse { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        // Text with the matched words wrapped in <m>...</m>.
        public string HighlightedText { get; set; } = string.Empty;
    }

    /// <summary>One page of search results; TotalCount is the number of matches across all pages.</summary>
    public sealed class SearchResultsDto
    {
        public static readonly SearchResultsDto Empty = new();

        public int TotalCount { get; set; }
        public IReadOnlyList<SearchResultDto> Items { get; set; } = Array.Empty<SearchResultDto>();
    }

    /// <summary>Limits a search to one testament or one book. Null fields mean no limit.</summary>
    public sealed record SearchScope(Testament? Testament = null, int? BookId = null)
    {
        public static readonly SearchScope All = new();
    }

    public enum SearchOrder
    {
        Bible,      // Genesis to Revelation
        BestMatch,  // the exact phrase first, then by relevance
    }

    /// <summary>
    /// A verse search. AsYouType also matches longer forms of the word still being typed
    /// ("shep" finds shepherd); WordForms does that for every word ("love" finds loved, loveth).
    /// </summary>
    public sealed record SearchRequest(string Query, int TranslationId)
    {
        public SearchScope Scope { get; init; } = SearchScope.All;
        public int Skip { get; init; }
        public int Take { get; init; } = 50;
        public bool AsYouType { get; init; }
        public bool WordForms { get; init; }
        public SearchOrder Order { get; init; } = SearchOrder.Bible;
    }

    /// <summary>How many verses in a book match a search.</summary>
    public sealed record BookMatchCount(int BookId, int Count);
}

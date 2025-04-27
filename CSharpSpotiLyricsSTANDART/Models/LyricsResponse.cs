/*
Author : s*rp
Purpose Of File : Model for the internal Spotify lyrics endpoint response.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CSharpSpotiLyrics.Core.Models
{
    public class LyricsResponse
    {
        [JsonPropertyName("lyrics")]
        public LyricsData? Lyrics { get; set; }

        [JsonPropertyName("colors")]
        public ColorData? Colors { get; set; } // Might be useful later

        [JsonPropertyName("hasVocalRemoval")]
        public bool HasVocalRemoval { get; set; }
    }

    public class LyricsData
    {
        [JsonPropertyName("syncType")]
        public string? SyncType { get; set; } // e.g., "LINE_SYNCED", "UNSYNCED"

        [JsonPropertyName("lines")]
        public List<LyricsLine>? Lines { get; set; }

        [JsonPropertyName("provider")]
        public string? Provider { get; set; }

        [JsonPropertyName("providerLyricsId")]
        public string? ProviderLyricsId { get; set; }

        [JsonPropertyName("providerDisplayName")]
        public string? ProviderDisplayName { get; set; }

        [JsonPropertyName("syncLyricsUri")]
        public string? SyncLyricsUri { get; set; }

        [JsonPropertyName("isDenseTypeface")]
        public bool IsDenseTypeface { get; set; }

        [JsonPropertyName("alternatives")]
        public List<object>? Alternatives { get; set; } // Define further if needed

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("isRtlLanguage")]
        public bool IsRtlLanguage { get; set; }

        [JsonPropertyName("fullscreenAction")]
        public string? FullscreenAction { get; set; }

        [JsonPropertyName("showUpsell")]
        public bool ShowUpsell { get; set; }
    }

    public class LyricsLine
    {
        [JsonPropertyName("startTimeMs")]
        public string? StartTimeMs { get; set; } // Keep as string for flexibility, parse to long/int when needed

        [JsonPropertyName("words")]
        public string? Words { get; set; }

        [JsonPropertyName("syllables")]
        public List<object>? Syllables { get; set; } // Define further if needed

        [JsonPropertyName("endTimeMs")]
        public string? EndTimeMs { get; set; }
    }

    public class ColorData // Example structure, adjust if needed
    {
        [JsonPropertyName("background")]
        public int? Background { get; set; }

        [JsonPropertyName("text")]
        public int? Text { get; set; }

        [JsonPropertyName("highlightText")]
        public int? HighlightText { get; set; }
    }
}

/*
Author : s*rp
Purpose Of File : Model for Spotify User object.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Text.Json.Serialization;

namespace CSharpSpotiLyrics.Core.Models
{
    public class SpotifyUser
    {
        [JsonPropertyName("display_name")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("followers")]
        public FollowersObject? Followers { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; } // From 'me' endpoint

        [JsonPropertyName("product")]
        public string? Product { get; set; } // From 'me' endpoint

        [JsonPropertyName("explicit_content")]
        public ExplicitContentSettingsObject? ExplicitContent { get; set; } // From 'me' endpoint

        [JsonPropertyName("email")]
        public string? Email { get; set; } // From 'me' endpoint (if scope permits)
    }

    // --- Helper classes used by SpotifyUser and potentially others ---

    public class ImageObject
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }
    }

    public class FollowersObject
    {
        [JsonPropertyName("href")]
        public string? Href { get; set; } // Always null in the followers object returned by Get User Profile

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class ExplicitContentSettingsObject
    {
        [JsonPropertyName("filter_enabled")]
        public bool FilterEnabled { get; set; }

        [JsonPropertyName("filter_locked")]
        public bool FilterLocked { get; set; }
    }
}

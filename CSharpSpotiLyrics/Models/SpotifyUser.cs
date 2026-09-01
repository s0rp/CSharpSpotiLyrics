/*
Author : s*rp
Purpose Of File : Model for Spotify User object.
Date : 24.04.2025
Update: 23.01.2026
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
        public List<ImageObjectt>? Images { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("followers")]
        public FollowersObject? Followers { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("product")]
        public string? Product { get; set; } 

        [JsonPropertyName("explicit_content")]
        public ExplicitContentSettingsObject? ExplicitContent { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }


    public class ImageObjectt
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
        public string? Href { get; set; }

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

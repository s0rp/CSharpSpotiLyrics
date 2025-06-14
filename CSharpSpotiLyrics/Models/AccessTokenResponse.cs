/*
Author : s*rp
Purpose Of File : Model for Spotify access token response.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Text.Json.Serialization;

namespace CSharpSpotiLyrics.Core.Models
{
    public class AccessTokenResponse
    {
        [JsonPropertyName("accessToken")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("accessTokenExpirationTimestampMs")]
        public long AccessTokenExpirationTimestampMs { get; set; }

        [JsonPropertyName("isAnonymous")]
        public bool IsAnonymous { get; set; }

        [JsonPropertyName("clientId")]
        public string? ClientId { get; set; }
    }
}

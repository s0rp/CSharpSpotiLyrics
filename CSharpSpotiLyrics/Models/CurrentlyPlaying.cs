/*
Author : s*rp
Purpose Of File : Model for Currently Playing Context from Spotify API.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Text.Json.Serialization;

namespace CSharpSpotiLyrics.Core.Models
{
    // Represents the full context returned by /me/player/currently-playing
    public class CurrentlyPlayingContext
    {
        [JsonPropertyName("device")]
        public DeviceObject? Device { get; set; }

        [JsonPropertyName("repeat_state")]
        public string? RepeatState { get; set; } // "off", "track", "context"

        [JsonPropertyName("shuffle_state")]
        public bool ShuffleState { get; set; }

        [JsonPropertyName("context")]
        public ContextObject? Context { get; set; }

        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; }

        [JsonPropertyName("progress_ms")]
        public int? ProgressMs { get; set; }

        [JsonPropertyName("is_playing")]
        public bool IsPlaying { get; set; }

        [JsonPropertyName("item")]
        public SpotifyTrack? Item { get; set; } // Can be Track or Episode object

        [JsonPropertyName("currently_playing_type")]
        public string? CurrentlyPlayingType { get; set; } // "track", "episode", "ad", "unknown"

        [JsonPropertyName("actions")]
        public ActionsObject? Actions { get; set; }
    }

    public class DeviceObject
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("is_private_session")]
        public bool IsPrivateSession { get; set; }

        [JsonPropertyName("is_restricted")]
        public bool IsRestricted { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // e.g., "Computer", "Smartphone"

        [JsonPropertyName("volume_percent")]
        public int? VolumePercent { get; set; }
    }

    public class ContextObject
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; } // "album", "artist", "playlist"

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    public class ActionsObject
    {
        [JsonPropertyName("disallows")]
        public DisallowsObject? Disallows { get; set; }
    }

    public class DisallowsObject // Indicates actions NOT allowed
    {
        [JsonPropertyName("interrupting_playback")]
        public bool? InterruptingPlayback { get; set; }

        [JsonPropertyName("pausing")]
        public bool? Pausing { get; set; }

        // ... other potential disallows like resuming, seeking, skipping_next, etc.
        [JsonPropertyName("resuming")]
        public bool? Resuming { get; set; }

        [JsonPropertyName("seeking")]
        public bool? Seeking { get; set; }

        [JsonPropertyName("skipping_next")]
        public bool? SkippingNext { get; set; }

        [JsonPropertyName("skipping_prev")]
        public bool? SkippingPrev { get; set; }

        [JsonPropertyName("toggling_repeat_context")]
        public bool? TogglingRepeatContext { get; set; }

        [JsonPropertyName("toggling_shuffle")]
        public bool? TogglingShuffle { get; set; }

        [JsonPropertyName("toggling_repeat_track")]
        public bool? TogglingRepeatTrack { get; set; }

        [JsonPropertyName("transferring_playback")]
        public bool? TransferringPlayback { get; set; }
    }

    // --- You'll also need models for SpotifyTrack, SpotifyAlbum, SpotifyPlaylist etc. ---
    // --- See Spotify Web API documentation for their structure ---
    // Example: SpotifyTrack (simplified)
    public class SpotifyTrack
    {
        [JsonPropertyName("album")]
        public SimpleAlbumObject? Album { get; set; }

        [JsonPropertyName("artists")]
        public List<SimpleArtistObject>? Artists { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string>? AvailableMarkets { get; set; }

        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("external_ids")]
        public Dictionary<string, string>? ExternalIds { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("is_playable")]
        public bool? IsPlayable { get; set; } // Note: Nullable

        [JsonPropertyName("linked_from")]
        public LinkedTrackObject? LinkedFrom { get; set; }

        [JsonPropertyName("restrictions")]
        public RestrictionsObject? Restrictions { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // Should be "track"

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }

    // --- Helper objects for Track ---
    public class SimpleAlbumObject
    {
        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; } // album, single, compilation

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string>? AvailableMarkets { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("release_date_precision")]
        public string? ReleaseDatePrecision { get; set; } // year, month, day

        [JsonPropertyName("restrictions")]
        public RestrictionsObject? Restrictions { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // album

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("artists")] // Sometimes included even in SimpleAlbum
        public List<SimpleArtistObject>? Artists { get; set; }
    }

    public class SimpleArtistObject
    {
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // artist

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    public class LinkedTrackObject
    {
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // track

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    public class RestrictionsObject
    {
        [JsonPropertyName("reason")]
        public string? Reason { get; set; } // market, product, explicit
    }

    // --- Generic Paging Object ---
    public class PagingObject<T>
    {
        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("items")]
        public List<T>? Items { get; set; }

        [JsonPropertyName("limit")]
        public int Limit { get; set; }

        [JsonPropertyName("next")]
        public string? Next { get; set; }

        [JsonPropertyName("offset")]
        public int Offset { get; set; }

        [JsonPropertyName("previous")]
        public string? Previous { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    // --- Specific items used in paging ---
    public class SimpleTrackObject // Used in Album tracks
    {
        [JsonPropertyName("artists")]
        public List<SimpleArtistObject>? Artists { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string>? AvailableMarkets { get; set; }

        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("is_playable")]
        public bool? IsPlayable { get; set; }

        [JsonPropertyName("linked_from")]
        public LinkedTrackObject? LinkedFrom { get; set; }

        [JsonPropertyName("restrictions")]
        public RestrictionsObject? Restrictions { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // track

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }

    public class PlaylistItem // Used in Playlist tracks
    {
        [JsonPropertyName("added_at")]
        public DateTime? AddedAt { get; set; } // Use DateTime? and handle potential parsing issues

        [JsonPropertyName("added_by")]
        public SpotifyUser? AddedBy { get; set; } // Simplified, might need PublicUserObject

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }

        [JsonPropertyName("primary_color")]
        public string? PrimaryColor { get; set; } // Usually null

        [JsonPropertyName("track")]
        public SpotifyTrack? Track { get; set; } // Can be Track or Episode object

        [JsonPropertyName("video_thumbnail")]
        public VideoThumbnail? VideoThumbnail { get; set; }
    }

    public class VideoThumbnail
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
    }

    public class SimplePlaylistObject // Used in GetCurrentUserPlaylists
    {
        [JsonPropertyName("collaborative")]
        public bool Collaborative { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("owner")]
        public SpotifyUser? Owner { get; set; } // Simplified, PublicUserObject

        [JsonPropertyName("public")]
        public bool? Public { get; set; } // Nullable

        [JsonPropertyName("snapshot_id")]
        public string? SnapshotId { get; set; }

        [JsonPropertyName("tracks")]
        public PlaylistTracksRef? Tracks { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // playlist

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    public class PlaylistTracksRef
    {
        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    public class SavedAlbumObject // Used in GetCurrentUserSavedAlbums
    {
        [JsonPropertyName("added_at")]
        public DateTime? AddedAt { get; set; }

        [JsonPropertyName("album")]
        public SpotifyAlbum? Album { get; set; }
    }

    // --- Need full SpotifyAlbum model ---
    public class SpotifyAlbum
    {
        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; }

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("available_markets")]
        public List<string>? AvailableMarkets { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("release_date")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("release_date_precision")]
        public string? ReleaseDatePrecision { get; set; }

        [JsonPropertyName("restrictions")]
        public RestrictionsObject? Restrictions { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // album

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("artists")]
        public List<SimpleArtistObject>? Artists { get; set; } // List of simplified artists

        [JsonPropertyName("tracks")]
        public PagingObject<SimpleTrackObject>? Tracks { get; set; } // Paging object for tracks within the album

        // ... potentially copyrights, external_ids, genres, label, popularity
        [JsonPropertyName("copyrights")]
        public List<CopyrightObject>? Copyrights { get; set; }

        [JsonPropertyName("external_ids")]
        public Dictionary<string, string>? ExternalIds { get; set; }

        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }

        [JsonPropertyName("label")]
        public string? Label { get; set; }

        [JsonPropertyName("popularity")]
        public int? Popularity { get; set; }
    }

    public class CopyrightObject
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; } // C = copyright, P = performance
    }

    // --- Need full SpotifyPlaylist model ---
    public class SpotifyPlaylist
    {
        [JsonPropertyName("collaborative")]
        public bool Collaborative { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; }

        [JsonPropertyName("followers")]
        public FollowersObject? Followers { get; set; }

        [JsonPropertyName("href")]
        public string? Href { get; set; }

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("owner")]
        public SpotifyUser? Owner { get; set; } // Simplified, PublicUserObject

        [JsonPropertyName("public")]
        public bool? Public { get; set; } // Nullable

        [JsonPropertyName("snapshot_id")]
        public string? SnapshotId { get; set; }

        [JsonPropertyName("tracks")]
        public PagingObject<PlaylistItem>? Tracks { get; set; } // Paging object for tracks

        [JsonPropertyName("type")]
        public string? Type { get; set; } // playlist

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    // --- Response for GetMultipleTracks endpoint ---
    public class TracksResponse
    {
        [JsonPropertyName("tracks")]
        public List<SpotifyTrack?>? Tracks { get; set; } // List can contain nulls if a track ID was invalid
    }

    // --- Response for Search endpoint ---
    public class SearchResult
    {
        [JsonPropertyName("tracks")]
        public PagingObject<SpotifyTrack>? Tracks { get; set; }

        [JsonPropertyName("artists")]
        public PagingObject<SpotifyArtist>? Artists { get; set; } // Define SpotifyArtist if needed

        [JsonPropertyName("albums")]
        public PagingObject<SimpleAlbumObject>? Albums { get; set; } // Uses SimpleAlbumObject

        [JsonPropertyName("playlists")]
        public PagingObject<SimplePlaylistObject>? Playlists { get; set; } // Uses SimplePlaylistObject

        // ... potentially shows, episodes depending on search type
    }

    // Define SpotifyArtist if you search for artists
    public class SpotifyArtist : SimpleArtistObject // Inherit common fields
    {
        [JsonPropertyName("followers")]
        public FollowersObject? Followers { get; set; }

        [JsonPropertyName("genres")]
        public List<string>? Genres { get; set; }

        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }

        [JsonPropertyName("popularity")]
        public int? Popularity { get; set; }
    }
}

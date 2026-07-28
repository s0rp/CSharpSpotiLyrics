/*
Author : s*rp
Purpose Of File : Model for Currently Playing Context and GraphQL responses from Spotify API.
Date : 24.04.2025
Update: 23.01.2026
Supervisor : Dixiz 3A Neural (Coder MoE)
Revised: Restored all standard properties (Id, Explicit, ExternalUrls, etc.) required by CLI/Helpers.
*/
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CSharpSpotiLyrics.Core.Models
{

    public class GraphQLBody
    {
        [JsonPropertyName("variables")]
        public object Variables { get; set; }

        [JsonPropertyName("operationName")]
        public string OperationName { get; set; }

        [JsonPropertyName("extensions")]
        public GraphQLExtensions Extensions { get; set; }
    }

    public class GraphQLExtensions
    {
        [JsonPropertyName("persistedQuery")]
        public PersistedQuery PersistedQuery { get; set; }
    }

    public class PersistedQuery
    {
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        [JsonPropertyName("sha256Hash")]
        public string Sha256Hash { get; set; }
    }

    public class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }

    // --- Profile Data ---
    public class MeData
    {
        [JsonPropertyName("me")]
        public MeObject Me { get; set; }
    }

    public class MeObject
    {
        [JsonPropertyName("profile")]
        public ProfileObject Profile { get; set; }
        [JsonPropertyName("account")]
        public AccountObject Account { get; set; }
        [JsonPropertyName("libraryV3")]
        public LibraryV3Object LibraryV3 { get; set; }
    }

    public class ProfileObject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class AccountObject
    {
        [JsonPropertyName("country")]
        public string Country { get; set; }
        [JsonPropertyName("product")]
        public string Product { get; set; }
    }

    // --- Library Data ---
    public class LibraryV3Object
    {
        [JsonPropertyName("items")]
        public List<LibraryItemWrapper> Items { get; set; }
    }

    public class LibraryItemWrapper
    {
        [JsonPropertyName("item")]
        public LibraryItemData Item { get; set; }
        [JsonPropertyName("addedAt")]
        public AddedAtObject AddedAt { get; set; }
    }

    public class LibraryItemData
    {
        [JsonPropertyName("data")]
        public SimplePlaylistData Data { get; set; }
    }

    public class SimplePlaylistData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("images")]
        public PlaylistImages Images { get; set; }
        [JsonPropertyName("ownerV2")]
        public OwnerV2Wrapper OwnerV2 { get; set; }
    }

    // --- Playlist Data ---
    public class PlaylistData
    {
        [JsonPropertyName("playlistV2")]
        public PlaylistV2Object PlaylistV2 { get; set; }
    }

    public class PlaylistV2Object
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("description")]
        public string Description { get; set; }
        [JsonPropertyName("ownerV2")]
        public OwnerV2Wrapper OwnerV2 { get; set; }
        [JsonPropertyName("images")]
        public PlaylistImages Images { get; set; }
        [JsonPropertyName("content")]
        public PlaylistContent Content { get; set; }
    }

    public class PlaylistImages
    {
        [JsonPropertyName("items")]
        public List<PlaylistImageItem> Items { get; set; }
    }

    public class PlaylistImageItem
    {
        [JsonPropertyName("sources")]
        public List<ImageSource> Sources { get; set; }
    }

    public class ImageSource
    {
        [JsonPropertyName("url")]
        public string Url { get; set; }
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        [JsonPropertyName("height")]
        public int? Height { get; set; }
    }

    public class OwnerV2Wrapper
    {
        [JsonPropertyName("data")]
        public OwnerData Data { get; set; }
    }

    public class OwnerData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; }
    }

    public class PlaylistContent
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        [JsonPropertyName("items")]
        public List<PlaylistTrackItemWrapper> Items { get; set; }
    }

    public class PlaylistTrackItemWrapper
    {
        [JsonPropertyName("itemV2")]
        public TrackResponseWrapper ItemV2 { get; set; }
        [JsonPropertyName("addedAt")]
        public AddedAtObject AddedAt { get; set; }
    }

    public class TrackResponseWrapper
    {
        [JsonPropertyName("data")]
        public TrackData Data { get; set; }
    }

    public class TrackData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("trackDuration")]
        public DurationObject TrackDuration { get; set; }
        [JsonPropertyName("albumOfTrack")]
        public AlbumOfTrack AlbumOfTrack { get; set; }
        [JsonPropertyName("artists")]
        public ArtistList Artists { get; set; }
        [JsonPropertyName("contentRating")]
        public ContentRating ContentRating { get; set; }
        [JsonPropertyName("playability")]
        public Playability Playability { get; set; }
        [JsonPropertyName("discNumber")]
        public int DiscNumber { get; set; }
        [JsonPropertyName("trackNumber")]
        public int TrackNumber { get; set; }
    }

    public class DurationObject { [JsonPropertyName("totalMilliseconds")] public int TotalMilliseconds { get; set; } }
    public class ContentRating { [JsonPropertyName("label")] public string Label { get; set; } }
    public class Playability { [JsonPropertyName("playable")] public bool Playable { get; set; } }

    public class AlbumOfTrack
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("coverArt")]
        public PlaylistImages CoverArt { get; set; }
    }

    public class ArtistList
    {
        [JsonPropertyName("items")]
        public List<ArtistProfileWrapper> Items { get; set; }
    }

    public class ArtistProfileWrapper
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("profile")]
        public ArtistProfile Profile { get; set; }
    }

    public class ArtistProfile { [JsonPropertyName("name")] public string Name { get; set; } }


    public class AlbumData
    {
        [JsonPropertyName("albumUnion")]
        public AlbumUnionObject AlbumUnion { get; set; }
    }

    public class AlbumUnionObject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("date")]
        public DateObject Date { get; set; }

        [JsonPropertyName("coverArt")]
        public AlbumCoverArt CoverArt { get; set; }

        [JsonPropertyName("artists")]
        public ArtistList Artists { get; set; }

        [JsonPropertyName("tracksV2")]
        public AlbumTracksV2 TracksV2 { get; set; }

        [JsonPropertyName("label")]
        public string Label { get; set; }

        [JsonPropertyName("copyright")]
        public CopyrightWrapper Copyright { get; set; }

        [JsonPropertyName("sharingInfo")]
        public SharingInfoObject SharingInfo { get; set; } // Share URL için
    }

    public class DateObject
    {
        [JsonPropertyName("isoString")]
        public string IsoString { get; set; }
        [JsonPropertyName("precision")]
        public string Precision { get; set; }
    }

    public class AlbumCoverArt
    {
        [JsonPropertyName("sources")]
        public List<ImageSource> Sources { get; set; }

        [JsonPropertyName("extractedColors")]
        public ExtractedColorsObject ExtractedColors { get; set; }
    }

    public class ExtractedColorsObject
    {
        [JsonPropertyName("colorRaw")]
        public ColorWrapper ColorRaw { get; set; }
    }
    public class ColorWrapper { [JsonPropertyName("hex")] public string Hex { get; set; } }

    public class SharingInfoObject
    {
        [JsonPropertyName("shareUrl")]
        public string ShareUrl { get; set; }
        [JsonPropertyName("shareId")]
        public string ShareId { get; set; }
    }

    public class AlbumTracksV2
    {
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        [JsonPropertyName("items")]
        public List<AlbumTrackItem> Items { get; set; }
    }

    public class AlbumTrackItem
    {
        [JsonPropertyName("track")]
        public AlbumTrackData Track { get; set; }
        [JsonPropertyName("uid")]
        public string Uid { get; set; }
    }

    public class AlbumTrackData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("duration")]
        public DurationObject Duration { get; set; }

        [JsonPropertyName("trackDuration")]
        public DurationObject TrackDuration { get; set; }

        [JsonPropertyName("playcount")]
        public string Playcount { get; set; }

        [JsonPropertyName("discNumber")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("trackNumber")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("contentRating")]
        public ContentRating ContentRating { get; set; } // Explicit kontrolü için

        [JsonPropertyName("artists")]
        public ArtistList Artists { get; set; }

        [JsonPropertyName("playability")]
        public Playability Playability { get; set; }
    }

    public class CopyrightWrapper { [JsonPropertyName("items")] public List<CopyrightObject> Items { get; set; } }

    // --- Internal Metadata Models ---
    public class MetadataTrackResponse
    {
        [JsonPropertyName("gid")] public string? Gid { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("album")] public MetadataAlbum? Album { get; set; }
        [JsonPropertyName("artist")] public List<MetadataArtist>? Artist { get; set; }
        [JsonPropertyName("duration")] public int Duration { get; set; }
        [JsonPropertyName("canonical_uri")] public string? CanonicalUri { get; set; }
    }
    public class MetadataAlbum
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("cover_group")] public MetadataCoverGroup? CoverGroup { get; set; }
    }
    public class MetadataCoverGroup { [JsonPropertyName("image")] public List<MetadataImage>? Image { get; set; } }
    public class MetadataImage
    {
        [JsonPropertyName("file_id")] public string? FileId { get; set; }
        [JsonPropertyName("width")] public int Width { get; set; }
        [JsonPropertyName("height")] public int Height { get; set; }
    }
    public class MetadataArtist { [JsonPropertyName("name")] public string? Name { get; set; } }
    public class AddedAtObject { [JsonPropertyName("isoString")] public string IsoString { get; set; } }


    // ==========================================
    // STANDARD PUBLIC MODELS (Used by CLI & App)
    // ==========================================

    public class CurrentlyPlayingContext
    {
        [JsonPropertyName("device")] public DeviceObject? Device { get; set; }
        [JsonPropertyName("repeat_state")] public string? RepeatState { get; set; }
        [JsonPropertyName("shuffle_state")] public bool ShuffleState { get; set; }
        [JsonPropertyName("context")] public ContextObject? Context { get; set; }
        [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
        [JsonPropertyName("progress_ms")] public int? ProgressMs { get; set; }
        [JsonPropertyName("is_playing")] public bool IsPlaying { get; set; }
        [JsonPropertyName("item")] public SpotifyTrack? Item { get; set; }
        [JsonPropertyName("currently_playing_type")] public string? CurrentlyPlayingType { get; set; }
        [JsonPropertyName("actions")] public ActionsObject? Actions { get; set; }
    }

    public class DeviceObject
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("is_active")] public bool IsActive { get; set; }
        [JsonPropertyName("is_private_session")] public bool IsPrivateSession { get; set; }
        [JsonPropertyName("is_restricted")] public bool IsRestricted { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("volume_percent")] public int? VolumePercent { get; set; }
    }

    public class ContextObject
    {
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("href")] public string? Href { get; set; }
        [JsonPropertyName("external_urls")] public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();
        [JsonPropertyName("uri")] public string? Uri { get; set; }
    }

    public class ActionsObject { [JsonPropertyName("disallows")] public DisallowsObject? Disallows { get; set; } }

    public class DisallowsObject
    {
        [JsonPropertyName("interrupting_playback")] public bool? InterruptingPlayback { get; set; }
        [JsonPropertyName("pausing")] public bool? Pausing { get; set; }
        [JsonPropertyName("resuming")] public bool? Resuming { get; set; }
        [JsonPropertyName("seeking")] public bool? Seeking { get; set; }
        [JsonPropertyName("skipping_next")] public bool? SkippingNext { get; set; }
        [JsonPropertyName("skipping_prev")] public bool? SkippingPrev { get; set; }
        [JsonPropertyName("toggling_repeat_context")] public bool? TogglingRepeatContext { get; set; }
        [JsonPropertyName("toggling_shuffle")] public bool? TogglingShuffle { get; set; }
        [JsonPropertyName("toggling_repeat_track")] public bool? TogglingRepeatTrack { get; set; }
        [JsonPropertyName("transferring_playback")] public bool? TransferringPlayback { get; set; }
    }

    // --- Core Model: SpotifyTrack ---
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
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>(); // Init to avoid null ref

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

        [JsonPropertyName("popularity")]
        public int Popularity { get; set; }

        [JsonPropertyName("preview_url")]
        public string? PreviewUrl { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
    }

    // --- Helper Objects ---

    public class SimpleAlbumObject
    {
        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; }

        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();

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

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("uri")]
        public string? Uri { get; set; }

        [JsonPropertyName("artists")]
        public List<SimpleArtistObject>? Artists { get; set; }
    }

    public class SimpleArtistObject
    {
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();
        [JsonPropertyName("href")]
        public string? Href { get; set; }
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    public class SimpleTrackObject // Simplified version used inside Album responses
    {
        [JsonPropertyName("artists")]
        public List<SimpleArtistObject>? Artists { get; set; }

        [JsonPropertyName("disc_number")]
        public int DiscNumber { get; set; }

        [JsonPropertyName("duration_ms")]
        public int DurationMs { get; set; }

        [JsonPropertyName("explicit")]
        public bool Explicit { get; set; }

        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();

        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("track_number")]
        public int TrackNumber { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

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
        public string? Type { get; set; }
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    public class RestrictionsObject
    {
        [JsonPropertyName("reason")]
        public string? Reason { get; set; }
    }

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

    public class PlaylistItem
    {
        [JsonPropertyName("added_at")]
        public DateTime? AddedAt { get; set; }
        [JsonPropertyName("added_by")]
        public SpotifyUser? AddedBy { get; set; }
        [JsonPropertyName("is_local")]
        public bool IsLocal { get; set; }
        [JsonPropertyName("track")]
        public SpotifyTrack? Track { get; set; }
    }

    public class ImageObject
    {
        [JsonPropertyName("url")]
        public string? Url { get; set; }
        [JsonPropertyName("width")]
        public int? Width { get; set; }
        [JsonPropertyName("height")]
        public int? Height { get; set; }
    }

    public class SimplePlaylistObject
    {
        [JsonPropertyName("collaborative")]
        public bool Collaborative { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();
        [JsonPropertyName("href")]
        public string? Href { get; set; }
        [JsonPropertyName("id")]
        public string? Id { get; set; }
        [JsonPropertyName("images")]
        public List<ImageObject>? Images { get; set; }
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("owner")]
        public SpotifyUser? Owner { get; set; }
        [JsonPropertyName("public")]
        public bool? Public { get; set; }
        [JsonPropertyName("snapshot_id")]
        public string? SnapshotId { get; set; }
        [JsonPropertyName("tracks")]
        public PlaylistTracksRef? Tracks { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
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

    public class SavedAlbumObject
    {
        [JsonPropertyName("added_at")]
        public DateTime? AddedAt { get; set; }
        [JsonPropertyName("album")]
        public SpotifyAlbum? Album { get; set; }
    }

    public class SpotifyAlbum
    {
        [JsonPropertyName("album_type")]
        public string? AlbumType { get; set; }
        [JsonPropertyName("total_tracks")]
        public int TotalTracks { get; set; }
        [JsonPropertyName("available_markets")]
        public List<string>? AvailableMarkets { get; set; }
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();
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
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
        [JsonPropertyName("artists")]
        public List<SimpleArtistObject>? Artists { get; set; }
        [JsonPropertyName("tracks")]
        public PagingObject<SimpleTrackObject>? Tracks { get; set; }
        [JsonPropertyName("copyrights")]
        public List<CopyrightObject>? Copyrights { get; set; }
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
        public string? Type { get; set; }
    }

    public class SpotifyPlaylist
    {
        [JsonPropertyName("collaborative")]
        public bool Collaborative { get; set; }
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string>? ExternalUrls { get; set; } = new Dictionary<string, string>();
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
        public SpotifyUser? Owner { get; set; }
        [JsonPropertyName("public")]
        public bool? Public { get; set; }
        [JsonPropertyName("snapshot_id")]
        public string? SnapshotId { get; set; }
        [JsonPropertyName("tracks")]
        public PagingObject<PlaylistItem>? Tracks { get; set; }
        [JsonPropertyName("type")]
        public string? Type { get; set; }
        [JsonPropertyName("uri")]
        public string? Uri { get; set; }
    }

    /*public class SpotifyUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("display_name")]
        public string DisplayName { get; set; }
        [JsonPropertyName("country")]
        public string Country { get; set; }
        [JsonPropertyName("product")]
        public string Product { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; } = "user";
        [JsonPropertyName("uri")]
        public string Uri { get; set; }
        [JsonPropertyName("external_urls")]
        public Dictionary<string, string> ExternalUrls { get; set; } = new();
    }*/

    public class TracksResponse
    {
        [JsonPropertyName("tracks")]
        public List<SpotifyTrack?>? Tracks { get; set; }
    }

    public class SearchResult
    {
        [JsonPropertyName("tracks")]
        public PagingObject<SpotifyTrack>? Tracks { get; set; }
        [JsonPropertyName("artists")]
        public PagingObject<SpotifyArtist>? Artists { get; set; }
        [JsonPropertyName("albums")]
        public PagingObject<SimpleAlbumObject>? Albums { get; set; }
        [JsonPropertyName("playlists")]
        public PagingObject<SimplePlaylistObject>? Playlists { get; set; }
    }

    public class SpotifyArtist : SimpleArtistObject
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
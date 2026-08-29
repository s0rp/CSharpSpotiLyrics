# API Reference

Detailed documentation of public methods inside the `SpotifyClient` class.

## Initialization

### Constructor
```csharp
public SpotifyClient(string spDcToken)
```
- **spDcToken**: The `sp_dc` cookie value extracted from an active Spotify Web Player session.

---

## Authentication

### LoginAsync
```csharp
public async Task LoginAsync(bool force = false)
```
Authenticates the client using the provided `sp_dc` cookie. Calculates local and server-side TOTP tokens and exchanges them for an access token and client ID.
- **force**: Forces a fresh login, ignoring cached TOTP secrets.

---

## Player & Tracks

### GetCurrentSongAsync
```csharp
public async Task<CurrentlyPlayingContext?> GetCurrentSongAsync()
```
Fetches metadata of the currently playing track.
- **Returns**: `CurrentlyPlayingContext` containing track, progress, device, and active player options. Returns `null` if no song is active.

### GetLyricsAsync
```csharp
public async Task<LyricsResponse?> GetLyricsAsync(string trackId)
```
Fetches synchronized and line-synced lyrics for a specific track.
- **trackId**: The 22-character Base62 Spotify track ID (e.g., `4PTG3Z6ehGkBF36IccG1S8`).
- **Returns**: `LyricsResponse` with synced lyrics lines, colors, and sync type, or `null` if not found.

### GetTracksAsync
```csharp
public async Task<TracksResponse?> GetTracksAsync(IEnumerable<string> trackIds)
```
Fetches metadata for a list of track IDs concurrently via Spotify's REST metadata service. Throttled internally using `SemaphoreSlim` (max 10 concurrent requests).

---

## Visuals & Artists

### GetCanvasUrlAsync
```csharp
public async Task<string?> GetCanvasUrlAsync(string artistIdOrUri, string trackIdOrUri)
```
Fetches the Canvas MP4 video URL for a given track.
- **artistIdOrUri**: Spotify artist ID or URI.
- **trackIdOrUri**: Spotify track ID or URI.
- **Returns**: MP4 video URL, or `null` if the track has no canvas.

### GetArtistDetailsAsync
```csharp
public async Task<CustomArtistDetails?> GetArtistDetailsAsync(string artistUri)
```
Retrieves specific artist details like verified status, biography, and avatar image.

---

## Playlists & Albums

### GetPlaylistAsync
```csharp
public async Task<SpotifyPlaylist?> GetPlaylistAsync(string playlistId)
```
Fetches metadata and track list (first 100 tracks) of a playlist using the partner GraphQL API.

### GetAlbumAsync
```csharp
public async Task<SpotifyAlbum?> GetAlbumAsync(string albumId)
```
Fetches album tracks, artists, copyrights, and cover art metadata.
```
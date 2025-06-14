/*
Author : s*rp
Purpose Of File : Handles fetching, formatting, and saving lyrics.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using CSharpSpotiLyrics.Core.Api;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;
using CSharpSpotiLyrics.Core.Utils; // For HelperFunctions

namespace CSharpSpotiLyrics.Console.App
{
    public class LyricsHandler
    {
        private readonly SpotifyClient _client;
        private readonly Config _config;

        public LyricsHandler(SpotifyClient client, Config config)
        {
            _client = client;
            _config = config;
        }

        public async Task<(List<string> TrackIds, string? FolderName)> GetAlbumTracksAndFolderAsync(
            string albumUrlOrId
        )
        {
            string albumId = ExtractIdFromUrl(albumUrlOrId, "album");
            var albumData =
                await _client.GetAlbumAsync(albumId)
                ?? throw new ApiException($"Album not found: {albumId}");

            var folderData = new Dictionary<string, object>
            {
                { "Name", albumData.Name ?? "Unknown Album" },
                {
                    "Artists",
                    string.Join(
                        ',',
                        albumData.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                    )
                },
                // Add other relevant fields if needed for the format string
                { "Id", albumData.Id ?? "" },
                { "ReleaseDate", albumData.ReleaseDate ?? "" }
            };
            string folderName = HelperFunctions.RenameUsingFormat(
                _config.AlbumFolderName,
                folderData
            );

            System.Console.WriteLine($"> Album: {albumData.Name}");
            System.Console.WriteLine($"> Artist(s): {folderData["Artists"]}");
            System.Console.WriteLine(
                $"> Songs: {albumData.TotalTracks} Tracks",
                Environment.NewLine
            );

            var trackIds = await _client.GetAlbumTracksAsync(albumId, albumData.TotalTracks);
            return (trackIds, folderName);
        }

        public async Task<(
            List<string> TrackIds,
            string? FolderName
        )> GetPlaylistTracksAndFolderAsync(string playlistUrlOrId)
        {
            string playlistId = ExtractIdFromUrl(playlistUrlOrId, "playlist");
            var playData =
                await _client.GetPlaylistAsync(playlistId)
                ?? throw new ApiException($"Playlist not found: {playlistId}");

            var folderData = new Dictionary<string, object>
            {
                { "Name", playData.Name ?? "Unknown Playlist" },
                { "Owner", playData.Owner?.DisplayName ?? "Unknown Owner" },
                { "Collaborative", playData.Collaborative ? "[C]" : "" },
                // Add other relevant fields if needed
                { "Id", playData.Id ?? "" },
                { "Description", playData.Description ?? "" }
            };
            string folderName = HelperFunctions.RenameUsingFormat(
                _config.PlayFolderName,
                folderData
            );
            int totalTracks = playData.Tracks?.Total ?? 0; // Get total from Tracks paging object

            System.Console.WriteLine($"> Playlist: {playData.Name} {folderData["Collaborative"]}");
            System.Console.WriteLine($"> Owner: {folderData["Owner"]}");
            System.Console.WriteLine($"> Songs: {totalTracks} Tracks", Environment.NewLine);

            var trackIds = await _client.GetPlaylistTracksAsync(playlistId, totalTracks);
            return (trackIds, folderName);
        }

        public async Task<List<string>> DownloadLyricsForTracksAsync(
            List<string> trackIds,
            string? subFolder = null
        )
        {
            List<string> unableToFindLyrics = new();
            if (!trackIds.Any())
                return unableToFindLyrics;

            string targetFolder = _config.DownloadPath;
            if (!string.IsNullOrEmpty(subFolder))
            {
                if (_config.CreateFolder)
                {
                    targetFolder = Path.Combine(_config.DownloadPath, subFolder);
                    if (Directory.Exists(targetFolder) && !_config.ForceDownload)
                    {
                        System.Console.WriteLine(
                            $"Folder '{subFolder}' already exists. Skipping download (use --force to override)."
                        );
                        // Return empty list as we skipped the whole folder
                        return unableToFindLyrics;
                    }
                    Directory.CreateDirectory(targetFolder); // Create if not exists or if forcing
                }
                // If CreateFolder is false, subFolder is ignored, lyrics go to DownloadPath directly.
            }
            else
            {
                // Ensure base download path exists if not creating specific subfolders
                Directory.CreateDirectory(targetFolder);
            }

            System.Console.WriteLine($"Fetching details for {trackIds.Count} tracks...");
            List<SpotifyTrack?> fullTracksData = new();
            int CHUNK_SIZE = 50; // Spotify API limit for /tracks endpoint

            foreach (var idChunk in HelperFunctions.Chunk(trackIds, CHUNK_SIZE))
            {
                try
                {
                    var response = await _client.GetTracksAsync(idChunk);
                    if (response?.Tracks != null)
                    {
                        fullTracksData.AddRange(response.Tracks);
                    }
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine(
                        $"Error fetching track batch: {ex.Message}. Skipping {idChunk.Count} tracks in this batch."
                    );
                    // Optionally add these IDs to a separate 'failed fetch' list
                }
                await Task.Delay(50); // Small delay to avoid rate limiting
            }

            System.Console.WriteLine(
                $"Downloading lyrics ({fullTracksData.Count(t => t != null)} tracks found)..."
            );
            int downloadedCount = 0;
            int skippedCount = 0;

            var validTracks = fullTracksData.Where(t => t != null).ToList();

            // --- Progress reporting setup ---
            int total = validTracks.Count;
            int current = 0;
            object progressLock = new object(); // For thread safety if parallelized

            // --- Sequential download (simpler) ---
            foreach (var track in validTracks)
            {
                current++;
                if (
                    track == null
                    || string.IsNullOrEmpty(track.Id)
                    || string.IsNullOrEmpty(track.Name)
                )
                {
                    lock (progressLock)
                    {
                        skippedCount++;
                    }
                    continue; // Skip invalid track data
                }

                var trackInfo = HelperFunctions.SanitizeTrackData(track); // Prepare data for naming/formatting
                string fileName =
                    HelperFunctions.RenameUsingFormat(_config.FileName, trackInfo) + ".lrc";
                string filePath = Path.Combine(targetFolder, fileName);

                // Progress Update
                UpdateProgress(current, total, $"Processing: {track.Name}");

                if (File.Exists(filePath) && !_config.ForceDownload)
                {
                    lock (progressLock)
                    {
                        skippedCount++;
                    }
                    continue; // Skip if exists and not forcing
                }

                try
                {
                    var lyricsResponse = await _client.GetLyricsAsync(track.Id);

                    if (lyricsResponse?.Lyrics?.Lines != null)
                    {
                        string lrcContent = FormatLrc(lyricsResponse, trackInfo);
                        await SaveLyricsAsync(lrcContent, filePath);
                        lock (progressLock)
                        {
                            downloadedCount++;
                        }
                    }
                    else
                    {
                        unableToFindLyrics.Add($"{trackInfo.Artist} - {trackInfo.Name}");
                        lock (progressLock)
                        {
                            skippedCount++;
                        }
                    }
                }
                catch (LyricsNotFoundException) // Expected if no lyrics
                {
                    unableToFindLyrics.Add($"{trackInfo.Artist} - {trackInfo.Name}");
                    lock (progressLock)
                    {
                        skippedCount++;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine(
                        $"\nError downloading lyrics for '{track.Name}': {ex.Message}"
                    );
                    unableToFindLyrics.Add($"{trackInfo.Artist} - {trackInfo.Name} (Error)");
                    lock (progressLock)
                    {
                        skippedCount++;
                    }
                }
                await Task.Delay(20); // Small delay between lyric requests
            }

            // Final progress update
            ClearCurrentConsoleLine();
            System.Console.WriteLine(
                $"\nDownload complete. Downloaded: {downloadedCount}, Skipped/No Lyrics: {skippedCount + unableToFindLyrics.Count}."
            ); // Adjust counts based on implementation

            return unableToFindLyrics;
        }

        public string FormatLrc(LyricsResponse lyricsJson, TrackInfoPlaceholder trackData)
        {
            if (lyricsJson.Lyrics?.Lines == null)
                return string.Empty;

            var lrcLines = new List<string>();
            bool isSynced = lyricsJson.Lyrics.SyncType != "UNSYNCED" && _config.SyncedLyrics;

            // Attempt to parse duration for [length] tag
            string lengthTag = "";
            if (long.TryParse(trackData.TotalTracks, out long durationMs)) // Reusing TotalTracks field name from Python example seems wrong, should be track duration
            {
                // Corrected: Need track duration, not total album tracks. Let's assume trackData needs a DurationMs field.
                // Placeholder: Assuming TrackInfoPlaceholder has DurationMs (long)
                // long durationMs = trackData.DurationMs; // Get actual duration
                // For now, we'll skip the length tag if duration isn't readily available here.
                // TimeSpan duration = TimeSpan.FromMilliseconds(durationMs);
                // lengthTag = $"[length: {duration.Minutes:00}:{duration.Seconds:00}.{duration.Milliseconds / 10:00}]";
            }

            lrcLines.Add($"[ti:{trackData.Name ?? ""}]");
            lrcLines.Add($"[al:{trackData.AlbumName ?? ""}]");
            lrcLines.Add($"[ar:{trackData.Artist ?? ""}]");
            if (!string.IsNullOrEmpty(lengthTag))
                lrcLines.Add(lengthTag);
            // Add other tags if desired: [au:], [by:], [re:], [ve:]


            foreach (var line in lyricsJson.Lyrics.Lines)
            {
                if (
                    !isSynced
                    || string.IsNullOrEmpty(line.StartTimeMs)
                    || !long.TryParse(line.StartTimeMs, out long startTimeMs)
                )
                {
                    // Unsynced line
                    lrcLines.Add(line.Words ?? "");
                }
                else
                {
                    // Synced line
                    TimeSpan time = TimeSpan.FromMilliseconds(startTimeMs);
                    // Format: [mm:ss.xx] (LRC standard uses hundredths of a second)
                    string timestamp =
                        $"[{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds / 10:00}]";
                    lrcLines.Add($"{timestamp}{line.Words ?? ""}");
                }
            }

            return string.Join(Environment.NewLine, lrcLines);
        }

        public async Task SaveLyricsAsync(string lyrics, string path)
        {
            try
            {
                await File.WriteAllTextAsync(path, lyrics, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(
                    $"\nFailed to save lyrics file '{Path.GetFileName(path)}': {ex.Message}"
                );
            }
        }

        public string ExtractIdFromUrl(string urlOrId, string expectedType)
        {
            if (Uri.TryCreate(urlOrId, UriKind.Absolute, out Uri? uri))
            {
                // Example: https://open.spotify.com/album/abcdef12345?si=...
                // Example: spotify:album:abcdef12345
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    if (
                        uri.Host.Contains("spotify.com")
                        && uri.Segments.Length >= 3
                        && uri.Segments[uri.Segments.Length - 2].TrimEnd('/') == expectedType
                    )
                    {
                        return uri.Segments[uri.Segments.Length - 1].Split('?')[0].TrimEnd('/'); // Get last segment, remove query params
                    }
                }
                else if (uri.Scheme == "spotify")
                {
                    // spotify:track:id
                    var parts = uri.AbsolutePath.Split(':');
                    if (parts.Length == 2 && parts[0] == expectedType)
                    {
                        return parts[1];
                    }
                }
                throw new ArgumentException(
                    $"Invalid Spotify URL format for type '{expectedType}': {urlOrId}"
                );
            }
            else
            {
                // Assume it's just the ID if it's not a valid URI
                // Basic validation: check length or characters if needed
                if (!string.IsNullOrWhiteSpace(urlOrId)) // Add more robust ID validation if required
                {
                    return urlOrId;
                }
                throw new ArgumentException($"Invalid ID or URL provided: {urlOrId}");
            }
        }

        // --- Console Progress Helper ---
        private static readonly object ConsoleLock = new object();
        private static int lastProgressLength = 0;

        private static void UpdateProgress(int current, int total, string message)
        {
            lock (ConsoleLock)
            {
                int percent = (int)(((double)current / total) * 100);
                string progressBar =
                    $"[{new string('#', percent / 5)}{new string('-', 20 - percent / 5)}]"; // Simple 20 char bar
                string output =
                    $"\rProgress: {current}/{total} {progressBar} {percent}% - {message}";

                // Pad with spaces to overwrite previous line completely
                int currentLength = output.Length;
                if (currentLength < lastProgressLength)
                {
                    output += new string(' ', lastProgressLength - currentLength);
                }
                System.Console.Write(output);
                lastProgressLength = output.Length; // Store length without padding
            }
        }

        private static void ClearCurrentConsoleLine()
        {
            lock (ConsoleLock)
            {
                if (lastProgressLength > 0)
                {
                    System.Console.Write($"\r{new string(' ', lastProgressLength)}\r");
                    lastProgressLength = 0;
                }
            }
        }
    }
}

/*
Author : s*rp
Purpose Of File : Handles fetching, formatting, and saving lyrics.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Globalization;
using CSharpSpotiLyrics.Core.Api;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;
using CSharpSpotiLyrics.Core.Utils;

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

            var folderData = new Dictionary<string, string>
            {
                { "name", albumData.Name ?? "Unknown Album" },
                {
                    "artists",
                    string.Join(
                        ",",
                        albumData.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                    )
                },
                { "id", albumData.Id ?? "" },
                { "releasedate", albumData.ReleaseDate ?? "" }
            };
            string folderName = HelperFunctions.RenameUsingFormat(
                _config.AlbumFolderName,
                folderData
            );

            System.Console.WriteLine($"> Album: {albumData.Name}");
            System.Console.WriteLine($"> Artist(s): {folderData["artists"]}");
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

            var folderData = new Dictionary<string, string>
            {
                { "name", playData.Name ?? "Unknown Playlist" },
                { "owner", playData.Owner?.DisplayName ?? "Unknown Owner" },
                { "collaborative", playData.Collaborative ? "[C]" : "" },
                { "id", playData.Id ?? "" },
                { "description", playData.Description ?? "" }
            };
            string folderName = HelperFunctions.RenameUsingFormat(
                _config.PlayFolderName,
                folderData
            );
            int totalTracks = playData.Tracks?.Total ?? 0;

            System.Console.WriteLine($"> Playlist: {playData.Name} {folderData["collaborative"]}");
            System.Console.WriteLine($"> Owner: {folderData["owner"]}");
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
                        return unableToFindLyrics;
                    }
                    Directory.CreateDirectory(targetFolder);
                }
            }
            else
            {
                Directory.CreateDirectory(targetFolder);
            }

            System.Console.WriteLine($"Fetching details for {trackIds.Count} tracks...");
            List<SpotifyTrack?> fullTracksData = new();
            int CHUNK_SIZE = 50;

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
                        $"Error fetching track batch: {ex.Message}. Skipping {idChunk.Count()} tracks in this batch."
                    );
                }
                await Task.Delay(50);
            }

            System.Console.WriteLine(
                $"Downloading lyrics ({fullTracksData.Count(t => t != null)} tracks found)..."
            );
            int downloadedCount = 0;
            int skippedCount = 0;

            var validTracks = fullTracksData.Where(t => t != null).ToList();

            int total = validTracks.Count;
            int current = 0;
            object progressLock = new object();

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
                    continue;
                }

                var trackInfo = HelperFunctions.SanitizeTrackData(track);
                string fileName =
                    HelperFunctions.RenameUsingFormat(_config.FileName, trackInfo) + ".lrc";
                string filePath = Path.Combine(targetFolder, fileName);

                UpdateProgress(current, total, $"Processing: {track.Name}");

                if (File.Exists(filePath) && !_config.ForceDownload)
                {
                    lock (progressLock)
                    {
                        skippedCount++;
                    }
                    continue;
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
                catch (LyricsNotFoundException)
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
                await Task.Delay(20);
            }

            ClearCurrentConsoleLine();
            System.Console.WriteLine(
                $"\nDownload complete. Downloaded: {downloadedCount}, Skipped/No Lyrics: {skippedCount + unableToFindLyrics.Count}."
            );

            return unableToFindLyrics;
        }

        public string FormatLrc(LyricsResponse lyricsJson, TrackInfoPlaceholder trackData)
        {
            if (lyricsJson.Lyrics?.Lines == null)
                return string.Empty;

            var lrcLines = new List<string>();
            bool isSynced = lyricsJson.Lyrics.SyncType != "UNSYNCED" && _config.SyncedLyrics;

            lrcLines.Add($"[ti:{trackData.Name ?? ""}]");
            lrcLines.Add($"[al:{trackData.AlbumName ?? ""}]");
            lrcLines.Add($"[ar:{trackData.Artist ?? ""}]");

            foreach (var line in lyricsJson.Lyrics.Lines)
            {
                if (
                    !isSynced
                    || string.IsNullOrEmpty(line.StartTimeMs)
                    || !long.TryParse(line.StartTimeMs, out long startTimeMs)
                )
                {
                    lrcLines.Add(line.Words ?? "");
                }
                else
                {
                    TimeSpan time = TimeSpan.FromMilliseconds(startTimeMs);
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
                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    if (
                        uri.Host.Contains("spotify.com")
                        && uri.Segments.Length >= 3
                        && uri.Segments[uri.Segments.Length - 2].TrimEnd('/') == expectedType
                    )
                    {
                        return uri.Segments[uri.Segments.Length - 1].Split('?')[0].TrimEnd('/');
                    }
                }
                else if (uri.Scheme == "spotify")
                {
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
                if (!string.IsNullOrWhiteSpace(urlOrId))
                {
                    return urlOrId;
                }
                throw new ArgumentException($"Invalid ID or URL provided: {urlOrId}");
            }
        }

        private static readonly object ConsoleLock = new object();
        private static int lastProgressLength = 0;

        private static void UpdateProgress(int current, int total, string message)
        {
            lock (ConsoleLock)
            {
                int percent = (int)(((double)current / total) * 100);
                string progressBar =
                    $"[{new string('#', percent / 5)}{new string('-', 20 - percent / 5)}]";
                string output =
                    $"\rProgress: {current}/{total} {progressBar} {percent}% - {message}";

                int currentLength = output.Length;
                if (currentLength < lastProgressLength)
                {
                    output += new string(' ', lastProgressLength - currentLength);
                }
                System.Console.Write(output);
                lastProgressLength = output.Length;
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
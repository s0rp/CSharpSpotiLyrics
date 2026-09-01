/*
Author : s*rp
Purpose Of File : Reads metadata from local audio files and fetches corresponding lyrics.
Date : 24.04.2025
Update: 23.01.2026, 29.08.2026, 02.09.2026
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSharpSpotiLyrics.Core.Api;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;
using CSharpSpotiLyrics.Core.Utils;

namespace CSharpSpotiLyrics.Console.App
{
    public class FileMetadataReader
    {
        private readonly SpotifyClient _client;
        private readonly Config _config;
        private readonly LyricsHandler _lyricsHandler;

        public FileMetadataReader(SpotifyClient client, Config config, LyricsHandler lyricsHandler)
        {
            _client = client;
            _config = config;
            _lyricsHandler = lyricsHandler;
        }

        public async Task<List<string>> FetchLyricsForLocalFilesAsync(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                System.Console.Error.WriteLine($"Directory not found: {directoryPath}");
                return new List<string>();
            }

            System.Console.WriteLine($"Scanning directory for audio files: {directoryPath}");
            var supportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp3",
                ".flac",
                ".wav",
                ".m4a",
                ".ogg",
                ".opus",
                ".aac"
            };

            var audioFiles = Directory
                .EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f)))
                .ToList();

            if (!audioFiles.Any())
            {
                System.Console.WriteLine("No supported audio files found in the directory.");
                return new List<string>();
            }

            System.Console.WriteLine(
                $"Found {audioFiles.Count} audio files. Searching Spotify and fetching lyrics..."
            );

            List<string> unableToFindLyrics = new List<string>();
            int processedCount = 0;
            int foundCount = 0;
            int skippedExistingCount = 0;

            foreach (var filePath in audioFiles)
            {
                processedCount++;
                string fileNameOnly = Path.GetFileName(filePath);
                string lrcFilePath = Path.ChangeExtension(filePath, ".lrc");

                UpdateProgress(processedCount, audioFiles.Count, $"Processing: {fileNameOnly}");

                if (File.Exists(lrcFilePath) && !_config.ForceDownload)
                {
                    skippedExistingCount++;
                    continue;
                }

                string? trackId = null;
                SpotifyTrack? foundTrack = null;
                string trackIdentifier = fileNameOnly;

                try
                {
                    // 1. Attempt to read embedded audio metadata tags
                    var tags = SimpleAudioTagReader.ReadTags(filePath);
                    string? title = tags.Title;
                    string? album = tags.Album;
                    string? firstArtist = tags.Artist;

                    // 2. Fallback: Parse Title and Artist from Filename if embedded tags are missing
                    if (string.IsNullOrWhiteSpace(title))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(filePath);

                        // Check for common "Artist - Title" format
                        int separatorIndex = nameWithoutExt.IndexOf(" - ", StringComparison.Ordinal);
                        if (separatorIndex > 0)
                        {
                            firstArtist = nameWithoutExt.Substring(0, separatorIndex).Trim();
                            title = nameWithoutExt.Substring(separatorIndex + 3).Trim();
                        }
                        else
                        {
                            title = nameWithoutExt.Trim();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        trackIdentifier = !string.IsNullOrWhiteSpace(firstArtist)
                            ? $"{firstArtist} - {title}"
                            : title;

                        var queryParts = new List<string>
                        {
                            $"track:\"{title.Replace("\"", "")}\""
                        };
                        if (!string.IsNullOrWhiteSpace(firstArtist))
                            queryParts.Add($"artist:\"{firstArtist.Replace("\"", "")}\"");
                        if (!string.IsNullOrWhiteSpace(album))
                            queryParts.Add($"album:\"{album.Replace("\"", "")}\"");

                        string searchQuery = string.Join(" ", queryParts);

                        var searchResult = await _client.SearchAsync(searchQuery, "track", 1);

                        if (
                            searchResult?.Tracks?.Items?.Count > 0
                            && searchResult.Tracks.Items[0] != null
                        )
                        {
                            foundTrack = searchResult.Tracks.Items[0];
                            trackId = foundTrack.Id;
                        }
                        else
                        {
                            // Secondary fallback: simplified query
                            searchQuery = $"track:\"{title.Replace("\"", "")}\"";
                            if (!string.IsNullOrWhiteSpace(firstArtist))
                                searchQuery += $" artist:\"{firstArtist.Replace("\"", "")}\"";

                            searchResult = await _client.SearchAsync(searchQuery, "track", 1);
                            if (
                                searchResult?.Tracks?.Items?.Count > 0
                                && searchResult.Tracks.Items[0] != null
                            )
                            {
                                foundTrack = searchResult.Tracks.Items[0];
                                trackId = foundTrack.Id;
                            }
                        }
                    }
                    else
                    {
                        System.Console.Error.WriteLine(
                            $"\nWarning: Could not parse track info from '{fileNameOnly}'. Skipping Spotify search."
                        );
                    }
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine(
                        $"\nError processing file or searching Spotify for '{fileNameOnly}': {ex.Message}"
                    );
                }

                if (trackId != null && foundTrack != null)
                {
                    try
                    {
                        var lyricsResponse = await _client.GetLyricsAsync(trackId);
                        if (lyricsResponse?.Lyrics?.Lines != null)
                        {
                            var trackInfo = HelperFunctions.SanitizeTrackData(foundTrack);
                            string lrcContent = _lyricsHandler.FormatLrc(lyricsResponse, trackInfo);
                            await _lyricsHandler.SaveLyricsAsync(lrcContent, lrcFilePath);
                            foundCount++;
                        }
                        else
                        {
                            unableToFindLyrics.Add(
                                trackIdentifier + " (Lyrics not found on Spotify)"
                            );
                        }
                    }
                    catch (LyricsNotFoundException)
                    {
                        unableToFindLyrics.Add(trackIdentifier + " (Lyrics not found on Spotify)");
                    }
                    catch (Exception ex)
                    {
                        System.Console.Error.WriteLine(
                            $"\nError fetching/saving lyrics for '{trackIdentifier}': {ex.Message}"
                        );
                        unableToFindLyrics.Add(trackIdentifier + " (Error fetching lyrics)");
                    }
                }
                else if (
                    !string.IsNullOrWhiteSpace(trackIdentifier)
                    && trackIdentifier != fileNameOnly
                )
                {
                    unableToFindLyrics.Add(
                        trackIdentifier + " (Could not find matching track on Spotify)"
                    );
                }

                await Task.Delay(50);
            }

            ClearCurrentConsoleLine();
            System.Console.WriteLine(
                $"\nLocal file scan complete. Found lyrics for: {foundCount} files. Skipped existing: {skippedExistingCount}."
            );

            return unableToFindLyrics;
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
                lastProgressLength = Math.Min(output.Length, System.Console.BufferWidth > 0 ? System.Console.BufferWidth - 1 : 80);
            }
        }

        private static void ClearCurrentConsoleLine()
        {
            lock (ConsoleLock)
            {
                if (lastProgressLength > 0)
                {
                    int clearLength = Math.Min(
                        lastProgressLength,
                        System.Console.BufferWidth > 0 ? System.Console.BufferWidth - 1 : 80
                    );
                    System.Console.Write($"\r{new string(' ', clearLength)}\r");
                    lastProgressLength = 0;
                }
            }
        }
    }
}
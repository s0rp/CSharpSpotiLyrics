/*
Author : s*rp
Purpose Of File : Reads metadata from local audio files and fetches corresponding lyrics.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Diagnostics;
using CSharpSpotiLyrics.Core.Api;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;
using CSharpSpotiLyrics.Core.Utils; // For HelperFunctions
using TagLib; // Requires TagLibSharp NuGet package

namespace CSharpSpotiLyrics.Console.App
{
    public class FileMetadataReader
    {
        private readonly SpotifyClient _client;
        private readonly Config _config;
        private readonly LyricsHandler _lyricsHandler; // To reuse formatting/saving

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
                return new List<string>(); // Return empty list indicating failure
            }

            System.Console.WriteLine($"Scanning directory for audio files: {directoryPath}");
            var supportedExtensions = new HashSet<string>
            {
                ".mp3",
                ".flac",
                ".m4a",
                ".ogg",
                ".opus",
                ".wav",
                ".aiff"
            }; // Add more if needed
            var audioFiles = Directory
                .EnumerateFiles(directoryPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (!audioFiles.Any())
            {
                System.Console.WriteLine("No supported audio files found in the directory.");
                return new List<string>();
            }

            System.Console.WriteLine(
                $"Found {audioFiles.Count} audio files. Searching Spotify and fetching lyrics..."
            );

            List<string> unableToFindLyrics = new();
            int processedCount = 0;
            int foundCount = 0;
            int skippedExistingCount = 0;

            foreach (var filePath in audioFiles)
            {
                processedCount++;
                string fileNameOnly = Path.GetFileName(filePath);
                string lrcFilePath = Path.ChangeExtension(filePath, ".lrc");

                // Progress Update
                UpdateProgress(processedCount, audioFiles.Count, $"Processing: {fileNameOnly}");

                if (System.IO.File.Exists(lrcFilePath) && !_config.ForceDownload)
                {
                    skippedExistingCount++;
                    continue; // Skip if LRC exists and not forcing
                }

                string? trackId = null;
                SpotifyTrack? foundTrack = null;
                string trackIdentifier = fileNameOnly; // Default identifier if tags fail

                try
                {
                    // Use TagLibSharp to read metadata
                    using (var tagFile = TagLib.File.Create(filePath))
                    {
                        string? title = tagFile.Tag.Title;
                        string? album = tagFile.Tag.Album;
                        string? firstArtist =
                            tagFile.Tag.FirstPerformer ?? tagFile.Tag.FirstAlbumArtist; // Prioritize performer

                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            trackIdentifier = $"{firstArtist ?? "Unknown Artist"} - {title}"; // Better identifier for messages

                            // Construct search query (be specific)
                            // Query format recommended by Spotify: track:name artist:name album:name year:YYYY etc.
                            var queryParts = new List<string>();
                            queryParts.Add($"track:\"{title.Replace("\"", "")}\""); // Quote and remove internal quotes
                            if (!string.IsNullOrWhiteSpace(firstArtist))
                                queryParts.Add($"artist:\"{firstArtist.Replace("\"", "")}\"");
                            if (!string.IsNullOrWhiteSpace(album))
                                queryParts.Add($"album:\"{album.Replace("\"", "")}\"");
                            // Maybe add year: tagFile.Tag.Year ?

                            string searchQuery = string.Join(" ", queryParts);

                            // Search on Spotify
                            var searchResult = await _client.SearchAsync(searchQuery, "track", 1); // Search for top 1 track

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
                                // Fallback: Try searching just by title and artist if specific search failed
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
                                $"\nWarning: Could not read Title tag for '{fileNameOnly}'. Skipping Spotify search."
                            );
                        }
                    } // using tagFile ensures disposal
                }
                catch (CorruptFileException cfe)
                {
                    System.Console.Error.WriteLine(
                        $"\nWarning: Could not read tags from '{fileNameOnly}' (corrupt file?): {cfe.Message}"
                    );
                }
                catch (UnsupportedFormatException ufe)
                {
                    System.Console.Error.WriteLine(
                        $"\nWarning: Unsupported format or could not read tags from '{fileNameOnly}': {ufe.Message}"
                    );
                }
                catch (Exception ex) // Catch other potential TagLib or Spotify search errors
                {
                    System.Console.Error.WriteLine(
                        $"\nError processing tags or searching Spotify for '{fileNameOnly}': {ex.Message}"
                    );
                }

                // If we found a track ID, try to get lyrics
                if (trackId != null && foundTrack != null)
                {
                    try
                    {
                        var lyricsResponse = await _client.GetLyricsAsync(trackId);
                        if (lyricsResponse?.Lyrics?.Lines != null)
                        {
                            var trackInfo = HelperFunctions.SanitizeTrackData(foundTrack); // Use found track data
                            string lrcContent = _lyricsHandler.FormatLrc(lyricsResponse, trackInfo); // Reuse formatter
                            await _lyricsHandler.SaveLyricsAsync(lrcContent, lrcFilePath); // Reuse saver
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
                ) // Only add to unable list if we had tags but no match/ID
                {
                    unableToFindLyrics.Add(
                        trackIdentifier + " (Could not find matching track on Spotify)"
                    );
                }
                else if (
                    string.IsNullOrWhiteSpace(trackIdentifier)
                    || trackIdentifier == fileNameOnly
                )
                {
                    // File was likely skipped due to missing tags or tag read error, already warned.
                    // Don't add generic filename to "unable" list unless needed.
                }

                await Task.Delay(50); // Small delay between files to avoid hammering Spotify search
            }

            ClearCurrentConsoleLine(); // Clear the progress line
            System.Console.WriteLine(
                $"\nLocal file scan complete. Found lyrics for: {foundCount} files. Skipped existing: {skippedExistingCount}."
            );

            return unableToFindLyrics;
        }

        // --- Console Progress Helper (Copied from LyricsHandler for self-containment or move to a shared Util) ---
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

                int currentLength = output.Length;
                if (currentLength < lastProgressLength)
                {
                    output += new string(' ', lastProgressLength - currentLength);
                }
                System.Console.Write(output);
                lastProgressLength = Math.Min(output.Length, System.Console.BufferWidth - 1); // Prevent overflow
            }
        }

        private static void ClearCurrentConsoleLine()
        {
            lock (ConsoleLock)
            {
                if (lastProgressLength > 0)
                {
                    // Ensure we don't try to write outside buffer width
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

/*
Author : s*rp
Purpose Of File : Utility functions for string manipulation and file system operations.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Text.RegularExpressions;
using CSharpSpotiLyrics.Core.Models; // Assuming TrackInfo model exists here or nearby

namespace CSharpSpotiLyrics.Core.Utils
{
    // Temporary placeholder model - define this properly based on required fields
    public class TrackInfoPlaceholder
    {
        public string? Name { get; set; }
        public string? Artist { get; set; }
        public string? AlbumName { get; set; }
        public string? AlbumArtist { get; set; }
        public string? TrackNumber { get; set; }
        public string? TotalTracks { get; set; }
        public string? ReleaseDate { get; set; }
        public string? Explicit { get; set; } // e.g., "[E]" or ""
        public string? Owner { get; set; } // For playlists
        public string? Collaborative { get; set; } // For playlists "[C]" or ""
    }

    public static class HelperFunctions
    {
        private static readonly Regex InvalidFileCharsRegex =
            new(
                $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]",
                RegexOptions.Compiled
            );
        private static readonly Regex FormatRegex = new(@"\{(.+?)\}", RegexOptions.Compiled);

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "_"; // Default for empty/null
            // Replace invalid chars with underscore, trim result
            return InvalidFileCharsRegex.Replace(fileName, "_").Trim();
        }

        public static string RenameUsingFormat(string formatString, TrackInfoPlaceholder data)
        {
            // Use Regex.Replace with a MatchEvaluator for robust replacement
            string result = FormatRegex.Replace(
                formatString,
                match =>
                {
                    string key = match.Groups[1].Value;
                    object? value = data.GetType()
                        .GetProperty(
                            key,
                            System.Reflection.BindingFlags.IgnoreCase
                                | System.Reflection.BindingFlags.Public
                                | System.Reflection.BindingFlags.Instance
                        )
                        ?.GetValue(data);
                    // Handle different property types gracefully
                    return value switch
                    {
                        null => "", // Replace with empty string if property not found or null
                        string s => s,
                        // Add other type conversions if needed (e.g., numbers to strings)
                        _ => value.ToString() ?? "",
                    };
                }
            );

            // Sanitize the final result for file system compatibility
            return SanitizeFileName(result);
        }

        // Overload for Album/Playlist data (using Dictionary or a specific model)
        public static string RenameUsingFormat(string formatString, Dictionary<string, object> data)
        {
            string result = FormatRegex.Replace(
                formatString,
                match =>
                {
                    string key = match.Groups[1].Value;
                    if (data.TryGetValue(key, out object? value) && value != null)
                    {
                        return value.ToString() ?? "";
                    }
                    return ""; // Key not found or value is null
                }
            );
            return SanitizeFileName(result);
        }

        // Helper to prepare placeholder data from SpotifyTrack
        public static TrackInfoPlaceholder SanitizeTrackData(SpotifyTrack track)
        {
            return new TrackInfoPlaceholder
            {
                Name = track.Name,
                Artist = string.Join(
                    ',',
                    track.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                ),
                AlbumName = track.Album?.Name,
                AlbumArtist = string.Join(
                    ',',
                    track.Album?.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                ),
                TrackNumber = track.TrackNumber.ToString("D2"), // Pad with zero if needed
                TotalTracks = track.Album?.TotalTracks.ToString("D2"), // Pad with zero if needed
                ReleaseDate = track.Album?.ReleaseDate,
                Explicit = track.Explicit ? "[E]" : ""
            };
        }

        // Helper to chunk lists
        public static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int chunkSize)
        {
            using var enumerator = source.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var chunk = new List<T>(chunkSize) { enumerator.Current };
                for (int i = 1; i < chunkSize && enumerator.MoveNext(); i++)
                {
                    chunk.Add(enumerator.Current);
                }
                yield return chunk;
            }
        }
    }
}

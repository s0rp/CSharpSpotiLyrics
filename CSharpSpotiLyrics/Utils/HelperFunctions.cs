/*
Author : s*rp
Purpose Of File : Utility functions for string manipulation and file system operations.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CSharpSpotiLyrics.Core.Models;

namespace CSharpSpotiLyrics.Core.Utils
{
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
        private static readonly Regex InvalidFileCharsRegex = new Regex(
            $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))}]",
            RegexOptions.Compiled
        );
        private static readonly Regex FormatRegex = new Regex(@"\{(.+?)\}", RegexOptions.Compiled);

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "_";
            return InvalidFileCharsRegex.Replace(fileName, "_").Trim();
        }

        public static string RenameUsingFormat(string formatString, TrackInfoPlaceholder data)
        {
            string result = FormatRegex.Replace(
                formatString,
                match =>
                {
                    string key = match.Groups[1].Value;
                    string? value = key.ToLowerInvariant() switch
                    {
                        "name" => data.Name,
                        "artist" => data.Artist,
                        "albumname" => data.AlbumName,
                        "albumartist" => data.AlbumArtist,
                        "tracknumber" => data.TrackNumber,
                        "totaltracks" => data.TotalTracks,
                        "releasedate" => data.ReleaseDate,
                        "explicit" => data.Explicit,
                        "owner" => data.Owner,
                        "collaborative" => data.Collaborative,
                        _ => ""
                    };
                    return value ?? "";
                }
            );

            return SanitizeFileName(result);
        }

        public static string RenameUsingFormat(string formatString, Dictionary<string, string> data)
        {
            string result = FormatRegex.Replace(
                formatString,
                match =>
                {
                    string key = match.Groups[1].Value;
                    if (data.TryGetValue(key, out string? value) && value != null)
                    {
                        return value;
                    }
                    return "";
                }
            );
            return SanitizeFileName(result);
        }


        public static TrackInfoPlaceholder SanitizeTrackData(SpotifyTrack track)
        {
            return new TrackInfoPlaceholder
            {
                Name = track.Name,
                Artist = string.Join(
                    ",",
                    track.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                ),
                AlbumName = track.Album?.Name,
                AlbumArtist = string.Join(
                    ",",
                    track.Album?.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                ),
                TrackNumber = track.TrackNumber.ToString("D2"),
                TotalTracks = track.Album?.TotalTracks.ToString("D2"),
                ReleaseDate = track.Album?.ReleaseDate,
                Explicit = track.Explicit ? "[E]" : "",
            };
        }

        public static IEnumerable<IEnumerable<T>> Chunk<T>(IEnumerable<T> source, int chunkSize)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (chunkSize <= 0) throw new ArgumentOutOfRangeException(nameof(chunkSize));

#if NET6_0_OR_GREATER
            return System.Linq.Enumerable.Chunk(source, chunkSize);
#else
            return ChunkIterator(source, chunkSize);
#endif
        }

#if !NET6_0_OR_GREATER
        private static IEnumerable<List<T>> ChunkIterator<T>(IEnumerable<T> source, int chunkSize)
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
#endif
    }
}
/*
Author : s*rp
Purpose Of File : Fetching Hash Table & Totp secrets.
Date : 24.04.2025
Update: 29.08.2026 & 01.09.2026
Supervisor : Dixiz 3A Neural (Coder MoE)
- MAJOR UPDT FROM 04.08.2026:
    - Removed playwright usage (Package removed).
    - Added fetching totp & hashes from http client and regex fully.
    - Fixed totp version and secrets mismatching from spotify (due to regex mostly).
- REFACTORED 29.08.2026:
    - optimizations & sec updts. (371580a83eead4c1061b0ffbbda9cb47a07bb487)
        - Throttled concurrent HTTP requests in `FetchSecretAndHashesAsync` using `SemaphoreSlim`.
        - Added a `ConcurrentDictionary`-based reflection property cache in `RenameUsingFormat`.
        - Added regex timeouts to reduce the risk of ReDoS attacks.
        - Added conditional compilation to use the built-in `Enumerable.Chunk` on modern target frameworks.
    - Migrated Console.WriteLine to Microsoft.Extensions.Logging.ILogger.
    - Added Regex timeouts to mitigate ReDoS vulnerabilities.
    - Integrated SemaphoreSlim to throttle concurrent JS downloads.
- UPDT FROM 01.09.2026:
    - Native Aot compatible (net8.0) 

 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using CSharpSpotiLyrics.Core.Models;

namespace CSharpSpotiLyrics.Core.Api
{
    public static class SpotifyTotp
    {
        private const int Period = 30;
        private const int Digits = 6;
        private static readonly string TotpFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".SPOTIFYTOTP");
        private static readonly string HashPath = Path.Combine(Directory.GetCurrentDirectory(), ".SPOTIFYHASH");
        private static readonly HttpClient _httpClient = new HttpClient();

        public class TotpReturn
        {
            public string Totp { get; set; } = string.Empty;
            public int Version { get; set; }
            public bool isCached { get; set; }
        }

        public class SecretVersionJSON
        {
            public string? Secret { get; set; }
            public int Version { get; set; }
        }

        public static TotpReturn GenerateTotp(long serverTimeMilliseconds, bool force = false, ILogger? logger = null)
        {
            var (secret, version, cached) = GetSecretAndVersion(force, logger).GetAwaiter().GetResult();

            long counter = serverTimeMilliseconds / 1000 / Period;
            byte[] counterBytes = BitConverter.GetBytes(counter);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            using (HMACSHA1 hmac = new HMACSHA1(secret))
            {
                byte[] hmacResult = hmac.ComputeHash(counterBytes);

                int offset = hmacResult[hmacResult.Length - 1] & 0x0F;
                int binaryCode =
                    ((hmacResult[offset] & 0x7F) << 24)
                    | ((hmacResult[offset + 1] & 0xFF) << 16)
                    | ((hmacResult[offset + 2] & 0xFF) << 8)
                    | (hmacResult[offset + 3] & 0xFF);

                int otp = binaryCode % (int)Math.Pow(10, Digits);
                return new TotpReturn
                {
                    Totp = otp.ToString($"D{Digits}"),
                    Version = version,
                    isCached = cached,
                };
            }
        }

        public static string Base64Encode(string content)
        {
            byte[] contentBytes = Encoding.UTF8.GetBytes(content);
            return Convert.ToBase64String(contentBytes);
        }

        public static string Base64Decode(string base64Content)
        {
            byte[] base64Bytes = Convert.FromBase64String(base64Content);
            return Encoding.UTF8.GetString(base64Bytes);
        }

        private static async Task<(byte[] secret, int version, bool cached)> GetSecretAndVersion(bool forceNew = false, ILogger? logger = null)
        {
            SecretVersionJSON? cache = null;
            try
            {
                string? cachedText = File.ReadAllText(TotpFilePath);
                if (!string.IsNullOrEmpty(cachedText) && !forceNew)
                {
                    cache = JsonHelper.Deserialize(cachedText, SpotiLyricsJsonContext.Default.SecretVersionJSON);
                }
            }
            catch { cache = null; }

            SecretVersionJSON? extractedData = null;

            if (cache == null)
            {
                extractedData = await FetchSecretAndHashesAsync(logger);
            }
            else
            {
                try
                {
                    extractedData = new SecretVersionJSON
                    {
                        Secret = Base64Decode(cache.Secret!),
                        Version = cache.Version,
                    };
                }
                catch
                {
                    extractedData = await FetchSecretAndHashesAsync(logger);
                }
            }

            if (extractedData == null || string.IsNullOrEmpty(extractedData.Secret))
            {
                throw new InvalidOperationException("Failed to extract TOTP secret from Spotify Web Player.");
            }

            string secretString = extractedData.Secret;
            int version = extractedData.Version;
            string secretKey;

            if (secretString.All(char.IsDigit))
            {
                secretKey = secretString;
            }
            else
            {
                byte[] asciiCodes = secretString.Select(c => (byte)c).ToArray();
                byte[] transformed = asciiCodes
                    .Select((val, i) => (byte)(val ^ ((i % 33) + 9)))
                    .ToArray();

                secretKey = string.Join(string.Empty, transformed.Select(b => b.ToString()));
            }

            return (Encoding.UTF8.GetBytes(secretKey), version, (cache != null));
        }

        private static async Task<SecretVersionJSON> FetchSecretAndHashesAsync(ILogger? logger = null)
        {
            string? bestSecret = null;
            int bestVersion = -1;
            var queryHashes = new Dictionary<string, string>();

            try
            {
                _httpClient.DefaultRequestHeaders.UserAgent.Clear();
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");

                string html = await _httpClient.GetStringAsync("https://open.spotify.com");

                var jsUrlRegex = new Regex(@"(https://[^/]+/cdn/build/web-player/[^""'\s>]+\.js|/cdn/build/web-player/[^""'\s>]+\.js)", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
                var jsUrls = jsUrlRegex.Matches(html)
                    .Cast<Match>()
                    .Select(m =>
                    {
                        string val = m.Value;
                        if (!val.StartsWith("http")) val = "https://open.spotifycdn.com" + val;
                        return val;
                    })
                    .Distinct()
                    .ToList();

                var secretRegex = new Regex(@"secret:\s*([""'])(?<secret>(?:(?!\1)[^\\]|\\.)*)\1\s*,\s*version:\s*(?<version>\d+)", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
                var hashRegex = new Regex(@"[""'](?<op>[a-zA-Z0-9_]+)[""']\s*,\s*[""'](?:query|mutation)[""']\s*,\s*[""'](?<hash>[a-f0-9]{64})[""']", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

                using var throttler = new SemaphoreSlim(3, 3);
                var tasks = jsUrls.Select(async url =>
                {
                    await throttler.WaitAsync();
                    try
                    {
                        string jsContent = await _httpClient.GetStringAsync(url);

                        var secretMatches = secretRegex.Matches(jsContent);
                        foreach (Match m in secretMatches)
                        {
                            try
                            {
                                int ver = int.Parse(m.Groups["version"].Value);
                                string rawSecret = Regex.Unescape(m.Groups["secret"].Value);

                                lock (queryHashes)
                                {
                                    if (ver > bestVersion)
                                    {
                                        bestVersion = ver;
                                        bestSecret = rawSecret;
                                    }
                                }
                            }
                            catch { }
                        }

                        var hashMatches = hashRegex.Matches(jsContent);
                        foreach (Match m in hashMatches)
                        {
                            lock (queryHashes)
                            {
                                queryHashes[m.Groups["op"].Value] = m.Groups["hash"].Value;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "Failed to process JS file {Url}", url);
                    }
                    finally
                    {
                        throttler.Release();
                    }
                });

                await Task.WhenAll(tasks);

                logger?.LogInformation("Total Hashes {HashCount} Found From open.spotify.com", queryHashes.Count);

                if (queryHashes.Count > 0)
                {
                    File.WriteAllText(HashPath, JsonHelper.Serialize(queryHashes, SpotiLyricsJsonContext.Default.DictionaryStringString));
                    logger?.LogInformation("Hashes Saved To '{HashPath}'", HashPath);
                }

                if (!string.IsNullOrEmpty(bestSecret))
                {
                    File.WriteAllText(TotpFilePath, JsonHelper.Serialize(new SecretVersionJSON
                    {
                        Secret = Base64Encode(bestSecret),
                        Version = bestVersion,
                    }, SpotiLyricsJsonContext.Default.SecretVersionJSON));
                    logger?.LogInformation("TOTP Secret Saved To '{TotpFilePath}'", TotpFilePath);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "FetchSecretAndHashesAsync execution failed");
            }

            return new SecretVersionJSON { Secret = bestSecret, Version = bestVersion };
        }
    }
}
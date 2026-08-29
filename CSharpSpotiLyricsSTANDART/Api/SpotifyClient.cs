/*
Author : s*rp
Purpose Of File : Client for interacting with Spotify internal and public APIs.
Date : 24.04.2025
Update: 23.01.2026, 28.07.2026 & 29.08.2026
Supervisor : Dixiz 3A Neural (Coder MoE)
- Revised 23.01.2026: 
    - Replaced all V1 REST calls to New GraphQL
- MINOR UPDT FROM 28.07.2026:
    - Added Most of Graphql hashes.
- MINOR UPDT FROM 04.08.2026:
    - Now we're fetchin' time from api.
- NOTE FROM 28.07.2026 : I have been running this code on my own site (https://sxrp.me) 24/7 for nearly 4-5 months on my Spot' Acc. I haven't encountered any significant issues, so feel free to use it!!!
- REFACTORED 29.08.2026:
    - optimizations & sec updts. (371580a83eead4c1061b0ffbbda9cb47a07bb487)
        - Throttled concurrent HTTP requests in `GetTracksAsync` and `FetchSecretAndHashesAsync` using `SemaphoreSlim`.
        - Added a `ConcurrentDictionary`-based reflection property cache in `RenameUsingFormat`.
        - Configured `LoginAsync` and `GetCurrentSongAsync` to parse JSON directly from response streams.
        - Added regex timeouts to reduce the risk of ReDoS attacks.
        - Replaced `File.Exists` checks in `LoginAsync` with `try-catch` blocks for atomic file reads.
        - Added conditional compilation to use the built-in `Enumerable.Chunk` on modern target frameworks.
    - Migrated Console.WriteLine to Microsoft.Extensions.Logging.ILogger.
    - Optimized HTTP requests and stream-based JSON parsing to reduce memory allocations.
    - Added thread-safe parallel processing for GetTracksAsync.

   ▄██▄                     ▄ █   █     █         ▄ ▄ ▄
 ▄██████▄     ▄ ▄       ▄   █ █ ▄ █ ▄ █ █ █   ▄   █ █ █
 ███▄▄███   ▄ █ █ ▄ ▄ ▄ █ ▄ █ █ █ █ █ █ █ █ ▄ █ ▄ █ █ █ ▄
 ███▀▀███   ▀ █ █ █ ▀ ▀ █ █ █ █ █ █ █ █ █ █ █ █ █ █ █ █ ▀
 ▀██████▀                   █ █   █ ▀ █ █ █   ▀   █ █ █
   ▀██▀                     █ █   █     █         █

(this scannable actually works tho, thanks to SpotifyAsciiScannables)



*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;
using Microsoft.Extensions.Logging;
using static CSharpSpotiLyrics.Core.Api.SpotifyTotp;

namespace CSharpSpotiLyrics.Core.Api
{
    #region GraphQL Base Models
    public class PersistedQuery
    {
        [JsonPropertyName("version")]
        public int Version { get; set; }

        [JsonPropertyName("sha256Hash")]
        public string Sha256Hash { get; set; }
    }

    public class GraphQLExtensions
    {
        [JsonPropertyName("persistedQuery")]
        public PersistedQuery PersistedQuery { get; set; }
    }

    public class GraphQLBody
    {
        [JsonPropertyName("operationName")]
        public string OperationName { get; set; }

        [JsonPropertyName("variables")]
        public object Variables { get; set; }

        [JsonPropertyName("extensions")]
        public GraphQLExtensions Extensions { get; set; }
    }

    public class GraphQLResponse<T>
    {
        [JsonPropertyName("data")]
        public T Data { get; set; }
    }
    #endregion

    #region GraphQL Data Models
    public class AlbumOfTrack { public CoverArt coverArt { get; set; } public string uri { get; set; } }
    public class Artists { public List<Item> items { get; set; } }
    public class ArtistUnion { public string __typename { get; set; } public Goods goods { get; set; } public object headerImage { get; set; } public string id { get; set; } public Profile profile { get; set; } public Stats stats { get; set; } public string uri { get; set; } public Visuals visuals { get; set; } }
    public class AssociationsV3 { public AudioAssociations audioAssociations { get; set; } }
    public class AudioAssociations { public List<Item> items { get; set; } }
    public class AvatarImage { public List<Source> sources { get; set; } }
    public class Biography { public string text { get; set; } public string type { get; set; } }
    public class Canvas { public string fileId { get; set; } public string type { get; set; } public string uri { get; set; } public string url { get; set; } }
    public class ColorDark { public string hex { get; set; } }
    public class Concerts { public List<Item> items { get; set; } public int? totalCount { get; set; } }
    public class ContentRating { public string label { get; set; } }
    public class Contributors { public List<Item> items { get; set; } }
    public class CoverArt { public ExtractedColors extractedColors { get; set; } public List<Source> sources { get; set; } }
    public class Credit { public string __typename { get; set; } public string artistName { get; set; } public string artistUri { get; set; } public bool? isArtistUriLinkable { get; set; } public string role { get; set; } }
    public class CreditsTrait { public Contributors contributors { get; set; } public object sources { get; set; } }
    public class Data { public ArtistUnion artistUnion { get; set; } public TrackUnion trackUnion { get; set; } public string __typename { get; set; } public Artists artists { get; set; } public bool? festival { get; set; } public Location location { get; set; } public string startDateIsoString { get; set; } public string title { get; set; } public string uri { get; set; } public string id { get; set; } public Profile profile { get; set; } public List<Source> sources { get; set; } public AlbumOfTrack albumOfTrack { get; set; } public AssociationsV3 associationsV3 { get; set; } public ContentRating contentRating { get; set; } public string name { get; set; } }
    public class ExternalLinks { public List<Item> items { get; set; } }
    public class ExtractedColors { public ColorDark colorDark { get; set; } }
    public class Gallery { public List<Item> items { get; set; } }
    public class Goods { public Concerts concerts { get; set; } }
    public class CanvasJSON { public Data data { get; set; } }
    public class Item { public Data data { get; set; } public TrackAudio trackAudio { get; set; } public string name { get; set; } public string url { get; set; } public string city { get; set; } public string country { get; set; } public int? numberOfListeners { get; set; } public string region { get; set; } public List<Source> sources { get; set; } public string role { get; set; } public RoleGroup roleGroup { get; set; } public string uri { get; set; } public string __typename { get; set; } public TrackOfVideo trackOfVideo { get; set; } public Profile profile { get; set; } }
    public class Location { public string city { get; set; } public string name { get; set; } }
    public class Merch { public List<object> items { get; set; } public int? totalCount { get; set; } }
    public class Profile { public string name { get; set; } public Biography biography { get; set; } public ExternalLinks externalLinks { get; set; } public bool? verified { get; set; } }
    public class RelatedVideos { public string __typename { get; set; } public List<Item> items { get; set; } public int? totalCount { get; set; } }
    public class RoleGroup { public string name { get; set; } }
    public class Root { public Data data { get; set; } }
    public class Source { public int? maxHeight { get; set; } public int? maxWidth { get; set; } public string url { get; set; } public int? height { get; set; } public int? width { get; set; } public List<Item> items { get; set; } }
    public class Stats { public int? followers { get; set; } public int? monthlyListeners { get; set; } public TopCities topCities { get; set; } public int? worldRank { get; set; } }
    public class TopCities { public List<Item> items { get; set; } }
    public class TrackAudio { public string _uri { get; set; } }
    public class TrackOfVideo { public string __typename { get; set; } public string _uri { get; set; } public Data data { get; set; } }
    public class TrackUnion { public string __typename { get; set; } public AssociationsV3 associationsV3 { get; set; } public Canvas canvas { get; set; } public List<Credit> credits { get; set; } public CreditsTrait creditsTrait { get; set; } public Merch merch { get; set; } public RelatedVideos relatedVideos { get; set; } }
    public class Visuals { public AvatarImage avatarImage { get; set; } public Gallery gallery { get; set; } }

    public class CustomArtistDetails
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
    }
    #endregion

    public class SpotifyClient : IDisposable
    {
        private const string BrowserUserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/144.0.0.0 Safari/537.36";
        private const string ClientTokenUrl = "https://clienttoken.spotify.com/v1/clienttoken";
        private const string WebPlayerClientVersion = "1.2.95.439.ga887f843";
        private static readonly string TotpFilePath = Path.Combine(Directory.GetCurrentDirectory(), ".SPOTIFYTOTP");
        private static readonly string HashPath = Path.Combine(Directory.GetCurrentDirectory(), ".SPOTIFYHASH");

        public Dictionary<string, string> OperationToHashTable = new Dictionary<string, string>
        {
            { "profileAttributes", "53bcb064f6cd18c23f752bc324a791194d20df612d8e1239c735144ab0399ced" },
            { "fetchPlaylistMetadata","e4b2953f160e58e38ac025d79b5a9b3aceee5c4c716598e9830bfceb69faff5f" },
            { "libraryV3","390c78e5b951029bad359785e69b07b536a509c581cbcd0aded5e5067f187455" },
            { "getAlbum","b9bfabef66ed756e5e13f68a942deb60bd4125ec1f1be8cc42769dc0259b4b10" },
            { "queryAlbumMerch", "3ef44ed6f17be67299538fe77faffab4075aeaf9e1085f10fc835592266711b5" },
            { "areEntitiesInLibrary", "134337999233cc6fdd6b1e6dbf94841409f04a946c5c7b744b09ba0dfe5a85ed" },
            { "isCurated", "e4ed1f91a2cc5415befedb85acf8671dc1a4bf3ca1a5b945a6386101a22e28a6" },
            { "centralisedStatePlayerOptions", "e2dcfcab470854d4d1c7cb1a851438f14fe0a94d57db7f0b9dde492559d5395d" },
            { "decorateContextTracks", "383de00240775c39a6afe0b1055dc562b2a3930894201f9762f3fc32a74971c7" },
            { "fetchEntitiesForRecentlyPlayed", "cf5d2e94ffd82788470788ae1f6090cc3e9e774fb8fd383580634c6e6f50f7be" },
            { "queryNpvArtist", "047c9c225967d41a763949a4db3f0493e901c9f8689a6537408aabf9beffc177" }
        };

        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private readonly ILogger<SpotifyClient>? _logger;

        private string? _accessToken;
        private string? _clientToken;
        private string? _clientId;
        private bool _isLoggedIn = false;
        private bool _TotpCached = false;
        private DateTime _clientTokenExpiresAt = DateTime.MinValue;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public SpotifyClient(string spDcToken, ILogger<SpotifyClient>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(spDcToken))
            {
                throw new ArgumentNullException(nameof(spDcToken), "sp_dc token cannot be empty.");
            }

            _logger = logger;
            _cookieContainer = new CookieContainer();
            _cookieContainer.Add(new Uri("https://open.spotify.com"), new Cookie("sp_dc", spDcToken));

            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.ExpectContinue = false;

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", BrowserUserAgent);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en;q=0.8");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua", "\"Not(A:Brand\";v=\"8\", \"Chromium\";v=\"144\", \"Google Chrome\";v=\"144\"");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-mobile", "?0");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-ch-ua-platform", "\"Windows\"");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-dest", "empty");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-mode", "cors");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-fetch-site", "same-site");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("sec-gpc", "1");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://open.spotify.com/");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Origin", "https://open.spotify.com");

            _httpClient.BaseAddress = new Uri("https://api.spotify.com/v1/");
        }

        public void RemoveCaches()
        {
            try { if (File.Exists(TotpFilePath)) File.Delete(TotpFilePath); } catch { }
            try { if (File.Exists(HashPath)) File.Delete(HashPath); } catch { }
        }

        private void UpdateHeaders()
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
            _httpClient.DefaultRequestHeaders.Remove("client-token");
            _httpClient.DefaultRequestHeaders.Remove("client-id");
            _httpClient.DefaultRequestHeaders.Remove("spotify-app-version");
            _httpClient.DefaultRequestHeaders.Remove("app-platform");

            if (!string.IsNullOrEmpty(_accessToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            }

            if (!string.IsNullOrEmpty(_clientToken))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("client-token", _clientToken);
            }

            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("spotify-app-version", WebPlayerClientVersion);
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("app-platform", "WebPlayer");

            if (!string.IsNullOrEmpty(_clientId))
            {
                _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("client-id", _clientId);
            }
        }

        private async Task EnsureLoggedInAsync(bool forceRelogin = false)
        {
            if (!_isLoggedIn || forceRelogin)
            {
                await LoginAsync(forceRelogin);
            }
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new NotValidSpDcException("Failed to obtain access token.");
            }
            UpdateHeaders();
        }

        public async Task LoginAsync(bool force = false)
        {
            _logger?.LogInformation("Attempting to log in using sp_dc token...");
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    long localTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    long serverTimeSeconds = localTimeSeconds;

                    try
                    {
                        using var requestSTS = new HttpRequestMessage(HttpMethod.Get, "https://open.spotify.com/api/server-time");
                        using var responseSTS = await _httpClient.SendAsync(requestSTS, HttpCompletionOption.ResponseHeadersRead);
                        responseSTS.EnsureSuccessStatusCode();

                        using var stream = await responseSTS.Content.ReadAsStreamAsync();
                        using (JsonDocument document = await JsonDocument.ParseAsync(stream))
                        {
                            if (document.RootElement.TryGetProperty("serverTime", out JsonElement serverTimeElement) &&
                                serverTimeElement.ValueKind == JsonValueKind.Number)
                            {
                                serverTimeSeconds = serverTimeElement.GetInt64();
                            }
                        }
                    }
                    catch
                    {
                        serverTimeSeconds = localTimeSeconds;
                    }

                    long localTimeMilliseconds = localTimeSeconds * 1000;
                    long serverTimeMilliseconds = serverTimeSeconds * 1000;

                    TotpReturn totpLocal = SpotifyTotp.GenerateTotp(localTimeMilliseconds, force, _logger);
                    TotpReturn totpServer = SpotifyTotp.GenerateTotp(serverTimeMilliseconds, force, _logger);
                    _TotpCached = totpLocal.isCached;

                    string tokenUrl = $"https://open.spotify.com/api/token?reason=init&productType=web-player&totp={totpLocal.Totp}&totpServer={totpServer.Totp}&totpVer={totpLocal.Version}";

                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
                    using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

                    if (response.StatusCode == HttpStatusCode.Found)
                    {
                        throw new NotValidSpDcException($"Auth Failed (302 Redirect). sp_dc expired, missing user-agents or time desync!");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new NotValidSpDcException($"Failed to get access token. Status: {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");
                    }

                    using var tokenStream = await response.Content.ReadAsStreamAsync();
                    using (JsonDocument doc = await JsonDocument.ParseAsync(tokenStream))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("accessToken", out var tokenProp))
                            _accessToken = tokenProp.GetString();
                        if (root.TryGetProperty("clientId", out var clientIdProp))
                            _clientId = clientIdProp.GetString();
                    }

                    if (string.IsNullOrEmpty(_accessToken))
                    {
                        throw new NotValidSpDcException("Received null or empty access token from Spotify.");
                    }

                    if (!_accessToken.StartsWith("BQ"))
                    {
                        _logger?.LogWarning("Received potentially invalid token (attempt {Attempt})...", i + 1);
                        if (i < maxRetries - 1) continue;
                        else throw new NotValidSpDcException($"Failed to obtain a valid access token after {maxRetries} attempts.");
                    }

                    _isLoggedIn = true;
                    UpdateHeaders();

                    if (string.IsNullOrEmpty(_clientToken) || DateTime.UtcNow > _clientTokenExpiresAt)
                    {
                        await GetClientTokenAsync(force);
                        UpdateHeaders();
                    }

                    _logger?.LogInformation("Logged in successfully. ClientId: {ClientId}", _clientId);

                    Dictionary<string, string> tempHashTable = OperationToHashTable;
                    try
                    {
                        string? jsonContent = null;
                        try
                        {
                            jsonContent = File.ReadAllText(HashPath);
                        }
                        catch (FileNotFoundException) { /* Handled silently */ }
                        catch (DirectoryNotFoundException) { /* Handled silently */ }

                        if (!string.IsNullOrEmpty(jsonContent))
                        {
                            Dictionary<string, string>? loadedHashes = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent);
                            _logger?.LogInformation("{HashCount} Hash(es) Loaded From .SPOTIFYHASH", loadedHashes?.Count);

                            if (loadedHashes != null && loadedHashes.Count > 0)
                            {
                                foreach (var kvp in loadedHashes)
                                {
                                    OperationToHashTable[kvp.Key] = kvp.Value;
                                }
                                _logger?.LogInformation("Internal Hash Table Updated from cache.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to load hashes from cache, using default fallback hashes.");
                        OperationToHashTable = tempHashTable;
                    }

                    return;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Login attempt {Attempt} failed", i + 1);
                    if (i == maxRetries - 1)
                    {
                        _isLoggedIn = false;
                        _accessToken = null;
                        throw new NotValidSpDcException("sp_dc provided is invalid or connection failed after multiple attempts.", ex);
                    }
                    await Task.Delay(500);
                }
            }
        }

        private async Task GetClientTokenAsync(bool alreadyforced = false)
        {
            try
            {
                _logger?.LogInformation("Fetching Client Token...");

                var payloadObj = new
                {
                    client_data = new
                    {
                        client_version = WebPlayerClientVersion,
                        client_id = _clientId,
                        js_sdk_data = new
                        {
                            device_brand = "unknown",
                            device_model = "unknown",
                            os = "windows",
                            os_version = "NT 10.0",
                            device_id = GenerateRandomHex(32),
                            device_type = "computer"
                        }
                    }
                };

                string jsonPayload = JsonSerializer.Serialize(payloadObj);

                using var request = new HttpRequestMessage(HttpMethod.Post, ClientTokenUrl);
                var content = new StringContent(jsonPayload, Encoding.UTF8);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                request.Content = content;

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError("ClientToken Request Failed! Status: {StatusCode}", response.StatusCode);
                    if (_TotpCached && !alreadyforced)
                    {
                        _logger?.LogWarning("TOTP is cached. Forcing re-login to refresh TOTP and retry Client Token fetch.");
                        await LoginAsync(force: true);
                        return;
                    }
                    response.EnsureSuccessStatusCode();
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using (JsonDocument doc = await JsonDocument.ParseAsync(stream))
                {
                    if (doc.RootElement.TryGetProperty("granted_token", out var grantedToken) &&
                        grantedToken.TryGetProperty("token", out var tokenElem))
                    {
                        _clientToken = tokenElem.GetString();
                        int ttl = 1209600;
                        if (grantedToken.TryGetProperty("expires_after_seconds", out var expiresElem))
                        {
                            ttl = expiresElem.GetInt32();
                        }

                        _clientTokenExpiresAt = DateTime.UtcNow.AddSeconds(ttl);
                        _logger?.LogInformation("Client Token obtained.");
                    }
                    else
                    {
                        throw new Exception("JSON response did not contain 'granted_token.token'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to fetch Client Token. Continuing without it.");
            }
        }

        private static string GenerateRandomHex(int length)
        {
            var random = new Random();
            var buffer = new byte[length / 2];
            random.NextBytes(buffer);
            return BitConverter.ToString(buffer).Replace("-", "").ToLower();
        }

        private string GetHash(string operationName, string fallbackHash)
        {
            return OperationToHashTable.TryGetValue(operationName, out string hash) ? hash : fallbackHash;
        }

        private async Task<T> SendPathfinderRequest<T>(GraphQLBody body, string version = "v2")
        {
            await EnsureLoggedInAsync();
            string url = $"https://api-partner.spotify.com/pathfinder/{version}/query";

            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            string jsonPayload = JsonSerializer.Serialize(body, _jsonOptions);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            content.Headers.ContentType.CharSet = "UTF-8";
            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(_jsonOptions);
        }

        // ----------------------------------------------------
        // RAW GRAPHQL ENDPOINTS (Returns JsonElement)
        // ----------------------------------------------------

        public async Task<JsonElement> GetLibraryV3RawAsync(string folderUri, int limit = 50, int offset = 0)
        {
            _logger?.LogInformation("Executing GraphQL: libraryV3 for folder '{FolderUri}'", folderUri);
            var body = new GraphQLBody
            {
                OperationName = "libraryV3",
                Variables = new { filters = new string[] { }, order = (string)null, textFilter = "", features = new[] { "LIKED_SONGS", "YOUR_EPISODES_V2", "PRERELEASES", "PRERELEASES_V2", "CLIPS", "EVENTS" }, limit = limit, offset = offset, flatten = false, expandedFolders = new string[] { }, folderUri = folderUri, includeFoldersWhenFlattening = true },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("libraryV3", "390c78e5b951029bad359785e69b07b536a509c581cbcd0aded5e5067f187455") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> FetchPlaylistMetadataRawAsync(string playlistUri, int limit = 100, int offset = 0)
        {
            _logger?.LogInformation("Executing GraphQL: fetchPlaylistMetadata for '{PlaylistUri}'", playlistUri);
            var body = new GraphQLBody
            {
                OperationName = "fetchPlaylistMetadata",
                Variables = new { uri = playlistUri, offset = offset, limit = limit, enableWatchFeedEntrypoint = true },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("fetchPlaylistMetadata", "e4b2953f160e58e38ac025d79b5a9b3aceee5c4c716598e9830bfceb69faff5f") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> QueryAlbumMerchAsync(string albumUri, string deviceId)
        {
            _logger?.LogInformation("Executing GraphQL: queryAlbumMerch for '{AlbumUri}'", albumUri);
            var body = new GraphQLBody
            {
                OperationName = "queryAlbumMerch",
                Variables = new { uri = albumUri, deviceInfo = new { deviceId = deviceId, deviceType = "computer", clientId = _clientId, clientVersion = WebPlayerClientVersion, productId = "1" } },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("queryAlbumMerch", "3ef44ed6f17be67299538fe77faffab4075aeaf9e1085f10fc835592266711b5") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> AreEntitiesInLibraryAsync(List<string> uris)
        {
            _logger?.LogInformation("Executing GraphQL: areEntitiesInLibrary for {UriCount} entities.", uris.Count);
            var body = new GraphQLBody
            {
                OperationName = "areEntitiesInLibrary",
                Variables = new { uris = uris },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("areEntitiesInLibrary", "134337999233cc6fdd6b1e6dbf94841409f04a946c5c7b744b09ba0dfe5a85ed") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> IsCuratedAsync(List<string> uris)
        {
            _logger?.LogInformation("Executing GraphQL: isCurated for {UriCount} entities.", uris.Count);
            var body = new GraphQLBody
            {
                OperationName = "isCurated",
                Variables = new { uris = uris },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("isCurated", "e4ed1f91a2cc5415befedb85acf8671dc1a4bf3ca1a5b945a6386101a22e28a6") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> GetCentralisedStatePlayerOptionsAsync(string albumUri, string deviceId)
        {
            _logger?.LogInformation("Executing GraphQL: centralisedStatePlayerOptions for '{AlbumUri}'", albumUri);
            var body = new GraphQLBody
            {
                OperationName = "centralisedStatePlayerOptions",
                Variables = new { uri = albumUri, deviceInfo = new { deviceId = deviceId, deviceType = "computer", clientId = _clientId, clientVersion = WebPlayerClientVersion, productId = "1" } },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("centralisedStatePlayerOptions", "e2dcfcab470854d4d1c7cb1a851438f14fe0a94d57db7f0b9dde492559d5395d") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> DecorateContextTracksAsync(List<string> uris)
        {
            _logger?.LogInformation("Executing GraphQL: decorateContextTracks for {UriCount} entities.", uris.Count);
            var body = new GraphQLBody
            {
                OperationName = "decorateContextTracks",
                Variables = new { uris = uris },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("decorateContextTracks", "383de00240775c39a6afe0b1055dc562b2a3930894201f9762f3fc32a74971c7") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        public async Task<JsonElement> FetchEntitiesForRecentlyPlayedAsync(List<string> uris)
        {
            _logger?.LogInformation("Executing GraphQL: fetchEntitiesForRecentlyPlayed for {UriCount} entities.", uris.Count);
            var body = new GraphQLBody
            {
                OperationName = "fetchEntitiesForRecentlyPlayed",
                Variables = new { uris = uris },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("fetchEntitiesForRecentlyPlayed", "cf5d2e94ffd82788470788ae1f6090cc3e9e774fb8fd383580634c6e6f50f7be") } }
            };
            return await SendPathfinderRequest<JsonElement>(body);
        }

        // ----------------------------------------------------
        // TYPED GRAPHQL ENDPOINTS
        // ----------------------------------------------------

        public async Task<string?> GetCanvasUrlAsync(string artistIdOrUri, string trackIdOrUri)
        {
            await EnsureLoggedInAsync();
            if (!artistIdOrUri.StartsWith("spotify:artist:")) artistIdOrUri = "spotify:artist:" + artistIdOrUri;
            if (!trackIdOrUri.StartsWith("spotify:track:")) trackIdOrUri = "spotify:track:" + trackIdOrUri;

            _logger?.LogInformation("Fetching Canvas URL for track '{TrackUri}'", trackIdOrUri);

            var body = new GraphQLBody
            {
                OperationName = "queryNpvArtist",
                Variables = new { artistUri = artistIdOrUri, trackUri = trackIdOrUri, contributorsLimit = 10, contributorsOffset = 0, enableRelatedVideos = true, enableRelatedAudioTracks = false },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("queryNpvArtist", "047c9c225967d41a763949a4db3f0493e901c9f8689a6537408aabf9beffc177") } }
            };

            var result = await SendPathfinderRequest<GraphQLResponse<Data>>(body);
            if (result.Data?.trackUnion?.canvas != null && !string.IsNullOrEmpty(result.Data.trackUnion.canvas.url) && result.Data.trackUnion.canvas.url.EndsWith(".mp4"))
            {
                _logger?.LogInformation("Canvas URL found.");
                return result.Data?.trackUnion?.canvas.url;
            }

            _logger?.LogWarning("No Canvas found for this track.");
            return null;
        }

        public async Task<CustomArtistDetails?> GetArtistDetailsAsync(string artistUri)
        {
            await EnsureLoggedInAsync();
            if (string.IsNullOrEmpty(artistUri)) return null;
            string safeArtistId = artistUri.Replace("spotify:artist:", "");

            if (!artistUri.StartsWith("spotify:artist:")) artistUri = "spotify:artist:" + artistUri;

            _logger?.LogInformation("Fetching Artist Details for '{ArtistUri}'", artistUri);

            var body = new GraphQLBody
            {
                OperationName = "queryNpvArtist",
                Variables = new { artistUri = artistUri, trackUri = "", contributorsLimit = 1, contributorsOffset = 0, enableRelatedVideos = false, enableRelatedAudioTracks = false },
                Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("queryNpvArtist", "047c9c225967d41a763949a4db3f0493e901c9f8689a6537408aabf9beffc177") } }
            };

            try
            {
                var result = await SendPathfinderRequest<JsonElement>(body);

                var profile = result.GetProperty("data").GetProperty("artistUnion").GetProperty("profile");
                string name = profile.GetProperty("name").GetString();

                var visuals = result.GetProperty("data").GetProperty("artistUnion").GetProperty("visuals");
                string imageUrl = visuals.GetProperty("avatarImage").GetProperty("sources")[0].GetProperty("url").GetString();

                return new CustomArtistDetails { Id = safeArtistId, Name = name, ImageUrl = imageUrl };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to fetch artist details");
                return null;
            }
        }

        public async Task<SpotifyUser?> GetMeAsync()
        {
            await EnsureLoggedInAsync();
            _logger?.LogInformation("Fetching User Profile...");
            try
            {
                var body = new GraphQLBody
                {
                    OperationName = "profileAttributes",
                    Variables = new { },
                    Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("profileAttributes", "53bcb064f6cd18c23f752bc324a791194d20df612d8e1239c735144ab0399ced") } }
                };

                var result = await SendPathfinderRequest<GraphQLResponse<MeData>>(body);

                if (result?.Data?.Me == null) return null;

                return new SpotifyUser
                {
                    Id = result.Data.Me.Profile.Username,
                    DisplayName = result.Data.Me.Profile.Name,
                    Country = "N/A",
                    Product = "N/A"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "GetMeAsync failed");
                var cookieUser = _cookieContainer.GetCookies(new Uri("https://open.spotify.com"))["sp_user"]?.Value;
                return new SpotifyUser { Id = cookieUser ?? "Unknown", DisplayName = cookieUser ?? "User", Country = "XX" };
            }
        }

        public async Task<SpotifyPlaylist?> GetPlaylistAsync(string playlistId)
        {
            await EnsureLoggedInAsync();
            _logger?.LogInformation("Fetching Playlist '{PlaylistId}'", playlistId);
            try
            {
                var body = new GraphQLBody
                {
                    OperationName = "fetchPlaylistMetadata",
                    Variables = new { uri = $"spotify:playlist:{playlistId}", offset = 0, limit = 100, enableWatchFeedEntrypoint = true },
                    Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("fetchPlaylistMetadata", "e4b2953f160e58e38ac025d79b5a9b3aceee5c4c716598e9830bfceb69faff5f") } }
                };

                var result = await SendPathfinderRequest<GraphQLResponse<PlaylistData>>(body);
                var plData = result?.Data?.PlaylistV2;

                if (plData == null) return null;

                var playlist = new SpotifyPlaylist
                {
                    Name = plData.Name,
                    Description = plData.Description,
                    Uri = plData.Uri,
                    Owner = new SpotifyUser { DisplayName = plData.OwnerV2.Data.Name, Id = plData.OwnerV2.Data.Username },
                    Images = new List<ImageObject>(),
                    Tracks = new PagingObject<PlaylistItem> { Items = new List<PlaylistItem>(), Total = plData.Content.TotalCount }
                };

                if (plData.Images?.Items != null)
                {
                    foreach (var imgItem in plData.Images.Items)
                    {
                        if (imgItem.Sources != null)
                        {
                            foreach (var src in imgItem.Sources)
                            {
                                playlist.Images.Add(new ImageObject { Url = src.Url, Width = src.Width, Height = src.Height });
                            }
                        }
                    }
                }

                if (plData.Content?.Items != null)
                {
                    foreach (var item in plData.Content.Items)
                    {
                        var trackData = item.ItemV2.Data;
                        var track = new SpotifyTrack
                        {
                            Name = trackData.Name,
                            Uri = trackData.Uri,
                            DurationMs = trackData.TrackDuration.TotalMilliseconds,
                            Artists = new List<SimpleArtistObject>(),
                            Album = null
                        };

                        if (!string.IsNullOrEmpty(track.Uri) && track.Uri.Contains(":track:"))
                        {
                            track.Id = track.Uri.Split(':').Last();
                        }

                        if (trackData.Artists?.Items != null)
                        {
                            foreach (var art in trackData.Artists.Items)
                            {
                                track.Artists.Add(new SimpleArtistObject { Name = art.Profile.Name, Uri = art.Uri });
                            }
                        }

                        playlist.Tracks.Items.Add(new PlaylistItem { Track = track });
                    }
                }

                return playlist;
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get playlist {playlistId}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetPlaylistTracksAsync(string playlistId, int totalTracks)
        {
            var pl = await GetPlaylistAsync(playlistId);
            var ids = new List<string>();
            if (pl?.Tracks?.Items != null)
            {
                ids.AddRange(pl.Tracks.Items.Select(i => i.Track.Id).Where(id => !string.IsNullOrEmpty(id)));
            }
            return ids;
        }

        public async Task<PagingObject<SimplePlaylistObject>?> GetCurrentUserPlaylistsAsync(int limit = 50, int offset = 0)
        {
            await EnsureLoggedInAsync();
            _logger?.LogInformation("Fetching Current User Playlists...");
            try
            {
                var body = new GraphQLBody
                {
                    OperationName = "libraryV3",
                    Variables = new { order = (object)null, textFilter = "", features = new[] { "LIKED_SONGS", "YOUR_EPISODES_V2", "PRERELEASES", "EVENTS" }, limit = limit, offset = offset, flatten = false, expandedFolders = new object[] { }, folderUri = (object)null, includeFoldersWhenFlattening = true },
                    Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("libraryV3", "390c78e5b951029bad359785e69b07b536a509c581cbcd0aded5e5067f187455") } }
                };

                var result = await SendPathfinderRequest<GraphQLResponse<MeData>>(body);
                var libItems = result?.Data?.Me?.LibraryV3?.Items;

                if (libItems == null) return new PagingObject<SimplePlaylistObject> { Items = new List<SimplePlaylistObject>() };

                var playlists = new List<SimplePlaylistObject>();
                foreach (var item in libItems)
                {
                    if (item.Item?.Data != null && item.Item.Data.Uri.Contains(":playlist"))
                    {
                        var plData = item.Item.Data;
                        var simplePl = new SimplePlaylistObject
                        {
                            Name = plData.Name,
                            Uri = plData.Uri,
                            Description = plData.Description,
                            Images = new List<ImageObject>()
                        };

                        if (plData.Uri.Contains(":playlist:"))
                            simplePl.Id = plData.Uri.Split(':').Last();

                        if (plData.Images?.Items != null)
                        {
                            foreach (var imgItem in plData.Images.Items)
                            {
                                if (imgItem.Sources != null)
                                {
                                    foreach (var src in imgItem.Sources)
                                        simplePl.Images.Add(new ImageObject { Url = src.Url, Width = src.Width, Height = src.Height });
                                }
                            }
                        }
                        playlists.Add(simplePl);
                    }
                }

                return new PagingObject<SimplePlaylistObject> { Items = playlists, Total = playlists.Count };
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get user playlists: {ex.Message}", ex);
            }
        }

        public async Task<SpotifyAlbum?> GetAlbumAsync(string albumId)
        {
            await EnsureLoggedInAsync();
            _logger?.LogInformation("Fetching Album '{AlbumId}'", albumId);
            try
            {
                var body = new GraphQLBody
                {
                    OperationName = "getAlbum",
                    Variables = new { uri = $"spotify:album:{albumId}", locale = "", offset = 0, limit = 50 },
                    Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("getAlbum", "b9bfabef66ed756e5e13f68a942deb60bd4125ec1f1be8cc42769dc0259b4b10") } }
                };

                var result = await SendPathfinderRequest<GraphQLResponse<AlbumData>>(body);
                var albData = result?.Data?.AlbumUnion;

                if (albData == null) return null;

                var album = new SpotifyAlbum
                {
                    Id = albumId,
                    Name = albData.Name,
                    Uri = albData.Uri,
                    Type = "album",
                    AlbumType = "album",
                    Label = albData.Label,
                    ReleaseDate = albData.Date?.IsoString,
                    ReleaseDatePrecision = albData.Date?.Precision?.ToLower(),
                    TotalTracks = albData.TracksV2?.TotalCount ?? 0,
                    Images = new List<ImageObject>(),
                    Artists = new List<SimpleArtistObject>(),
                    Copyrights = new List<CopyrightObject>(),
                    ExternalUrls = new Dictionary<string, string>(),
                    Tracks = new PagingObject<SimpleTrackObject> { Items = new List<SimpleTrackObject>(), Total = albData.TracksV2?.TotalCount ?? 0 }
                };

                if (!string.IsNullOrEmpty(albData.SharingInfo?.ShareUrl))
                    album.ExternalUrls["spotify"] = albData.SharingInfo.ShareUrl;
                else if (!string.IsNullOrEmpty(albumId))
                    album.ExternalUrls["spotify"] = $"https://open.spotify.com/album/{albumId}";

                if (albData.Copyright?.Items != null)
                {
                    foreach (var c in albData.Copyright.Items)
                        album.Copyrights.Add(new CopyrightObject { Text = c.Text, Type = c.Type });
                }

                if (albData.CoverArt?.Sources != null)
                {
                    foreach (var src in albData.CoverArt.Sources)
                    {
                        if (!string.IsNullOrEmpty(src.Url))
                            album.Images.Add(new ImageObject { Url = src.Url, Width = src.Width, Height = src.Height });
                    }
                }

                if (albData.Artists?.Items != null)
                {
                    foreach (var art in albData.Artists.Items)
                    {
                        if (art.Profile == null) continue;

                        var artId = art.Uri?.Split(':').LastOrDefault();
                        var simpleArtist = new SimpleArtistObject { Name = art.Profile.Name, Uri = art.Uri, Id = artId, Type = "artist", ExternalUrls = new Dictionary<string, string>() };

                        if (!string.IsNullOrEmpty(artId))
                            simpleArtist.ExternalUrls["spotify"] = $"https://open.spotify.com/artist/{artId}";

                        album.Artists.Add(simpleArtist);
                    }
                }

                if (albData.TracksV2?.Items != null)
                {
                    foreach (var item in albData.TracksV2.Items)
                    {
                        var tData = item.Track;
                        if (tData == null) continue;

                        int durationMs = tData.Duration?.TotalMilliseconds ?? tData.TrackDuration?.TotalMilliseconds ?? 0;
                        var trackId = !string.IsNullOrEmpty(tData.Uri) ? tData.Uri.Split(':').LastOrDefault() : null;

                        var track = new SimpleTrackObject
                        {
                            Id = trackId,
                            Name = tData.Name,
                            Uri = tData.Uri,
                            TrackNumber = tData.TrackNumber,
                            DiscNumber = tData.DiscNumber,
                            DurationMs = durationMs,
                            Type = "track",
                            Explicit = (tData.ContentRating?.Label?.Equals("EXPLICIT", StringComparison.OrdinalIgnoreCase) == true),
                            Artists = new List<SimpleArtistObject>(),
                            ExternalUrls = new Dictionary<string, string>()
                        };

                        if (!string.IsNullOrEmpty(trackId))
                            track.ExternalUrls["spotify"] = $"https://open.spotify.com/track/{trackId}";

                        if (tData.Artists?.Items != null)
                        {
                            foreach (var art in tData.Artists.Items)
                            {
                                if (art.Profile == null) continue;

                                var tArtId = art.Uri?.Split(':').LastOrDefault();
                                var tArtist = new SimpleArtistObject { Name = art.Profile.Name, Uri = art.Uri, Id = tArtId, Type = "artist" };

                                if (!string.IsNullOrEmpty(tArtId))
                                    tArtist.ExternalUrls["spotify"] = $"https://open.spotify.com/artist/{tArtId}";

                                track.Artists.Add(tArtist);
                            }
                        }

                        album.Tracks.Items.Add(track);
                    }
                }

                return album;
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get album {albumId}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetAlbumTracksAsync(string albumId, int totalTracks)
        {
            var album = await GetAlbumAsync(albumId);
            var ids = new List<string>();
            if (album?.Tracks?.Items != null)
            {
                ids.AddRange(album.Tracks.Items.Select(t => t.Id).Where(id => !string.IsNullOrEmpty(id)));
            }
            return ids;
        }

        public async Task<SearchResult?> SearchAsync(string query, string type, int limit)
        {
            await EnsureLoggedInAsync();
            _logger?.LogWarning("Search via GraphQL not fully implemented (missing reliable hash). Returning empty.");
            return new SearchResult();
        }

        public async Task<PagingObject<SavedAlbumObject>?> GetCurrentUserSavedAlbumsAsync(int limit = 50, int offset = 0)
        {
            await EnsureLoggedInAsync();
            _logger?.LogInformation("Fetching Current User Saved Albums...");
            try
            {
                var body = new GraphQLBody
                {
                    OperationName = "libraryV3",
                    Variables = new { order = (object)null, textFilter = "", features = new[] { "ALBUMS" }, limit = limit, offset = offset, flatten = false, expandedFolders = new object[] { }, folderUri = (object)null, includeFoldersWhenFlattening = true },
                    Extensions = new GraphQLExtensions { PersistedQuery = new PersistedQuery { Version = 1, Sha256Hash = GetHash("libraryV3", "390c78e5b951029bad359785e69b07b536a509c581cbcd0aded5e5067f187455") } }
                };

                var result = await SendPathfinderRequest<GraphQLResponse<MeData>>(body);
                var items = result?.Data?.Me?.LibraryV3?.Items;

                var savedAlbums = new List<SavedAlbumObject>();

                if (items != null)
                {
                    foreach (var item in items)
                    {
                        if (item.Item?.Data != null && item.Item.Data.Uri.Contains(":album"))
                        {
                            var data = item.Item.Data;
                            var album = new SpotifyAlbum { Name = data.Name, Uri = data.Uri, Images = new List<ImageObject>() };

                            if (data.Images?.Items != null)
                            {
                                foreach (var imgItem in data.Images.Items)
                                {
                                    if (imgItem.Sources != null)
                                    {
                                        foreach (var src in imgItem.Sources)
                                            album.Images.Add(new ImageObject { Url = src.Url, Width = src.Width, Height = src.Height });
                                    }
                                }
                            }

                            if (data.Uri.Contains(":album:"))
                                album.Id = data.Uri.Split(':').Last();

                            savedAlbums.Add(new SavedAlbumObject { Album = album });
                        }
                    }
                }

                return new PagingObject<SavedAlbumObject> { Items = savedAlbums, Total = savedAlbums.Count };
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get saved albums: {ex.Message}", ex);
            }
        }

        // ----------------------------------------------------
        // REST ENDPOINTS
        // ----------------------------------------------------

        public async Task<TracksResponse?> GetTracksAsync(IEnumerable<string> trackIds)
        {
            if (trackIds == null || !trackIds.Any()) throw new ArgumentNullException(nameof(trackIds));
            await EnsureLoggedInAsync();

            _logger?.LogInformation("Fetching Tracks metadata for {TrackCount} items via REST...", trackIds.Count());
            var spotifyTracks = new ConcurrentBag<SpotifyTrack>();
            using var throttler = new SemaphoreSlim(10, 10);

            var tasks = trackIds.Select(async trackId =>
            {
                await throttler.WaitAsync();
                try
                {
                    string hexId = SpotifyIdConverter.Base62ToHex(trackId);
                    string url = $"https://spclient.wg.spotify.com/metadata/4/track/{hexId}?market=from_token";
                    using var request = new HttpRequestMessage(HttpMethod.Get, url);

                    using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogError("Failed to fetch metadata for {TrackId}. Status: {StatusCode}", trackId, response.StatusCode);
                        return;
                    }

                    var metadata = await response.Content.ReadFromJsonAsync<MetadataTrackResponse>(_jsonOptions);
                    if (metadata != null)
                    {
                        spotifyTracks.Add(MapMetadataToSpotifyTrack(metadata, trackId));
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error fetching track {TrackId}", trackId);
                }
                finally
                {
                    throttler.Release();
                }
            });

            await Task.WhenAll(tasks);

            return new TracksResponse { Tracks = spotifyTracks.ToList() };
        }

        private SpotifyTrack MapMetadataToSpotifyTrack(MetadataTrackResponse meta, string originalId)
        {
            var track = new SpotifyTrack
            {
                Id = originalId,
                Name = meta.Name,
                DurationMs = meta.Duration,
                Type = "track",
                Uri = meta.CanonicalUri ?? $"spotify:track:{originalId}",
                Artists = new List<SimpleArtistObject>(),
                Album = new SimpleAlbumObject { Name = meta.Album?.Name, Images = new List<ImageObject>() }
            };

            if (meta.Artist != null)
            {
                foreach (var art in meta.Artist)
                    track.Artists.Add(new SimpleArtistObject { Name = art.Name });
            }

            if (meta.Album?.CoverGroup?.Image != null)
            {
                foreach (var img in meta.Album.CoverGroup.Image)
                {
                    if (!string.IsNullOrEmpty(img.FileId))
                        track.Album.Images.Add(new ImageObject { Url = $"https://i.scdn.co/image/{img.FileId}", Width = img.Width, Height = img.Height });
                }
            }

            return track;
        }

        public async Task<CurrentlyPlayingContext?> GetCurrentSongAsync()
        {
            await EnsureLoggedInAsync();
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "me/player/currently-playing?market=from_token");
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.NotFound)
                    return null;

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                if (stream.Length == 0) return null;

                return await JsonSerializer.DeserializeAsync<CurrentlyPlayingContext>(stream, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new NoSongPlayingException($"Failed to get currently playing song: {ex.Message}", ex);
            }
        }

        public async Task<LyricsResponse?> GetLyricsAsync(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId)) throw new ArgumentNullException(nameof(trackId));
            await EnsureLoggedInAsync();

            _logger?.LogInformation("Fetching Lyrics for Track '{TrackId}'...", trackId);
            string lyricsUrl = $"https://spclient.wg.spotify.com/color-lyrics/v2/track/{trackId}?format=json&market=from_token";

            try
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, lyricsUrl);
                using var response = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger?.LogWarning("Lyrics not found.");
                    return null;
                }

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                return await JsonSerializer.DeserializeAsync<LyricsResponse>(stream, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error fetching lyrics for {TrackId}", trackId);
                throw new LyricsNotFoundException($"Failed to get lyrics for track {trackId}: {ex.Message}", ex);
            }
        }

        public async Task<byte[]?> DownloadFileAsync(string url)
        {
            try { return await _httpClient.GetByteArrayAsync(url); } catch { return null; }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public static class SpotifyIdConverter
    {
        private const string Base62Digits = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        public static string Base62ToHex(string base62Id)
        {
            BigInteger id = 0;
            foreach (char c in base62Id)
            {
                int p = Base62Digits.IndexOf(c);
                if (p < 0) throw new ArgumentException("Invalid Base62 character", nameof(base62Id));
                id = id * 62 + p;
            }

            string hex = id.ToString("x");
            if (hex.Length > 32 && hex.StartsWith("0"))
            {
                hex = hex.Substring(1);
            }
            return hex.PadLeft(32, '0');
        }
    }
}
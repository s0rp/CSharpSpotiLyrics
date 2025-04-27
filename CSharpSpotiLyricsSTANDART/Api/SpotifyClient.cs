/*
Author : s*rp
Purpose Of File : Client for interacting with Spotify internal and public APIs.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json; // Requires System.Net.Http.Json nuget package
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;

namespace CSharpSpotiLyrics.Core.Api
{
    public class SpotifyClient : IDisposable
    {
        private const string UserAgent =
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/101.0.4951.41 Safari/537.36";
        private readonly HttpClient _httpClient;
        private readonly CookieContainer _cookieContainer;
        private string? _accessToken;
        private bool _isLoggedIn = false;

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            // Add converters if needed for specific Spotify types (e.g., dates)
        };

        public SpotifyClient(string spDcToken)
        {
            if (string.IsNullOrWhiteSpace(spDcToken))
            {
                throw new ArgumentNullException(nameof(spDcToken), "sp_dc token cannot be empty.");
            }

            _cookieContainer = new CookieContainer();
            _cookieContainer.Add(
                new Uri("https://open.spotify.com"),
                new Cookie("sp_dc", spDcToken)
            );

            var handler = new HttpClientHandler
            {
                CookieContainer = _cookieContainer,
                UseCookies = true,
                AllowAutoRedirect = false // Mimic allow_redirects=False
            };

            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            _httpClient.DefaultRequestHeaders.Add("app-platform", "WebPlayer");
            _httpClient.BaseAddress = new Uri("https://api.spotify.com/v1/"); // Base for standard API calls
        }

        private async Task EnsureLoggedInAsync(bool forceRelogin = false)
        {
            if (!_isLoggedIn || forceRelogin)
            {
                await LoginAsync();
            }
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new NotValidSpDcException("Failed to obtain access token.");
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _accessToken
            );
        }

        public async Task LoginAsync()
        {
            const int maxRetries = 3;
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    long serverTimeSeconds = -1;
                    try
                    {
                        var responseSTS = await _httpClient.GetAsync(
                            "https://open.spotify.com/server-time"
                        );
                        responseSTS.EnsureSuccessStatusCode();

                        var jsonString = await responseSTS.Content.ReadAsStringAsync();
                        using (JsonDocument document = JsonDocument.Parse(jsonString))
                        {
                            if (
                                document.RootElement.TryGetProperty(
                                    "serverTime",
                                    out JsonElement serverTimeElement
                                )
                                && serverTimeElement.ValueKind == JsonValueKind.Number
                            )
                            {
                                // Get the server time value as a long (Int64)
                                serverTimeSeconds = serverTimeElement.GetInt64();
                            }
                            else
                            {
                                serverTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                                // Handle cases where the expected property is missing or not a number
                                throw new InvalidOperationException(
                                    "Failed to parse 'serverTime' from Spotify response."
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        serverTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    }
                    string totp = SpotifyTotp.GenerateTotp(serverTimeSeconds);
                    string tokenUrl =
                        $"https://open.spotify.com/get_access_token?reason=init&productType=web-player&totp={totp}&totpVer=5&ts={serverTimeSeconds}";

                    // Use a separate request message to control headers precisely for this specific call
                    using var requestMessage = new HttpRequestMessage(HttpMethod.Get, tokenUrl);
                    // No auth header for this specific request, relies on cookie

                    using var response = await _httpClient.SendAsync(requestMessage);

                    if (!response.IsSuccessStatusCode)
                    {
                        // Check specific status codes if necessary
                        throw new NotValidSpDcException(
                            $"Failed to get access token. Status: {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}"
                        );
                    }

                    var tokenResponse =
                        await response.Content.ReadFromJsonAsync<AccessTokenResponse>(_jsonOptions);
                    if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
                    {
                        throw new NotValidSpDcException(
                            "Received null or empty access token from Spotify."
                        );
                    }

                    // Check if token looks valid (starts with BQ)
                    if (!tokenResponse.AccessToken.StartsWith("BQ"))
                    {
                        // Log this attempt or delay before retrying?
                        Console.Error.WriteLine(
                            $"Warning: Received potentially invalid token (attempt {i + 1}): {tokenResponse.AccessToken.Substring(0, Math.Min(10, tokenResponse.AccessToken.Length))}..."
                        );
                        if (i < maxRetries - 1)
                            continue; // Retry
                        else
                            throw new NotValidSpDcException(
                                $"Failed to obtain a valid access token after {maxRetries} attempts."
                            );
                    }

                    _accessToken = tokenResponse.AccessToken;
                    _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                        "Bearer",
                        _accessToken
                    );
                    _isLoggedIn = true;
                    Console.WriteLine($"Successfully obtained access token (attempt {i + 1}).");
                    return; // Success
                }
                catch (Exception ex)
                    when (ex is HttpRequestException
                        || ex is JsonException
                        || ex is NotValidSpDcException
                    )
                {
                    Console.Error.WriteLine($"Login attempt {i + 1} failed: {ex.Message}");
                    if (i == maxRetries - 1) // Last attempt failed
                    {
                        _isLoggedIn = false;
                        _accessToken = null;
                        throw new NotValidSpDcException(
                            "sp_dc provided is invalid or connection failed after multiple attempts.",
                            ex
                        );
                    }
                    await Task.Delay(500); // Short delay before retry
                }
            }
            _isLoggedIn = false;
            _accessToken = null;
            throw new NotValidSpDcException($"Failed to login after {maxRetries} attempts."); // Should not be reached ideally
        }

        public async Task<SpotifyUser?> GetMeAsync()
        {
            await EnsureLoggedInAsync();
            try
            {
                var response = await _httpClient.GetAsync("me");

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<SpotifyUser>(_jsonOptions);
                }
                else if (
                    response.StatusCode == HttpStatusCode.Unauthorized
                    || response.StatusCode == HttpStatusCode.Forbidden
                )
                {
                    // Token might have expired, try relogin once
                    await EnsureLoggedInAsync(forceRelogin: true);
                    var retryResponse = await _httpClient.GetAsync("me");

                    if (!retryResponse.IsSuccessStatusCode)
                    {
                        throw new NotValidSpDcException(
                            $"Failed to get user info even after relogin. Status: {retryResponse.StatusCode}"
                        );
                    }
                    return await retryResponse.Content.ReadFromJsonAsync<SpotifyUser>(_jsonOptions);
                }
                else
                {
                    throw new HttpRequestException(
                        $"Request failed with status code {response.StatusCode}"
                    );
                }
            }
            catch (HttpRequestException httpEx)
            {
                throw new ApiException($"HTTP request failed: {httpEx.Message}", httpEx);
            }
            catch (System.Text.Json.JsonException jsonEx)
            {
                throw new ApiException(
                    $"Failed to parse user info JSON response: {jsonEx.Message}",
                    jsonEx
                );
            }
            catch (Exception ex) // Catch-all for other unexpected errors
            {
                // Avoid re-wrapping exceptions we already handled and threw specifically
                if (ex is ApiException || ex is NotValidSpDcException)
                    throw;

                throw new ApiException($"Failed to get current user info: {ex.Message}", ex);
            } //REVISED FOR NETSTANDART
        }

        public async Task<CurrentlyPlayingContext?> GetCurrentSongAsync()
        {
            await EnsureLoggedInAsync();
            try
            {
                // Endpoint: https://api.spotify.com/v1/me/player/currently-playing
                var response = await _httpClient.GetAsync(
                    "me/player/currently-playing?market=from_token"
                ); // Add market if needed

                // Handle specific non-success cases first
                if (response.StatusCode == HttpStatusCode.NoContent) // 204 No Content means nothing is playing
                {
                    return null;
                }
                if (response.StatusCode == HttpStatusCode.NotFound) // Sometimes happens if no active device
                {
                    return null;
                }

                if (
                    response.StatusCode == HttpStatusCode.Unauthorized
                    || response.StatusCode == HttpStatusCode.Forbidden
                )
                {
                    await EnsureLoggedInAsync(forceRelogin: true);
                    // Retry the request after re-login
                    response = await _httpClient.GetAsync(
                        "me/player/currently-playing?market=from_token"
                    );

                    // Check the retry response
                    if (
                        response.StatusCode == HttpStatusCode.NoContent
                        || response.StatusCode == HttpStatusCode.NotFound
                    )
                        return null;

                    try
                    {
                        response.EnsureSuccessStatusCode();
                    }
                    catch (HttpRequestException retryEx) // Catch exception from EnsureSuccessStatusCode on retry
                    {
                        throw new NoSongPlayingException(
                            $"Failed to get current song even after relogin (Retry Status: {response.StatusCode}).",
                            retryEx // Pass the exception from the failed EnsureSuccessStatusCode
                        );
                    }
                    // If retry succeeded, fall through to read content
                }
                else // Original response was not 204, 404, 401, 403
                {
                    // Now ensure other potential errors are caught
                    response.EnsureSuccessStatusCode(); // This will throw HttpRequestException for other non-success codes
                }

                // If we get here, the response (either original or retry) is successful
                var content = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content))
                    return null; // Check for empty response body

                return JsonSerializer.Deserialize<CurrentlyPlayingContext>(content, _jsonOptions);
            }
            catch (HttpRequestException ex)
            {
                throw new NoSongPlayingException(
                    $"HTTP request failed while getting currently playing song: {ex.Message}",
                    ex
                );
            }
            catch (Exception ex) // Catch other exceptions like JSON parsing errors, or the NoSongPlayingException thrown above
            {
                // Avoid wrapping NoSongPlayingException in another one
                if (ex is NoSongPlayingException)
                    throw;

                throw new NoSongPlayingException(
                    $"Failed to get currently playing song: {ex.Message}",
                    ex
                );
            } //REVISED FOR NETSTANDART
        }

        public async Task<LyricsResponse?> GetLyricsAsync(string trackId)
        {
            if (string.IsNullOrWhiteSpace(trackId))
                throw new ArgumentNullException(nameof(trackId));
            await EnsureLoggedInAsync();
            // This uses the internal endpoint, requires auth header set by LoginAsync
            string lyricsUrl =
                $"https://spclient.wg.spotify.com/color-lyrics/v2/track/{trackId}?format=json&market=from_token";

            HttpResponseMessage response = null; // Define outside try block to access in catch/finally if needed

            try
            {
                // We need to use SendAsync to ensure headers like Authorization are correctly included
                using var requestMessage = new HttpRequestMessage(HttpMethod.Get, lyricsUrl);
                // Auth header should be set globally on _httpClient after successful login

                response = await _httpClient.SendAsync(requestMessage);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null; // No lyrics available for this track
                }

                // Explicitly check for auth errors *before* EnsureSuccessStatusCode
                if (
                    response.StatusCode == HttpStatusCode.Unauthorized
                    || response.StatusCode == HttpStatusCode.Forbidden
                )
                {
                    // Try relogin once
                    await EnsureLoggedInAsync(forceRelogin: true);
                    using var retryRequestMessage = new HttpRequestMessage(
                        HttpMethod.Get,
                        lyricsUrl
                    );
                    // Dispose the old response before making a new request
                    response?.Dispose();
                    response = await _httpClient.SendAsync(retryRequestMessage);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        return null; // Still not found after relogin
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        var tempEx = new HttpRequestException(
                            $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase})."
                        );
                        throw new LyricsNotFoundException(
                            $"Failed to get lyrics for track {trackId} even after relogin. Status: {response.StatusCode}",
                            tempEx // Pass the synthesized exception as inner
                        );
                    }
                    // If successful after retry, execution will continue to ReadFromJsonAsync below
                }

                // If we get here, the status was not NotFound initially, and if it was Auth error,
                response.EnsureSuccessStatusCode(); // Throw HttpRequestException for other non-success codes

                var lyrics = await response.Content.ReadFromJsonAsync<LyricsResponse>(_jsonOptions);
                return lyrics;
            }
            catch (HttpRequestException ex)
            {
                string statusCodeInfo =
                    response != null ? $" (Status Code: {response.StatusCode})" : "";
                Console.Error.WriteLine(
                    $"HttpRequestException fetching lyrics for {trackId}{statusCodeInfo}: {ex}"
                );
                throw new LyricsNotFoundException(
                    $"Failed to get lyrics for track {trackId}: {ex.Message}",
                    ex
                );
            }
            catch (Exception ex)
            {
                // Log the error for diagnostics
                Console.Error.WriteLine($"Error fetching lyrics for {trackId}: {ex}");
                throw new LyricsNotFoundException(
                    $"Failed to get lyrics for track {trackId}: {ex.Message}",
                    ex
                );
            }
            finally
            {
                response?.Dispose();
            } //REVISED FOR NETSTANDART
        }

        // --- Standard Web API Wrappers (Partial implementation based on Python code) ---

        public async Task<SpotifyAlbum?> GetAlbumAsync(string albumId)
        {
            if (string.IsNullOrWhiteSpace(albumId))
                throw new ArgumentNullException(nameof(albumId));
            await EnsureLoggedInAsync();
            try
            {
                return await _httpClient.GetFromJsonAsync<SpotifyAlbum>(
                    $"albums/{albumId}",
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get album {albumId}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetAlbumTracksAsync(string albumId, int totalTracks)
        {
            if (string.IsNullOrWhiteSpace(albumId))
                throw new ArgumentNullException(nameof(albumId));
            await EnsureLoggedInAsync();
            var trackIds = new List<string>();
            int limit = 50; // Max limit for album tracks endpoint

            try
            {
                for (int offset = 0; offset < totalTracks; offset += limit)
                {
                    var response = await _httpClient.GetFromJsonAsync<
                        PagingObject<SimpleTrackObject>
                    >($"albums/{albumId}/tracks?limit={limit}&offset={offset}", _jsonOptions);
                    if (response?.Items != null)
                    {
                        trackIds.AddRange(
                            response.Items.Where(t => t?.Id != null).Select(t => t.Id!)
                        );
                    }
                    if (response?.Next == null)
                        break; // Stop if no more pages
                }
                return trackIds.Where(id => !string.IsNullOrEmpty(id)).ToList(); // Filter out potential nulls just in case
            }
            catch (Exception ex)
            {
                throw new ApiException(
                    $"Failed to get tracks for album {albumId}: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<SpotifyPlaylist?> GetPlaylistAsync(string playlistId)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentNullException(nameof(playlistId));
            await EnsureLoggedInAsync();
            try
            {
                // You might need to request specific fields if the default response is too large or missing data
                // string fields = "id,name,owner(display_name),tracks(total),collaborative,external_urls";
                // return await _httpClient.GetFromJsonAsync<SpotifyPlaylist>($"playlists/{playlistId}?fields={fields}", _jsonOptions);
                return await _httpClient.GetFromJsonAsync<SpotifyPlaylist>(
                    $"playlists/{playlistId}",
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get playlist {playlistId}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetPlaylistTracksAsync(string playlistId, int totalTracks)
        {
            if (string.IsNullOrWhiteSpace(playlistId))
                throw new ArgumentNullException(nameof(playlistId));
            await EnsureLoggedInAsync();
            var trackIds = new List<string>();
            int limit = 100; // Max limit for playlist tracks endpoint

            try
            {
                for (int offset = 0; offset < totalTracks; offset += limit)
                {
                    // Requesting only track IDs might be more efficient if that's all you need here
                    // string fields = "items(track(id)),next";
                    var response = await _httpClient.GetFromJsonAsync<PagingObject<PlaylistItem>>(
                        $"playlists/{playlistId}/tracks?limit={limit}&offset={offset}",
                        _jsonOptions
                    );
                    if (response?.Items != null)
                    {
                        trackIds.AddRange(
                            response
                                .Items.Where(item => item?.Track?.Id != null) // Ensure track and track.id are not null
                                .Select(item => item.Track.Id!)
                        ); // Select the non-null track ID
                    }
                    if (response?.Next == null)
                        break; // Stop if no more pages
                }
                return trackIds.Where(id => !string.IsNullOrEmpty(id)).ToList(); // Filter out potential nulls
            }
            catch (Exception ex)
            {
                throw new ApiException(
                    $"Failed to get tracks for playlist {playlistId}: {ex.Message}",
                    ex
                );
            }
        }

        public async Task<TracksResponse?> GetTracksAsync(IEnumerable<string> trackIds)
        {
            if (trackIds == null || !trackIds.Any())
                throw new ArgumentNullException(nameof(trackIds));
            await EnsureLoggedInAsync();
            // Spotify API limit is 50 IDs per request
            if (trackIds.Count() > 50)
                throw new ArgumentException(
                    "Cannot request more than 50 tracks at once.",
                    nameof(trackIds)
                );

            string idsParam = string.Join(",", trackIds);
            try
            {
                return await _httpClient.GetFromJsonAsync<TracksResponse>(
                    $"tracks?ids={idsParam}",
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get tracks: {ex.Message}", ex);
            }
        }

        public async Task<SearchResult?> SearchAsync(string query, string type, int limit)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentNullException(nameof(query));
            await EnsureLoggedInAsync();
            string encodedQuery = Uri.EscapeDataString(query);
            try
            {
                return await _httpClient.GetFromJsonAsync<SearchResult>(
                    $"search?q={encodedQuery}&type={type}&limit={limit}",
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                throw new ApiException($"Search failed: {ex.Message}", ex);
            }
        }

        // Methods for User Interaction (Playlists/Albums) - Fetch data only, selection happens in Console UI

        public async Task<PagingObject<SimplePlaylistObject>?> GetCurrentUserPlaylistsAsync(
            int limit = 50,
            int offset = 0
        )
        {
            await EnsureLoggedInAsync();
            try
            {
                return await _httpClient.GetFromJsonAsync<PagingObject<SimplePlaylistObject>>(
                    $"me/playlists?limit={limit}&offset={offset}",
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get user playlists: {ex.Message}", ex);
            }
        }

        public async Task<PagingObject<SavedAlbumObject>?> GetCurrentUserSavedAlbumsAsync(
            int limit = 50,
            int offset = 0
        )
        {
            await EnsureLoggedInAsync();
            try
            {
                return await _httpClient.GetFromJsonAsync<PagingObject<SavedAlbumObject>>(
                    $"me/albums?limit={limit}&offset={offset}",
                    _jsonOptions
                );
            }
            catch (Exception ex)
            {
                throw new ApiException($"Failed to get user saved albums: {ex.Message}", ex);
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}

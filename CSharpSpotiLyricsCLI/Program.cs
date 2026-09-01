/*
Author : s*rp
Purpose Of File : Main entry point for the CSharpSpotiLyrics console application.
Date : 24.04.2025
Update: 23.01.2026, 29.08.2026, 01.09.2026
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CSharpSpotiLyrics.Console.App;
using CSharpSpotiLyrics.Core.Api;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models;

public class Program
{
    private static Config? _config;
    private static SpotifyClient? _client;
    private static LyricsHandler? _lyricsHandler;
    private static FileMetadataReader? _fileMetadataReader;

    static async Task<int> Main(string[] args)
    {
        string? url = null;
        string? directoryOverride = null;
        bool forceOverride = false;
        string? configAction = null;
        string? userItem = null;
        bool forceCacheClear = false;

        // Custom Argument Parser (AOT Friendly)
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i].ToLowerInvariant();

            if (arg == "-h" || arg == "--help")
            {
                ShowHelp();
                return 0;
            }
            else if (arg == "-f" || arg == "--force")
            {
                forceOverride = true;
            }
            else if (arg == "-cl" || arg == "--clearcache")
            {
                forceCacheClear = true;
            }
            else if (arg == "-d" || arg == "--directory")
            {
                if (i + 1 < args.Length) directoryOverride = args[++i];
            }
            else if (arg == "-c" || arg == "--config")
            {
                if (i + 1 < args.Length) configAction = args[++i];
            }
            else if (arg == "-u" || arg == "--user")
            {
                if (i + 1 < args.Length) userItem = args[++i];
            }
            else if (!args[i].StartsWith("-") && url == null)
            {
                url = args[i];
            }
        }

        if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(userItem) && string.IsNullOrEmpty(configAction))
        {
            ShowHelp();
            return 0;
        }

        await RunApplicationLogic(url, directoryOverride, forceOverride, configAction, userItem, forceCacheClear);
        return 0;
    }

    private static void ShowHelp()
    {
        PrintLogo();
        Console.WriteLine("\nUsage:");
        Console.WriteLine("  CSharpSpotiLyrics [url] [options]");
        Console.WriteLine("\nArguments:");
        Console.WriteLine("  url                 URL/ID of Song, Album, or Playlist from Spotify, or path to a local directory containing audio files.");
        Console.WriteLine("\nOptions:");
        Console.WriteLine("  -d, --directory     Path to the download directory. Overrides config setting.");
        Console.WriteLine("  -f, --force         Force download, skip check if lyrics file already exists.");
        Console.WriteLine("  -cl, --clearcache   Clear the cache before running to reset cached data.");
        Console.WriteLine("  -c, --config        Manage the configuration file. (edit|reset|open)");
        Console.WriteLine("  -u, --user          Download items from the logged-in user's library. (current|album|play)");
        Console.WriteLine("  -h, --help          Show command line help.");
        Console.WriteLine();
    }

    private static async Task RunApplicationLogic(
        string? url,
        string? directoryOverride,
        bool forceOverride,
        string? configAction,
        string? userItem,
        bool ForceCacheClear
    )
    {
        if (!string.IsNullOrEmpty(configAction))
        {
            HandleConfigAction(configAction);
            return;
        }

        if (!await InitializeAsync(directoryOverride, forceOverride, ForceCacheClear))
        {
            return;
        }

        string? target = await DetermineTargetAsync(url, userItem);
        if (target == null)
        {
            return;
        }

        PrintLogo();
        await PrintUserInfoAsync();
        Console.WriteLine($"Current download path : \"{_config!.DownloadPath}\"");
        Console.WriteLine("To change download path take a look at the config (-c) \nFor override download path use -d\n");

        List<string> tracksWithoutLyrics = new List<string>();
        try
        {
            Uri? uri = null;
            bool isSpotifyLink =
                (target.Contains("spotify.com") || target.StartsWith("spotify:"))
                && Uri.TryCreate(target, UriKind.Absolute, out uri);

            bool isLikelyId =
                !isSpotifyLink
                && !Path.IsPathRooted(target)
                && !target.Contains(Path.DirectorySeparatorChar)
                && target.Length > 10;

            bool isDirectory = Directory.Exists(target);

            if (isSpotifyLink || isLikelyId)
            {
                string itemType = DetectSpotifyItemType(target, uri);

                if (itemType == "album")
                {
                    var (trackIds, folderName) = await _lyricsHandler!.GetAlbumTracksAndFolderAsync(target);
                    tracksWithoutLyrics = await _lyricsHandler.DownloadLyricsForTracksAsync(trackIds, folderName);
                }
                else if (itemType == "playlist")
                {
                    var (trackIds, folderName) = await _lyricsHandler!.GetPlaylistTracksAndFolderAsync(target);
                    tracksWithoutLyrics = await _lyricsHandler.DownloadLyricsForTracksAsync(trackIds, folderName);
                }
                else if (itemType == "track")
                {
                    string trackId = _lyricsHandler!.ExtractIdFromUrl(target, "track");
                    tracksWithoutLyrics = await _lyricsHandler.DownloadLyricsForTracksAsync(new List<string> { trackId });
                }
                else
                {
                    Console.Error.WriteLine($"Invalid or unsupported Spotify URL/ID: {target}");
                    return;
                }
            }
            else if (isDirectory)
            {
                tracksWithoutLyrics = await _fileMetadataReader!.FetchLyricsForLocalFilesAsync(target);
            }
            else
            {
                Console.Error.WriteLine($"Invalid input: '{target}'. Please provide a valid Spotify URL/ID or an existing local directory path.");
                return;
            }
        }
        catch (NotValidSpDcException ex)
        {
            Console.Error.WriteLine($"Authentication Error: {ex.Message}");
            Console.Error.WriteLine("Please ensure your sp_dc token is correct and valid. Run with '--config edit' to update.");
        }
        catch (ApiException ex)
        {
            Console.Error.WriteLine($"Spotify API Error: {ex.Message}");
            if (ex.InnerException != null)
                Console.Error.WriteLine($"  Details: {ex.InnerException.Message}");
        }
        catch (NoSongPlayingException ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
        }
        catch (CorruptedConfigException ex)
        {
            Console.Error.WriteLine($"Configuration Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }

        if (tracksWithoutLyrics.Any())
        {
            Console.WriteLine("\nLyrics could not be found or downloaded for the following tracks:");
            foreach (var trackName in tracksWithoutLyrics)
            {
                Console.WriteLine($"- {trackName}");
            }
        }
        else
        {
            Console.WriteLine("\nProcessing complete.");
        }
    }

    private static void HandleConfigAction(string action)
    {
        switch (action.ToLowerInvariant())
        {
            case "edit":
                ConfigurationManager.EditConfigInteractively(reset: false);
                Environment.Exit(0);
                break;
            case "reset":
                ConfigurationManager.EditConfigInteractively(reset: true);
                Environment.Exit(0);
                break;
            case "open":
                ConfigurationManager.OpenConfig();
                Environment.Exit(0);
                break;
            default:
                Console.Error.WriteLine($"Invalid config action: '{action}'. Use 'edit', 'reset', or 'open'.");
                Environment.Exit(1);
                break;
        }
    }

    private static async Task<bool> InitializeAsync(string? directoryOverride, bool forceOverride, bool ForceCacheClear = false)
    {
        try
        {
            if (!ConfigurationManager.ConfigExists())
            {
                ConfigurationManager.LoadConfig();
                Console.WriteLine($"Default config file created at: {ConfigurationManager.GetConfigFilePath()}");
                Console.WriteLine("Please run '--config edit' to set your 'sp_dc' token before proceeding.");
                return false;
            }

            _config = ConfigurationManager.LoadConfig();

            if (!string.IsNullOrWhiteSpace(directoryOverride))
            {
                _config.DownloadPath = directoryOverride;
            }
            if (forceOverride)
            {
                _config.ForceDownload = true;
            }

            if (string.IsNullOrWhiteSpace(_config.SpDc))
            {
                Console.Error.WriteLine("Error: Spotify 'sp_dc' token is missing in the configuration.");
                Console.Error.WriteLine($"Config file location: {ConfigurationManager.GetConfigFilePath()}");
                Console.Error.WriteLine("Please run '--config edit' to set it.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(_config.DownloadPath))
            {
                Console.Error.WriteLine("Error: Download path is missing in the configuration.");
                Console.Error.WriteLine("Please run '--config edit' to set it.");
                return false;
            }

            _client = new SpotifyClient(_config.SpDc);
            if (ForceCacheClear)
                _client.RemoveCaches();
            await _client.LoginAsync();

            _lyricsHandler = new LyricsHandler(_client, _config);
            _fileMetadataReader = new FileMetadataReader(_client, _config, _lyricsHandler);

            return true;
        }
        catch (CorruptedConfigException ex)
        {
            Console.Error.WriteLine($"Configuration Error: {ex.Message}");
            return false;
        }
        catch (NotValidSpDcException ex)
        {
            Console.Error.WriteLine($"Authentication Error: {ex.Message}");
            Console.Error.WriteLine("Please check your sp_dc token validity and network connection.");
            Console.Error.WriteLine("Try again with Clearcache; for more information, check the help or the repository.");
            return false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Initialization failed: {ex.Message}");
            return false;
        }
    }

    private static async Task<string?> DetermineTargetAsync(string? url, string? userItem)
    {
        if (!string.IsNullOrEmpty(userItem))
        {
            switch (userItem.ToLowerInvariant())
            {
                case "current":
                    try
                    {
                        var current = await _client!.GetCurrentSongAsync();
                        if (current?.Item?.ExternalUrls?.TryGetValue("spotify", out string? spotifyUrl) == true)
                        {
                            return spotifyUrl;
                        }
                        else
                        {
                            Console.Error.WriteLine("Could not get currently playing song, or no song is playing.");
                            return null;
                        }
                    }
                    catch (NoSongPlayingException ex)
                    {
                        Console.Error.WriteLine($"Error getting current song: {ex.Message}");
                        return null;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Unexpected error getting current song: {ex.Message}");
                        return null;
                    }

                case "album":
                    var selectedAlbum = await SelectUserAlbumAsync();
                    return selectedAlbum?.Uri;

                case "play":
                    var selectedPlaylist = await SelectUserPlaylistAsync();
                    return selectedPlaylist?.Uri;

                default:
                    Console.Error.WriteLine($"Invalid user item specified: '{userItem}'. Use 'current', 'album', or 'play'.");
                    return null;
            }
        }
        else if (!string.IsNullOrEmpty(url))
        {
            return url;
        }

        return null;
    }

    private static async Task<SimplePlaylistObject?> SelectUserPlaylistAsync()
    {
        Console.WriteLine("Fetching your playlists...");
        try
        {
            var playlistsPage = await _client!.GetCurrentUserPlaylistsAsync(limit: 50);
            var playlists = playlistsPage?.Items;

            if (playlists == null || !playlists.Any())
            {
                Console.WriteLine("No playlists found or could not fetch playlists.");
                return null;
            }

            Console.WriteLine("Select a playlist:");
            for (int i = 0; i < playlists.Count; i++)
            {
                Console.WriteLine($"{i + 1}: {playlists[i].Name} ({(playlists[i].Owner?.DisplayName ?? "Unknown Owner")})");
            }

            while (true)
            {
                Console.Write("Enter the number of the playlist: ");
                if (int.TryParse(Console.ReadLine(), out int index) && index >= 1 && index <= playlists.Count)
                {
                    return playlists[index - 1];
                }
                Console.WriteLine("Invalid input. Please enter a valid number.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching playlists: {ex.Message}");
            return null;
        }
    }

    private static async Task<SimpleAlbumObject?> SelectUserAlbumAsync()
    {
        Console.WriteLine("Fetching your saved albums...");
        try
        {
            var albumsPage = await _client!.GetCurrentUserSavedAlbumsAsync(limit: 50);
            var savedAlbums = albumsPage?.Items;

            if (savedAlbums == null || !savedAlbums.Any())
            {
                Console.WriteLine("No saved albums found or could not fetch albums.");
                return null;
            }

            var albums = savedAlbums.Select(sa => sa.Album).Where(a => a != null).ToList();
            if (!albums.Any())
            {
                Console.WriteLine("No valid album data found in saved items.");
                return null;
            }

            Console.WriteLine("Select an album:");
            for (int i = 0; i < albums.Count; i++)
            {
                string artists = string.Join(", ", albums[i]!.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>());
                Console.WriteLine($"{i + 1}: {albums[i]!.Name} ({artists})");
            }

            while (true)
            {
                Console.Write("Enter the number of the album: ");
                if (int.TryParse(Console.ReadLine(), out int index) && index >= 1 && index <= albums.Count)
                {
                    var selectedFullAlbum = albums[index - 1];
                    if (selectedFullAlbum == null) return null;

                    return new SimpleAlbumObject
                    {
                        Id = selectedFullAlbum.Id,
                        Name = selectedFullAlbum.Name,
                        Uri = selectedFullAlbum.Uri,
                        Artists = selectedFullAlbum.Artists,
                        Images = selectedFullAlbum.Images
                    };
                }
                Console.WriteLine("Invalid input. Please enter a valid number.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching saved albums: {ex.Message}");
            return null;
        }
    }

    private static void PrintLogo()
    {
        string logo = """
                         
            $$$$$$\   $$$$$$\  $$\                                      $$$$$$\                       $$\     $$\ $$\                          $$\                     
            $$  __$$\ $$  __$$\ $$ |                                    $$  __$$\                      $$ |    \__|$$ |                         \__|                    
            $$ /  \__|$$ /  \__|$$$$$$$\   $$$$$$\   $$$$$$\   $$$$$$\  $$ /  \__| $$$$$$\   $$$$$$\ $$$$$$\   $$\ $$ |     $$\   $$\  $$$$$$\  $$\  $$$$$$$\  $$$$$$$\ 
            $$ |      \$$$$$$\  $$  __$$\  \____$$\ $$  __$$\ $$  __$$\ \$$$$$$\  $$  __$$\ $$  __$$\\_$$  _|  $$ |$$ |     $$ |  $$ |$$  __$$\ $$ |$$  _____|$$  _____|
            $$ |       \____$$\ $$ |  $$ | $$$$$$$ |$$ |  \__|$$ /  $$ | \____$$\ $$ /  $$ |$$ /  $$ | $$ |    $$ |$$ |     $$ |  $$ |$$ |  \__|$$ |$$ /      \$$$$$$\  
            $$ |  $$\ $$\   $$ |$$ |  $$ |$$  __$$ |$$ |      $$ |  $$ |$$\   $$ |$$ |  $$ |$$ |  $$ | $$ |$$\ $$ |$$ |     $$ |  $$ |$$ |      $$ |$$ |       \____$$\ 
            \$$$$$$  |\$$$$$$  |$$ |  $$ |\$$$$$$$ |$$ |      $$$$$$$  |\$$$$$$  |$$$$$$$  |\$$$$$$  | \$$$$  |$$ |$$$$$$$$\\$$$$$$$ |$$ |      $$ |\$$$$$$$\ $$$$$$$  |
             \______/  \______/ \__|  \__| \_______|\__|      $$  ____/  \______/ $$  ____/  \______/   \____/ \__|\________|\____$$ |\__|      \__| \_______|\_______/ 
                                                              $$ |                $$ |                                      $$\   $$ |                                  
            $$$$$$$\                   $$$$$$\                $$ |                $$ |                                      \$$$$$$  |                                  
            $$  __$$\                 $$  __$$\ $$\$$\        \__|                \__|                                       \______/                                   
            $$ |  $$ |$$\   $$\       $$ /  \__|\$$$  |  $$$$$$\   $$$$$$\                                                                                              
            $$$$$$$\ |$$ |  $$ |      \$$$$$$\ $$$$$$$\ $$  __$$\ $$  __$$\                                                                                             
            $$  __$$\ $$ |  $$ |       \____$$\\_$$$ __|$$ |  \__|$$ /  $$ |                                                                                            
            $$ |  $$ |$$ |  $$ |      $$\   $$ |$$ $$\  $$ |      $$ |  $$ |                                                                                            
            $$$$$$$  |\$$$$$$$ |      \$$$$$$  |\__\__| $$ |      $$$$$$$  |                                                                                            
            \_______/  \____$$ |       \______/         \__|      $$  ____/                                                                                             
                      $$\   $$ |                                  $$ |                                                                                                  
                      \$$$$$$  |                                  $$ |                                                                                                  
                       \______/                                   \__|    
                       
            """;
        Console.WriteLine(logo);

        string version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                         ?? typeof(Program).Assembly.GetName().Version?.ToString()
                         ?? "2.0.2";

        if (version.Contains('+'))
            version = version.Split('+')[0];

        Console.WriteLine($"Version : {version}");

        // Canary / Pre-release Console Warning
        if (version.Contains("canary", StringComparison.OrdinalIgnoreCase) ||
            version.Contains("beta", StringComparison.OrdinalIgnoreCase) ||
            version.Contains("alpha", StringComparison.OrdinalIgnoreCase) ||
            version.Contains("rc", StringComparison.OrdinalIgnoreCase) ||
            version.Contains("preview", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[WARNING] You are running an experimental Canary / Pre-release build.");
            Console.WriteLine("[WARNING] Features may be unstable or subject to breaking changes.\n");
            Console.ResetColor();
        }
    }

    private static async Task PrintUserInfoAsync()
    {
        try
        {
            var user = await _client!.GetMeAsync();
            if (user != null)
            {
                Console.WriteLine("Successfully Logged In as:");
                Console.WriteLine($"Name: {user.DisplayName ?? "N/A"}");
                Console.WriteLine($"Country: {user.Country ?? "N/A"}");
                Console.WriteLine($"UserID: {user.Id ?? "N/A"}");
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("Could not retrieve user information.\n");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching user info: {ex.Message}\n");
        }
    }

    private static string DetectSpotifyItemType(string input, Uri? uri)
    {
        if (uri != null)
        {
            if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                if (uri.Segments.Length >= 2)
                {
                    string typeSegment = uri.Segments[uri.Segments.Length - 2].TrimEnd('/');
                    if (typeSegment == "track" || typeSegment == "album" || typeSegment == "playlist")
                        return typeSegment;
                }
            }
            else if (uri.Scheme == "spotify")
            {
                var parts = uri.AbsolutePath.Split(':');
                if (parts.Length >= 1 && (parts[0] == "track" || parts[0] == "album" || parts[0] == "playlist"))
                    return parts[0];
            }
        }
        if (!input.Contains('/') && !input.Contains('\\') && input.Length > 15)
        {
            Console.WriteLine("Warning: Could not definitively determine Spotify item type from input. Assuming track ID.");
            return "track";
        }

        return "unknown";
    }
}
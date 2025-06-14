/*
Author : s*rp
Purpose Of File : Main entry point for the CSharpSpotiLyrics console application.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Reflection.Metadata;
using CSharpSpotiLyrics.Console.App;
using CSharpSpotiLyrics.Core.Api;
using CSharpSpotiLyrics.Core.Exceptions;
using CSharpSpotiLyrics.Core.Models; // For selection models

public class Program
{
    private static Config? _config;
    private static SpotifyClient? _client;
    private static LyricsHandler? _lyricsHandler;
    private static FileMetadataReader? _fileMetadataReader;

    static async Task<int> Main(string[] args)
    {
        // --- Define Command Line Arguments ---
        var rootCommand = new RootCommand(
            "CSharpSpotiLyrics: Download Spotify lyrics (.lrc files)."
        );

        var urlArgument = new Argument<string?>(
            name: "url",
            description: "URL/ID of Song, Album, or Playlist from Spotify, or path to a local directory containing audio files.",
            getDefaultValue: () => null // Make it optional initially
        );

        var directoryOption = new Option<string?>( // Allow overriding config download path
            aliases: new[] { "-d", "--directory" },
            description: "Path to the download directory. Overrides config setting."
        );

        var forceOption = new Option<bool>(
            aliases: new[] { "-f", "--force" },
            description: "Force download, skip check if lyrics file already exists. Overrides config setting.",
            getDefaultValue: () => false // Default to not forcing via CLI flag
        );

        var configOption = new Option<string?>(
            aliases: new[] { "-c", "--config" },
            description: "Manage the configuration file.",
            getDefaultValue: () => null
        )
        {
            ArgumentHelpName = "action (edit|reset|open)"
        };
        configOption.AddCompletions("edit", "reset", "open"); // Suggest possible values

        var userOption = new Option<string?>(
            aliases: new[] { "-u", "--user" },
            description: "Download items from the logged-in user's library.",
            getDefaultValue: () => null
        )
        {
            ArgumentHelpName = "item (current|album|play)"
        };
        userOption.AddCompletions("current", "album", "play"); // Suggest possible values

        rootCommand.AddArgument(urlArgument);
        rootCommand.AddOption(directoryOption);
        rootCommand.AddOption(forceOption);
        rootCommand.AddOption(configOption);
        rootCommand.AddOption(userOption);

        // --- Set Handler for the Root Command ---
        rootCommand.SetHandler(
            async (InvocationContext context) =>
            {
                var url = context.ParseResult.GetValueForArgument(urlArgument);
                var directory = context.ParseResult.GetValueForOption(directoryOption);
                var force = context.ParseResult.GetValueForOption(forceOption);
                var configAction = context.ParseResult.GetValueForOption(configOption);
                var userItem = context.ParseResult.GetValueForOption(userOption);
                if (
                    string.IsNullOrEmpty(url)
                    && string.IsNullOrEmpty(userItem)
                    && string.IsNullOrEmpty(configAction)
                )
                {
                    await rootCommand.InvokeAsync("--help"); //show help to newbies ;)
                    return;
                }
                await RunApplicationLogic(url, directory, force, configAction, userItem);
            }
        );

        // --- Invoke the command ---
        return await rootCommand.InvokeAsync(args);
    }

    // --- Main Application Logic ---
    private static async Task RunApplicationLogic(
        string? url,
        string? directoryOverride,
        bool forceOverride,
        string? configAction,
        string? userItem
    )
    {
        // Handle config actions first, as they might exit
        if (!string.IsNullOrEmpty(configAction))
        {
            HandleConfigAction(configAction); // This might exit
            return; // Exit if config action was handled (e.g., editing)
        }

        // Perform initial setup: Load config, initialize client
        if (!await InitializeAsync(directoryOverride, forceOverride))
        {
            return; // Initialization failed (e.g., bad config, failed login)
        }

        // Determine the target URL/path based on user input or options
        string? target = await DetermineTargetAsync(url, userItem);
        if (target == null)
        {
            if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(userItem))
            {
                /*Console.WriteLine(
                    "Please provide a Spotify URL/ID, a local directory path, or use the --user option."
                );*/

                // Better to show help :> TODO (alr showed but i can forget there lul)
            }
            // Error messages for invalid user options etc. handled within DetermineTargetAsync
            return;
        }

        // Logo and User Info
        PrintLogo();
        await PrintUserInfoAsync();
        Console.WriteLine($"Current download path : \"{_config.DownloadPath}\"");
        Console.WriteLine(
            $"To change download path take a look at the config (-c) \nFor override download path use -d"
        );
        // Process the target
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
                && target.Length > 10; // Basic ID guess
            bool isDirectory = Directory.Exists(target);

            if (isSpotifyLink || isLikelyId)
            {
                string itemType = DetectSpotifyItemType(target, uri); // Detect album/playlist/track

                if (itemType == "album")
                {
                    var (trackIds, folderName) = await _lyricsHandler!.GetAlbumTracksAndFolderAsync(
                        target
                    );
                    tracksWithoutLyrics = await _lyricsHandler.DownloadLyricsForTracksAsync(
                        trackIds,
                        folderName
                    );
                }
                else if (itemType == "playlist")
                {
                    var (trackIds, folderName) =
                        await _lyricsHandler!.GetPlaylistTracksAndFolderAsync(target);
                    tracksWithoutLyrics = await _lyricsHandler.DownloadLyricsForTracksAsync(
                        trackIds,
                        folderName
                    );
                }
                else if (itemType == "track")
                {
                    string trackId = _lyricsHandler!.ExtractIdFromUrl(target, "track");
                    tracksWithoutLyrics = await _lyricsHandler.DownloadLyricsForTracksAsync(
                        new List<string> { trackId }
                    );
                }
                else
                {
                    Console.Error.WriteLine($"Invalid or unsupported Spotify URL/ID: {target}");
                    return;
                }
            }
            else if (isDirectory)
            {
                tracksWithoutLyrics = await _fileMetadataReader!.FetchLyricsForLocalFilesAsync(
                    target
                );
            }
            else
            {
                Console.Error.WriteLine(
                    $"Invalid input: '{target}'. Please provide a valid Spotify URL/ID or an existing local directory path."
                );
                return;
            }
        }
        catch (NotValidSpDcException ex)
        {
            Console.Error.WriteLine($"Authentication Error: {ex.Message}");
            Console.Error.WriteLine(
                "Please ensure your sp_dc token is correct and valid. Run with '--config edit' to update."
            );
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
        catch (Exception ex) // Catch-all for unexpected errors
        {
            Console.Error.WriteLine($"An unexpected error occurred: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace); // Provide stack trace for debugging
        }

        // Report tracks without lyrics
        if (tracksWithoutLyrics.Any())
        {
            Console.WriteLine(
                "\nLyrics could not be found or downloaded for the following tracks:"
            );
            foreach (var trackName in tracksWithoutLyrics)
            {
                Console.WriteLine($"- {trackName}");
            }
        }
        else
        {
            // Check if any processing happened. If target was invalid, this might be misleading.
            // Maybe add a flag to track if any processing attempt was made.
            Console.WriteLine("\nProcessing complete.");
        }
    }

    private static void HandleConfigAction(string action)
    {
        switch (action.ToLowerInvariant())
        {
            case "edit":
                ConfigurationManager.EditConfigInteractively(reset: false);
                Environment.Exit(0); // Exit after editing
                break;
            case "reset":
                ConfigurationManager.EditConfigInteractively(reset: true);
                Environment.Exit(0); // Exit after resetting
                break;
            case "open":
                ConfigurationManager.OpenConfig();
                Environment.Exit(0); // Exit after trying to open
                break;
            default:
                Console.Error.WriteLine(
                    $"Invalid config action: '{action}'. Use 'edit', 'reset', or 'open'."
                );
                Environment.Exit(1); // Exit with error code
                break;
        }
    }

    private static async Task<bool> InitializeAsync(string? directoryOverride, bool forceOverride)
    {
        try
        {
            // Ensure config file exists or is created, then load it
            if (!ConfigurationManager.ConfigExists())
            {
                ConfigurationManager.LoadConfig(); // This creates and saves default if missing
                Console.WriteLine(
                    $"Default config file created at: {ConfigurationManager.GetConfigFilePath()}"
                );
                Console.WriteLine(
                    "Please run '--config edit' to set your 'sp_dc' token before proceeding."
                );
                return false; // Require user to edit first time.
            }

            _config = ConfigurationManager.LoadConfig();

            // Apply command-line overrides
            if (!string.IsNullOrWhiteSpace(directoryOverride))
            {
                _config.DownloadPath = directoryOverride;
            }
            // CLI --force overrides config setting ONLY if set to true.
            // If CLI is false (default), the config value is used.
            if (forceOverride)
            {
                _config.ForceDownload = true;
            }

            // Validate essential config
            if (string.IsNullOrWhiteSpace(_config.SpDc))
            {
                Console.Error.WriteLine(
                    "Error: Spotify 'sp_dc' token is missing in the configuration."
                );
                Console.Error.WriteLine(
                    $"Config file location: {ConfigurationManager.GetConfigFilePath()}"
                );
                Console.Error.WriteLine("Please run '--config edit' to set it.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(_config.DownloadPath))
            {
                Console.Error.WriteLine("Error: Download path is missing in the configuration.");
                Console.Error.WriteLine("Please run '--config edit' to set it.");
                return false;
            }

            // Initialize Spotify Client and attempt login
            _client = new SpotifyClient(_config.SpDc);
            await _client.LoginAsync(); // Attempt login early to check token

            // Initialize handlers
            _lyricsHandler = new LyricsHandler(_client, _config);
            _fileMetadataReader = new FileMetadataReader(_client, _config, _lyricsHandler);

            return true; // Initialization successful
        }
        catch (CorruptedConfigException ex)
        {
            Console.Error.WriteLine($"Configuration Error: {ex.Message}");
            return false;
        }
        catch (NotValidSpDcException ex)
        {
            Console.Error.WriteLine($"Authentication Error: {ex.Message}");
            Console.Error.WriteLine(
                "Please check your sp_dc token validity and network connection."
            );
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
                        if (
                            current?.Item?.ExternalUrls?.TryGetValue(
                                "spotify",
                                out string? spotifyUrl
                            ) == true
                        )
                        {
                            return spotifyUrl;
                        }
                        else
                        {
                            Console.Error.WriteLine(
                                "Could not get currently playing song, or no song is playing."
                            );
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
                        Console.Error.WriteLine(
                            $"Unexpected error getting current song: {ex.Message}"
                        );
                        return null;
                    }

                case "album":
                    var selectedAlbum = await SelectUserAlbumAsync();
                    return selectedAlbum?.ExternalUrls?["spotify"]; // Return URL if selected

                case "play":
                    var selectedPlaylist = await SelectUserPlaylistAsync();
                    return selectedPlaylist?.ExternalUrls?["spotify"]; // Return URL if selected

                default:
                    Console.Error.WriteLine(
                        $"Invalid user item specified: '{userItem}'. Use 'current', 'album', or 'play'."
                    );
                    return null;
            }
        }
        else if (!string.IsNullOrEmpty(url))
        {
            return url; // Use the URL/path provided directly
        }
        else
        {
            // No URL/path and no --user option provided
            return null;
        }
    }

    private static async Task<SimplePlaylistObject?> SelectUserPlaylistAsync()
    {
        Console.WriteLine("Fetching your playlists...");
        try
        {
            // Note: This only fetches the first page (default 50). Implement pagination if needed.
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
                Console.WriteLine(
                    $"{i + 1}: {playlists[i].Name} ({(playlists[i].Owner?.DisplayName ?? "Unknown Owner")})"
                );
            }

            while (true)
            {
                Console.Write("Enter the number of the playlist: ");
                if (
                    int.TryParse(Console.ReadLine(), out int index)
                    && index >= 1
                    && index <= playlists.Count
                )
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

    private static async Task<SimpleAlbumObject?> SelectUserAlbumAsync() // Note: API returns SavedAlbumObject which contains SpotifyAlbum
    {
        Console.WriteLine("Fetching your saved albums...");
        try
        {
            // Note: This only fetches the first page (default 50). Implement pagination if needed.
            var albumsPage = await _client!.GetCurrentUserSavedAlbumsAsync(limit: 50);
            var savedAlbums = albumsPage?.Items;

            if (savedAlbums == null || !savedAlbums.Any())
            {
                Console.WriteLine("No saved albums found or could not fetch albums.");
                return null;
            }

            // Extract the actual album data for display and return
            var albums = savedAlbums.Select(sa => sa.Album).Where(a => a != null).ToList();
            if (!albums.Any())
            {
                Console.WriteLine("No valid album data found in saved items.");
                return null;
            }

            Console.WriteLine("Select an album:");
            for (int i = 0; i < albums.Count; i++)
            {
                // Need to get SimpleAlbumObject representation if the return type is SpotifyAlbum
                // For simplicity, let's assume we can access needed fields directly from SpotifyAlbum
                string artists = string.Join(
                    ", ",
                    albums[i]!.Artists?.Select(a => a.Name) ?? Enumerable.Empty<string>()
                );
                Console.WriteLine($"{i + 1}: {albums[i]!.Name} ({artists})");
            }

            while (true)
            {
                Console.Write("Enter the number of the album: ");
                if (
                    int.TryParse(Console.ReadLine(), out int index)
                    && index >= 1
                    && index <= albums.Count
                )
                {
                    // We need to return SimpleAlbumObject, but we have SpotifyAlbum.
                    // Construct one or ensure the caller can handle SpotifyAlbum.
                    // For now, let's return the SpotifyAlbum and caller adapts, or create SimpleAlbumObject here.
                    // Returning null for now as the types mismatch - needs refactoring of models or selection logic
                    // return albums[index - 1]; // This returns SpotifyAlbum?
                    var selectedFullAlbum = albums[index - 1];
                    if (selectedFullAlbum == null)
                    {
                        Console.WriteLine("selectedFullAlbum was null.");
                        return null;
                    }
                    else
                    {
                        // Create a SimpleAlbumObject from the full album if needed by downstream code
                        return new SimpleAlbumObject
                        {
                            Id = selectedFullAlbum.Id,
                            Name = selectedFullAlbum.Name,
                            ExternalUrls = selectedFullAlbum.ExternalUrls,
                            Artists = selectedFullAlbum.Artists, // Assuming SimpleArtistObject matches
                            Images = selectedFullAlbum.Images,
                            // ... copy other relevant fields ...
                        };
                    }
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
                Console.WriteLine(); // Newline amazing
            }
            else
            {
                Console.WriteLine("Could not retrieve user information.");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error fetching user info: {ex.Message}");
            Console.WriteLine();
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
                    if (
                        typeSegment == "track"
                        || typeSegment == "album"
                        || typeSegment == "playlist"
                    )
                        return typeSegment;
                }
            }
            else if (uri.Scheme == "spotify")
            {
                var parts = uri.AbsolutePath.Split(':');
                if (
                    parts.Length >= 1
                    && (parts[0] == "track" || parts[0] == "album" || parts[0] == "playlist")
                )
                    return parts[0];
            }
        }
        if (!input.Contains('/') && !input.Contains('\\') && input.Length > 15)
        { // Very rough guess
            // Could try/catch API calls for album/playlist/track to determine type?
            // For now, let LyricsHandler handle potential errors from wrong IDs.
            Console.WriteLine(
                "Warning: Could not definitively determine Spotify item type from input. Assuming track ID."
            );
            return "track"; // Default guess
        } //Alicengiz oyunları

        return "unknown";
    }
}

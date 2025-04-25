# CSharpSpotiLyrics

A command-line tool built with C# to download lyrics from Spotify and save them as `.lrc` files. This tool can fetch lyrics for individual tracks, albums, playlists, your currently playing song, items from your library, or even attempt to find lyrics for local audio files based on their metadata. (Included with dll.)

---
## Alternative Languages (For README)
[Türkçe](https://github.com/s0rp/CSharpSpotiLyrics/blob/main/README_TR.md)
---

---
⚠️ **Disclaimer** ⚠️

**This project might violate Spotify's Terms of Service. Use it responsibly and at your own risk. The developers assume no liability for any consequences resulting from its use.**

---

## Features

*   Download lyrics for Spotify tracks, albums, or playlists using their URL or ID.
*   Fetch lyrics for local audio files in a specified directory by reading metadata and searching Spotify.
*   Download lyrics for the song currently playing on your Spotify account.
*   Interactively select and download lyrics for albums or playlists saved in your Spotify library.
*   Save lyrics in the standard `.lrc` format (synced lyrics).
*   Configuration file (`config.json`) for persistent settings (download path, `sp_dc` token).
*   Command-line options to override configuration settings (download path, force overwrite).
*   Interactive configuration management (edit, reset, open config file location).
*   Authenticates using your Spotify `sp_dc` cookie.
*   Reports tracks for which lyrics could not be found or downloaded.

## Prerequisites

*   **.NET SDK:** You need the .NET SDK installed (e.g., .NET 6.0 or later recommended) to build and run the project. Download from [here](https://dotnet.microsoft.com/download).
*   **Spotify `sp_dc` Cookie:** The application requires a valid `sp_dc` cookie from your Spotify web session for authentication.

## Installation / Setup

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/s0rp/CSharpSpotiLyrics
    cd CSharpSpotiLyrics
    ```
2.  **Build the Project (Optional but Recommended):**
    ```bash
    dotnet build -c Release
    ```
    This compiles the code. You can run it directly using `dotnet run` or publish it for a standalone executable. (Dont forget to cd Cli dir!)

## Configuration

Before using the application, you **must** configure your Spotify `sp_dc` cookie.

**1. How to get your `sp_dc` Cookie:**

*   Open your web browser and log in to [open.spotify.com](https://open.spotify.com).
*   Open your browser's Developer Tools (usually by pressing `F12`).
*   Go to the "Application" (Chrome/Edge) or "Storage" (Firefox) tab.
*   Find "Cookies" in the sidebar and select `https://open.spotify.com`.
*   Locate the cookie named `sp_dc`.
*   Copy its **value**. This is your token.

    **Security Note:** Keep your `sp_dc` token secure. Do not share it, as it grants access to your Spotify account.

**2. Setting the `sp_dc` Cookie in the App:**

*   Run the application with the `edit` config action for the first time:
    ```bash
    # From the project directory
    dotnet run -- --config edit
    ```
    Or, if you have published an executable (e.g., `CSharpSpotiLyrics.exe` or `CSharpSpotiLyrics`):
    ```bash
    ./CSharpSpotiLyrics --config edit
    ```
*   The application will guide you through creating/editing the configuration file (`config.json`).
*   Paste your copied `sp_dc` token when prompted.
*   Set your desired default download path.
*   Configure other options like `ForceDownload` if needed.

The configuration file is typically stored in a platform-specific application data folder. The application will show the path when you first run it or when editing.

**Other Config Actions:**

*   `--config reset`: Resets the configuration to default values (you will need to enter the `sp_dc` token again).
*   `--config open`: Attempts to open the directory containing the `config.json` file in your file explorer.

## Usage

Run the application from your terminal within the project directory using `dotnet run --` followed by arguments and options, or run the published executable directly.

**Basic Syntax:**

```bash
# Using dotnet run
dotnet run -- [options] [<url_or_path>]

# Using published executable (example)
./CSharpSpotiLyrics [options] [<url_or_path>]
```

**Arguments:**

*   `url_or_path` (Optional): The Spotify URL/ID (track, album, playlist) or the path to a local directory containing audio files.

**Options:**

*   `-d`, `--directory <path>`: Specify a download directory for this run, overriding the config.
*   `-f`, `--force`: Force download, even if `.lrc` files already exist. Overrides config setting.
*   `-c`, `--config <action>`: Manage configuration (`edit`, `reset`, `open`).
*   `-u`, `--user <item>`: Interact with the logged-in user's library (`current`, `album`, `play`).

**Examples:**

*   **Download lyrics for a specific track URL:**
    ```bash
    dotnet run -- "https://open.spotify.com/track/your_track_id"
    ```
*   **Download lyrics for an album ID:**
    ```bash
    dotnet run -- spotify:album:your_album_id
    ```
*   **Download lyrics for a playlist URL:**
    ```bash
    dotnet run -- "https://open.spotify.com/playlist/your_playlist_id"
    ```
*   **Fetch lyrics for local files in a directory:**
    ```bash
    dotnet run -- "/path/to/your/music/folder"
    ```
*   **Download lyrics for the currently playing song:**
    ```bash
    dotnet run -- --user current
    ```
*   **Download lyrics for an album from your library (interactive selection):**
    ```bash
    dotnet run -- --user album
    ```
*   **Download lyrics for a playlist from your library (interactive selection):**
    ```bash
    dotnet run -- --user play
    ```
*   **Download track lyrics, overriding download path:**
    ```bash
    dotnet run -- --directory "/custom/lyrics/path" "spotify:track:your_track_id"
    ```
*   **Force download lyrics for an album:**
    ```bash
    dotnet run -- --force "spotify:album:your_album_id"
    ```

## Credits

*   **Development & C# Implementation:** S0rp
*   **Code Rewriting & Arrangement:** Dixiz 3A
*   **Original Concept / Python Implementation Inspiration:** [syrics by akashrchandran](https://github.com/akashrchandran/syrics)

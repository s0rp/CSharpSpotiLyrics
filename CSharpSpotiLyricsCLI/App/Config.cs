/*
Author : s*rp
Purpose Of File : Defines the structure of the configuration file.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
namespace CSharpSpotiLyrics.Console.App
{
    public class Config
    {
        public string SpDc { get; set; } = "";
        public string DownloadPath { get; set; } = "downloads";
        public bool CreateFolder { get; set; } = true;
        public string AlbumFolderName { get; set; } = "{Name} - {Artists}"; // Use keys from SpotifyAlbum
        public string PlayFolderName { get; set; } = "{Name} - {Owner}"; // Use keys from SpotifyPlaylist
        public string FileName { get; set; } = "{TrackNumber}. {Name}"; // Use keys from TrackInfoPlaceholder
        public bool SyncedLyrics { get; set; } = true;
        public bool ForceDownload { get; set; } = false; // Overridden by CLI --force

        // --- Default values ---
        public static Config Default =>
            new Config
            {
                SpDc = "",
                DownloadPath = Path.Combine(Environment.CurrentDirectory, "downloads"), // Default to subfolder
                CreateFolder = true,
                AlbumFolderName = "{Name} - {Artists}",
                PlayFolderName = "{Name} - {Owner}",
                FileName = "{TrackNumber}. {Name}",
                SyncedLyrics = true,
                ForceDownload = false
            };
    }
}

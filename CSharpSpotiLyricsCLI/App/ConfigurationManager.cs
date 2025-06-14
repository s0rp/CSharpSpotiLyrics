/*
Author : s*rp
Purpose Of File : Manages loading, saving, and editing the configuration file.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Diagnostics;
using System.Text.Json;
using CSharpSpotiLyrics.Core.Exceptions;

namespace CSharpSpotiLyrics.Console.App
{
    public static class ConfigurationManager
    {
        private static readonly string ConfigFileName = "config.json";
        private static string? _configFilePath;

        private static readonly JsonSerializerOptions JsonOptions =
            new()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true // Be lenient on load
            };

        public static string GetConfigFilePath()
        {
            if (_configFilePath == null)
            {
                string configDir;
                if (OperatingSystem.IsWindows())
                {
                    configDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "CSharpSpotiLyrics"
                    );
                }
                else // Linux/macOS
                {
                    configDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".config",
                        "CSharpSpotiLyrics"
                    );
                }
                Directory.CreateDirectory(configDir); // Ensure it exists
                _configFilePath = Path.Combine(configDir, ConfigFileName);
            }
            return _configFilePath;
        }

        public static bool ConfigExists()
        {
            return File.Exists(GetConfigFilePath());
        }

        public static Config LoadConfig()
        {
            string filePath = GetConfigFilePath();
            if (!File.Exists(filePath))
            {
                System.Console.WriteLine(
                    "Config file not found. Creating default config. Please run 'CSharpSpotiLyrics --config edit' to set your sp_dc token."
                );
                var defaultConfig = Config.Default;
                SaveConfig(defaultConfig);
                return defaultConfig;
                // Or throw? Maybe returning default is better first run experience.
                // throw new FileNotFoundException("Config file not found.", filePath);
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<Config>(json, JsonOptions);
                return config
                    ?? throw new CorruptedConfigException(
                        $"Failed to deserialize config file: {filePath}"
                    );
            }
            catch (Exception ex) when (ex is JsonException || ex is NotSupportedException)
            {
                throw new CorruptedConfigException(
                    $"Config file seems corrupted: {filePath}. Run 'CSharpSpotiLyrics --config reset'.",
                    ex
                );
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to load config file: {filePath}", ex);
            }
        }

        public static void SaveConfig(Config config)
        {
            string filePath = GetConfigFilePath();
            try
            {
                string json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                // Log error
                System.Console.Error.WriteLine($"Error saving config to {filePath}: {ex.Message}");
                throw new ApplicationException($"Failed to save config file: {filePath}", ex);
            }
        }

        public static void EditConfigInteractively(bool reset = false)
        {
            System.Console.WriteLine(reset ? "Resetting Config File..." : "Editing Config File...");
            System.Console.WriteLine("To keep the current value, just press Enter.");
            System.Console.WriteLine("---------------------------------------------");

            Config currentConfig = reset
                ? Config.Default
                : (ConfigExists() ? LoadConfig() : Config.Default);
            Config newConfig = new(); // Start with empty to copy over edited values

            newConfig.SpDc = AskInput("Enter the sp_dc:", currentConfig.SpDc, isSensitive: true);
            newConfig.DownloadPath = AskInput(
                "Enter the download path:",
                currentConfig.DownloadPath
            );
            newConfig.CreateFolder = AskInputBool(
                "Create folder for album/playlists (true/false):",
                currentConfig.CreateFolder
            );
            newConfig.AlbumFolderName = AskInput(
                "Enter the album folder naming format:",
                currentConfig.AlbumFolderName
            );
            newConfig.PlayFolderName = AskInput(
                "Enter the playlist folder naming format:",
                currentConfig.PlayFolderName
            );
            newConfig.FileName = AskInput("Enter the file naming format:", currentConfig.FileName);
            newConfig.SyncedLyrics = AskInputBool(
                "Get synced lyrics (true/false):",
                currentConfig.SyncedLyrics
            );
            newConfig.ForceDownload = AskInputBool(
                "Skip check for if file already exists (true/false):",
                currentConfig.ForceDownload
            );

            SaveConfig(newConfig);
            System.Console.WriteLine("---------------------------------------------");
            System.Console.WriteLine("Config successfully saved.");
            if (string.IsNullOrWhiteSpace(newConfig.SpDc))
            {
                System.Console.WriteLine(
                    "WARNING: sp_dc token is empty. You need to set it to use the application."
                );
            }
            System.Console.WriteLine("Run the program again to use the new configuration.");
        }

        private static string AskInput(
            string question,
            string currentValue,
            bool isSensitive = false
        )
        {
            System.Console.WriteLine(question);
            string displayValue = isSensitive
                ? (
                    string.IsNullOrWhiteSpace(currentValue)
                        ? "[Not Set]"
                        : "[Set - Press Enter to keep]"
                )
                : $"[Current: {currentValue}]";
            System.Console.WriteLine(displayValue);
            System.Console.Write("> ");
            string? input = System.Console.ReadLine();
            return string.IsNullOrWhiteSpace(input) ? currentValue : input;
        }

        private static bool AskInputBool(string question, bool currentValue)
        {
            while (true)
            {
                string inputStr = AskInput(question, currentValue.ToString());
                if (bool.TryParse(inputStr, out bool result))
                {
                    return result;
                }
                if (inputStr.Equals(currentValue.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return currentValue; // User pressed Enter or entered the same value
                }
                System.Console.WriteLine("Invalid input. Please enter 'true' or 'false'.");
            }
        }

        public static void OpenConfig()
        {
            string filePath = GetConfigFilePath();
            if (!File.Exists(filePath))
            {
                System.Console.WriteLine(
                    $"Config file not found at {filePath}. Run '--config edit' to create it."
                );
                return;
            }

            try
            {
                // UseShellExecute allows opening with the default editor
                ProcessStartInfo psi = new ProcessStartInfo(filePath) { UseShellExecute = true };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                System.Console.Error.WriteLine(
                    $"Error opening config file '{filePath}': {ex.Message}"
                );
                // Fallback for Linux/macOS if UseShellExecute fails?
                if (!OperatingSystem.IsWindows())
                {
                    try
                    {
                        Process.Start("xdg-open", filePath);
                    } // Common Linux command
                    catch
                    {
                        try
                        {
                            Process.Start("open", filePath);
                        } // Common macOS command
                        catch
                        {
                            System.Console.Error.WriteLine(
                                "Could not automatically open editor. Please open the file manually."
                            );
                        }
                    }
                }
            }
        }
    }
}

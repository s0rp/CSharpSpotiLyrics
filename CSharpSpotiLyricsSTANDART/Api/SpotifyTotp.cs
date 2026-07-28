/*
Author : s*rp
Purpose Of File : Generates the specific TOTP needed for Spotify internal authentication.
Date : 24.04.2025
Update: 23.01.2026
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Playwright;

namespace CSharpSpotiLyrics.Core.Api
{
    public static class SpotifyTotp
    {
        private const int Period = 30;
        private const int Digits = 6;
        private static readonly string TotpFilePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ".SPOTIFYTOTP"
        );
        private static readonly string HashPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            ".SPOTIFYHASH"
        );

        public class TotpReturn
        {
            public string Totp { get; set; }
            public int Version { get; set; }
            public bool isCached { get; set; }
        }

        public class SecretVersionJSON
        {
            public string Secret { get; set; }
            public int Version { get; set; }
        }

        public static TotpReturn GenerateTotp(long serverTimeMilliseconds, bool force = false)
        {
            var (secret, version, cached) = GetSecretAndVersion(force);

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

        private static (byte[] secret, int version, bool cached) GetSecretAndVersion(bool forceNew = false)
        {
            SecretVersionJSON cache = null;
            try
            {
                /*if (File.Exists(TotpFilePath) && forceNew == false)
                    cache = JsonSerializer.Deserialize<SecretVersionJSON>(
                        File.ReadAllText(TotpFilePath)
                    );*/
            }
            catch
            {
                cache = null;
            }

        FETCH_SECRET:
            var extractedData = new SecretVersionJSON { Secret = string.Empty, Version = -1 };

            if (cache == null)
            {
                extractedData = Task.Run(async () =>
                {
                    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
                    if (exitCode != 0)
                        throw new Exception($"Failed to install Playwright Chromium. Exit code: {exitCode}");

                    using var playwright = await Playwright.CreateAsync();

                    IBrowser browser = await playwright.Chromium.LaunchAsync(
                        new BrowserTypeLaunchOptions
                        {
                            Args = new[]
                            {
                                "--no-sandbox",
                                "--disable-setuid-sandbox",
                                "--disable-dev-shm-usage",
                                "--disable-gpu",
                            },
                        }
                    );

                    string hook = @"(()=>{if(globalThis.__secretHookInstalled)return;globalThis.__secretHookInstalled=true;globalThis.__captures=[];
Object.defineProperty(Object.prototype,'secret',{configurable:true,set:function(v){try{__captures.push({secret:v,version:this.version,obj:this});}catch(e){}
Object.defineProperty(this,'secret',{value:v,writable:true,configurable:true,enumerable:true});}});})();";

                    var context = await browser.NewContextAsync();
                    await context.AddInitScriptAsync(hook);

                    var page = await context.NewPageAsync();
                    var jsFileContents = new List<string>();

                    try
                    {
                        page.Response += async (_, response) =>
                        {
                            var url = response.Url;

                            if (url.Contains("web-player") && url.EndsWith(".js") && response.Status == 200)
                            {
                                try
                                {
                                    var content = await response.TextAsync();
                                    jsFileContents.Add(content);
                                }
                                catch { }
                            }
                        };
                    }
                    catch { }

                    await page.GotoAsync("https://open.spotify.com");

                    try
                    {
                        await page.WaitForLoadStateAsync(
                            LoadState.NetworkIdle,
                            new PageWaitForLoadStateOptions { Timeout = 45000 }
                        );
                    }
                    catch { }

                    await page.WaitForTimeoutAsync(3000);

                    try
                    {
                        string pattern = @"[""']([^""']+)[""']\s*,\s*[""'](query|mutation)[""']\s*,\s*[""']([a-f0-9]{64})[""']";
                        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                        var queryHashes = new Dictionary<string, string>();

                        foreach (var jsContent in jsFileContents)
                        {
                            var matches = regex.Matches(jsContent);
                            foreach (Match match in matches)
                            {
                                string opName = match.Groups[1].Value;
                                string hash = match.Groups[3].Value;

                                if (!queryHashes.ContainsKey(opName))
                                {
                                    queryHashes[opName] = hash;
                                }
                            }
                        }

                        Console.WriteLine($"Total Hashes {queryHashes.Count} Found From open.spotify.com");

                        if (queryHashes.Count > 0)
                        {
                            File.WriteAllText(HashPath, JsonSerializer.Serialize(queryHashes));
                            Console.WriteLine($"[LOG] Hashes Saved To '{HashPath}'");
                        }
                    }
                    catch { }

                    var capturesHandle = await page.EvaluateHandleAsync("globalThis.__captures");
                    var jsonElement = await capturesHandle.JsonValueAsync<JsonElement>();

                    string bestSecret = null;
                    int bestVersion = -1;

                    if (jsonElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in jsonElement.EnumerateArray())
                        {
                            string currentSec = null;
                            int currentVer = 1;

                            if (item.TryGetProperty("secret", out var secretProp))
                            {
                                if (secretProp.ValueKind == JsonValueKind.String)
                                {
                                    currentSec = secretProp.GetString();
                                }
                                else if (secretProp.ValueKind == JsonValueKind.Object)
                                {
                                    if (secretProp.TryGetProperty("bytes", out var bytesProp) && bytesProp.ValueKind == JsonValueKind.String)
                                    {
                                        try
                                        {
                                            byte[] decodedBytes = Convert.FromBase64String(bytesProp.GetString());
                                            currentSec = Encoding.UTF8.GetString(decodedBytes);
                                        }
                                        catch { }
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(currentSec))
                                continue;

                            if (item.TryGetProperty("version", out var versionProp) && versionProp.ValueKind == JsonValueKind.Number)
                            {
                                currentVer = versionProp.GetInt32();
                            }
                            else if (item.TryGetProperty("obj", out var objProp) && objProp.ValueKind == JsonValueKind.Object)
                            {
                                if (objProp.TryGetProperty("version", out var subVerProp) && subVerProp.ValueKind == JsonValueKind.Number)
                                {
                                    currentVer = subVerProp.GetInt32();
                                }
                            }

                            if (currentVer > bestVersion)
                            {
                                bestVersion = currentVer;
                                bestSecret = currentSec;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(bestSecret))
                    {
                        File.WriteAllText(
                            TotpFilePath,
                            JsonSerializer.Serialize(new SecretVersionJSON
                            {
                                Secret = Base64Encode(bestSecret),
                                Version = bestVersion,
                            })
                        );
                    }

                    return new SecretVersionJSON { Secret = bestSecret, Version = bestVersion };

                }).GetAwaiter().GetResult();
            }
            else
            {
                try
                {
                    extractedData = new SecretVersionJSON
                    {
                        Secret = Base64Decode(cache.Secret),
                        Version = cache.Version,
                    };
                }
                catch
                {
                    cache = null;
                    goto FETCH_SECRET;
                }

                if (extractedData == null || string.IsNullOrEmpty(extractedData.Secret))
                {
                    cache = null;
                    goto FETCH_SECRET;
                }
            }

            if (string.IsNullOrEmpty(extractedData.Secret))
            {
                throw new InvalidOperationException("Failed to extract TOTP secret via Playwright.");
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
    }
}
# Canvas & Video Fetching

Spotify Canvas consists of short looping video loops shown behind songs. This library can fetch these MP4 video links directly.

## How It Works

Canvas URLs are retrieved via the internal `queryNpvArtist` GraphQL schema. The library makes an authenticated POST request to `https://api-partner.spotify.com/pathfinder/v2/query` containing persisted queries and SHA256 hashes.

## Implementation Example

Below is a complete example of how to initialize the client and fetch a track's canvas MP4 stream:

```csharp
using System;
using System.Threading.Tasks;
using CSharpSpotiLyrics.Core.Api;

class Program
{
    static async Task Main()
    {
        string spDcToken = "YOUR_SP_DC_COOKIE_HERE";
        
        using var client = new SpotifyClient(spDcToken);
        
        try
        {
            string artistId = "spotify:artist:1Xyo4u8uXC1ZmMfv76g0Sg"; 
            string trackId = "spotify:track:4PTG3Z6ehGkBF36IccG1S8";

            Console.WriteLine("Fetching Canvas URL...");
            string? canvasUrl = await client.GetCanvasUrlAsync(artistId, trackId);

            if (!string.IsNullOrEmpty(canvasUrl))
            {
                Console.WriteLine($"[Success] Canvas URL found: {canvasUrl}");
            }
            else
            {
                Console.WriteLine("[Notice] This track does not have an associated Canvas.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] {ex.Message}");
        }
    }
}
```

(PRE-BETA as v2.0.x)
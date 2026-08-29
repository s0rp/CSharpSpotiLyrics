# Exceptions & Troubleshooting

Overview of custom exceptions thrown by `CSharpSpotiLyrics` and common troubleshooting patterns.

## Custom Exceptions

The library defines specific exceptions under `CSharpSpotiLyrics.Core.Exceptions`:

| Exception Class | Throw Condition |
| :--- | :--- |
| `NotValidSpDcException` | Thrown if the `sp_dc` token is expired, system clock is desynchronized, or authentication redirect occurs. |
| `NoSongPlayingException` | Thrown if `GetCurrentSongAsync` fails due to connectivity issues (HTTP 204/404 is handled gracefully). |
| `LyricsNotFoundException` | Thrown when lyrics cannot be retrieved or found (HTTP 404). |
| `ApiException` | Thrown during partner GraphQL/REST failures (e.g., failed playlist/album metadata parsing). |
| `CorruptedConfigException` | Thrown when local configuration is invalid or corrupted. |

## Troubleshooting Guide

### 1. `sp_dc expired` or 302 Redirect Auth Failures
**Symptoms:** `NotValidSpDcException` is thrown immediately during `LoginAsync()`.
- **Cause:** Your browser `sp_dc` cookie might have expired or was invalidated (e.g., by logging out of the Web Player).
- **Solution:** Clear browser cookies, log in to `open.spotify.com`, and extract a fresh `sp_dc` cookie.

### 2. TOTP Generation Mismatch (Time Desync)
**Symptoms:** The client fails to obtain an access token even with a fresh `sp_dc`.
- **Cause:** The host machine's system clock is desynchronized by more than 30 seconds from the true internet time, causing TOTP generation to drift.
- **Solution:** Ensure your machine's system clock is synchronized with internet time servers (NTP). The library attempts to query `https://open.spotify.com/api/server-time` as a fallback, but system-wide clock drift can still impact local calculation.
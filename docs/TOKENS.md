# Internal Token & TOTP Parsing

To bypass the need for a bloated browser automation package (like Playwright), this library parses Spotify's internal web player files to perform Time-Based One-Time Password (TOTP) generation locally.

## The Bypass Mechanism

To request an active access token (`accessToken`) and client ID (`clientId`), the client simulates the initialization sequence of the Spotify Web Player:

1. **HTML Ingestion**: The client queries `https://open.spotify.com` and locates the active web-player JavaScript assets (`/cdn/build/web-player/*.js`).
2. **Regex Scanning**: Using optimized regex with strict execution timeouts, the client parses these JS assets looking for:
   - `secret`: An obfuscated or plaintext string key.
   - `version`: The TOTP protocol version used by Spotify's authentication token generator.
3. **De-obfuscation**: If the extracted secret is obfuscated, it performs a bitwise XOR rotation:
   `val ^ ((index % 33) + 9)`
4. **TOTP Generation**:
   - Compares host system time against `/api/server-time`.
   - Hashes the normalized unix timestamp counter (`timestamp / 30`) with the de-obfuscated secret using HMAC-SHA1.
   - Truncates the hash to a 6-digit integer string (OTP).
5. **Token Exchange**: The calculated Local and Server TOTP codes are passed directly to `https://open.spotify.com/api/token?totp={local}&totpServer={server}` to return a fresh OAuth `accessToken` starting with the prefix `BQ`.
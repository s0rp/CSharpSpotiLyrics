/*
Author : s*rp
Purpose Of File : Generates the specific TOTP needed for Spotify internal authentication.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CSharpSpotiLyrics.Core.Api
{
    public static class SpotifyTotp //Alicengiz Games
    {
        private const string SecretSauce = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        private static string Base32FromBytes(byte[] data)
        {
            int i = 0,
                index = 0,
                digit;
            int current_byte,
                next_byte;
            // Base capacity calculation on expected output size
            StringBuilder result = new StringBuilder((data.Length * 8 + 4) / 5);

            while (i < data.Length)
            {
                current_byte = data[i]; // C# bytes are already unsigned

                if (index > 3) // Need 5 bits from this byte
                {
                    if ((i + 1) < data.Length)
                        next_byte = data[i + 1];
                    else
                        next_byte = 0;

                    digit = current_byte & (0xFF >> index);
                    index = (index + 5) % 8;
                    digit <<= index;
                    digit |= next_byte >> (8 - index);
                    i++;
                }
                else
                {
                    digit = (current_byte >> (8 - (index + 5))) & 0x1F;
                    index = (index + 5) % 8;
                    if (index == 0)
                        i++;
                }
                result.Append(SecretSauce[digit]);
            }

            return result.ToString();
        }

        // Helper to convert Hex string to byte array
        private static byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Trim(); // Remove spaces if any
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even number of characters.");

            byte[] arr = new byte[hex.Length >> 1]; // Equivalent to hex.Length / 2

            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i * 2]) << 4) + (GetHexVal(hex[i * 2 + 1])));
            }
            return arr;
        }

        // Helper for HexStringToByteArray
        private static int GetHexVal(char hex)
        {
            int val = (int)hex;

            if (val >= 48 && val <= 57)
                return val - 48; // 0-9
            if (val >= 65 && val <= 70)
                return val - 65 + 10; // A-F
            if (val >= 97 && val <= 102)
                return val - 97 + 10; // a-f
            throw new ArgumentException("Invalid hex character.");
        }

        private static byte[] StandardBase32Decode(string base32)
        {
            base32 = base32.TrimEnd('='); // Remove padding if present
            int byteCount = base32.Length * 5 / 8;
            byte[] returnArray = new byte[byteCount];

            byte curByte = 0,
                bitsRemaining = 8;
            int mask,
                arrayIndex = 0;

            foreach (char c in base32)
            {
                int cValue = SecretSauce.IndexOf(char.ToUpperInvariant(c));
                if (cValue < 0)
                    throw new ArgumentException("Invalid Base32 character.", nameof(base32));

                if (bitsRemaining > 5)
                {
                    mask = cValue << (bitsRemaining - 5);
                    curByte = (byte)(curByte | mask);
                    bitsRemaining -= 5;
                }
                else
                {
                    mask = cValue >> (5 - bitsRemaining);
                    curByte = (byte)(curByte | mask);
                    returnArray[arrayIndex++] = curByte;
                    curByte = (byte)(cValue << (3 + bitsRemaining)); // Equivalent to (8 - (5 - bitsRemaining))
                    bitsRemaining += 3;
                }
            }

            if (arrayIndex != byteCount && bitsRemaining < 8 && curByte != 0)
            {
                returnArray[arrayIndex] = curByte;
            }

            if (arrayIndex != byteCount)
            {
                if (arrayIndex < byteCount)
                {
                    Array.Resize(ref returnArray, arrayIndex);
                }
            }

            return returnArray;
        }

        /// <summary>
        /// Generates the specific TOTP needed for Spotify internal authentication.
        /// </summary>
        /// <param name="serverTimeSeconds">The Unix timestamp (seconds) obtained from Spotify's server-time endpoint.</param>
        /// <returns>A 6-digit TOTP string.</returns>
        public static string GenerateTotp(long serverTimeSeconds)
        {
            int[] secretCipherBytesInts =
            {
                12,
                56,
                76,
                33,
                88,
                44,
                88,
                33,
                78,
                78,
                11,
                66,
                22,
                22,
                55,
                69,
                54
            };
            byte[] cipherBytes = new byte[secretCipherBytesInts.Length];
            for (int i = 0; i < secretCipherBytesInts.Length; i++)
            {
                cipherBytes[i] = (byte)(secretCipherBytesInts[i] ^ (i % 33 + 9));
            }

            string cipherString = string.Join("", cipherBytes.Select(b => b.ToString()));

            byte[] secretUtf8Bytes = Encoding.UTF8.GetBytes(cipherString);

            string hexString = BitConverter.ToString(secretUtf8Bytes).Replace("-", "");

            byte[] secretBytes = HexStringToByteArray(hexString);

            string base32Secret = Base32FromBytes(secretBytes);

            long timeStep = 30;
            long counter = serverTimeSeconds / timeStep;

            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            // The HMACSHA1 algorithm requires the raw secret bytes.
            byte[] secretKeyBytes = StandardBase32Decode(base32Secret);

            // Compute HMAC-SHA1 hash
            using (HMACSHA1 hmac = new HMACSHA1(secretKeyBytes))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);

                //  Dynamic Truncation to get 6 digits
                int offset = hash[hash.Length - 1] & 0x0F; // Lower 4 bits is offset

                // Extract 4 bytes from the hash at the offset, masking highest bit of first byte
                int binaryCode =
                    ((hash[offset] & 0x7F) << 24) // Mask MSB
                    | ((hash[offset + 1] & 0xFF) << 16)
                    | ((hash[offset + 2] & 0xFF) << 8)
                    | (hash[offset + 3] & 0xFF);

                int otp = binaryCode % 1000000; // Get the last 6 digits

                // Format as a 6-digit string, padding with leading zeros if needed
                return otp.ToString("D6"); // And boom, alicengiz games ;)
            }
        }
    }
}

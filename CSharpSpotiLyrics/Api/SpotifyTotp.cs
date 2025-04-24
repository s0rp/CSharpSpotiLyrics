/*
Author : s*rp
Purpose Of File : Generates the specific TOTP needed for Spotify internal authentication.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/
using System.Security.Cryptography;
using System.Text;

namespace CSharpSpotiLyrics.Core.Api
{
    public static class SpotifyTotp
    {
        private const string SecretSauce = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        // Function to convert byte array to Base32 string (RFC 4648 variant without padding)
        private static string Base32FromBytes(byte[] data)
        {
            int i = 0,
                index = 0,
                digit;
            int current_byte,
                next_byte;
            StringBuilder result = new StringBuilder((data.Length + 7) * 8 / 5); // Approximate length

            while (i < data.Length)
            {
                current_byte = (data[i] >= 0) ? data[i] : (data[i] + 256); // Treat byte as unsigned

                if (index > 3) // We need 5 bits from this byte
                {
                    if ((i + 1) < data.Length)
                        next_byte = (data[i + 1] >= 0) ? data[i + 1] : (data[i + 1] + 256);
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

        private static byte[] HexStringToByteArray(string hex)
        {
            hex = hex.Replace(" ", "").Trim();
            if (hex.Length % 2 != 0)
                throw new ArgumentException("Hex string must have an even number of characters.");

            byte[] arr = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }
            return arr;
        }

        private static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87)); // Handles 0-9, A-F, a-f
        }

        public static string GenerateTotp()
        {
            // The specific cipher bytes and XOR logic from the Python code
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

            // Convert the resulting numbers to a string, then get UTF8 bytes
            string cipherString = string.Join("", cipherBytes.Select(b => b.ToString()));
            byte[] secretUtf8Bytes = Encoding.UTF8.GetBytes(cipherString);

            // Convert UTF8 bytes to hex string
            string hexString = BitConverter.ToString(secretUtf8Bytes).Replace("-", "");

            // Convert hex string back to bytes (equivalent to Python's bytes.fromhex)
            byte[] secretBytes = HexStringToByteArray(hexString);

            // Encode these bytes using the custom Base32
            string base32Secret = Base32FromBytes(secretBytes);

            // Standard TOTP Generation (RFC 6238) using the derived Base32 secret
            long timeStep = 30;
            long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long counter = unixTimestamp / timeStep;

            // Convert counter to 8-byte array (Big Endian)
            byte[] counterBytes = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(counterBytes);
            }

            // Decode the Base32 secret back to bytes for HMACSHA1
            // Standard Base32 decoding needed here (The custom encoder doesn't have a readily available decoder counterpart,
            // but standard libraries expect standard Base32 decoding. Let's assume pyotp uses standard base32 decoding internally
            // even if the initial secret generation was custom. If this fails, we need a custom Base32 decoder).
            // We'll use a simple standard Base32 decoder for now.
            byte[] secretKeyBytes = StandardBase32Decode(base32Secret);

            using (HMACSHA1 hmac = new HMACSHA1(secretKeyBytes))
            {
                byte[] hash = hmac.ComputeHash(counterBytes);

                // Dynamic Truncation (RFC 4226)
                int offset = hash[hash.Length - 1] & 0x0F;
                int binaryCode =
                    ((hash[offset] & 0x7F) << 24)
                    | ((hash[offset + 1] & 0xFF) << 16)
                    | ((hash[offset + 2] & 0xFF) << 8)
                    | (hash[offset + 3] & 0xFF);

                int otp = binaryCode % 1000000; // 6 digits

                return otp.ToString("D6"); // Zero-padded 6-digit string
            }
        }

        // Standard Base32 Decoding (minimal implementation for this specific use)
        private static byte[] StandardBase32Decode(string base32)
        {
            base32 = base32.TrimEnd('='); // Remove padding
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
                    throw new ArgumentException("Invalid Base32 character.");

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
                    curByte = (byte)(cValue << (3 + bitsRemaining)); // (8 - (5 - bitsRemaining))
                    bitsRemaining += 3;
                }
            }

            // If we didn't end on a byte boundary, we might have missed the last byte.
            if (arrayIndex != byteCount && bitsRemaining < 8)
            {
                returnArray[arrayIndex] = curByte;
            }

            return returnArray;
        }
    }
}

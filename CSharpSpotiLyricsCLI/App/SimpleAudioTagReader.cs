using System;
using System.IO;
using System.Text;

namespace CSharpSpotiLyrics.Console.App
{
    public static class SimpleAudioTagReader
    {
        public static (string? Title, string? Artist, string? Album) ReadTags(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                byte[] header = new byte[4];
                if (fs.Read(header, 0, 4) < 4) return (null, null, null);

                if (header[0] == 'I' && header[1] == 'D' && header[2] == '3')
                {
                    return ReadId3v2(fs);
                }
                else if (header[0] == 'f' && header[1] == 'L' && header[2] == 'a' && header[3] == 'C')
                {
                    return ReadFlac(fs);
                }
            }
            catch { }
            return (null, null, null);
        }

        private static (string? Title, string? Artist, string? Album) ReadId3v2(FileStream fs)
        {
            fs.Position = 6;
            byte[] sizeBytes = new byte[4];
            fs.Read(sizeBytes, 0, 4);
            int tagSize = (sizeBytes[0] << 21) | (sizeBytes[1] << 14) | (sizeBytes[2] << 7) | sizeBytes[3];

            string? title = null, artist = null, album = null;
            long endPos = fs.Position + tagSize;

            while (fs.Position < endPos)
            {
                byte[] frameHeader = new byte[10];
                if (fs.Read(frameHeader, 0, 10) < 10) break;

                string frameId = Encoding.ASCII.GetString(frameHeader, 0, 4);
                if (frameId[0] == '\0') break;

                int frameSize = (frameHeader[4] << 24) | (frameHeader[5] << 16) | (frameHeader[6] << 8) | frameHeader[7];
                if (frameSize <= 0 || fs.Position + frameSize > endPos) break;

                if (frameId == "TIT2" || frameId == "TPE1" || frameId == "TALB")
                {
                    byte[] frameData = new byte[frameSize];
                    fs.Read(frameData, 0, frameSize);
                    string value = DecodeId3Text(frameData);
                    if (frameId == "TIT2") title = value;
                    else if (frameId == "TPE1") artist = value;
                    else if (frameId == "TALB") album = value;
                }
                else
                {
                    fs.Position += frameSize;
                }
            }
            return (title, artist, album);
        }

        private static string DecodeId3Text(byte[] data)
        {
            if (data.Length < 2) return "";
            byte encoding = data[0];
            return encoding switch
            {
                0 => Encoding.GetEncoding("ISO-8859-1").GetString(data, 1, data.Length - 1).Trim('\0'),
                1 => Encoding.Unicode.GetString(data, 1, data.Length - 1).Trim('\0'),
                2 => Encoding.BigEndianUnicode.GetString(data, 1, data.Length - 1).Trim('\0'),
                3 => Encoding.UTF8.GetString(data, 1, data.Length - 1).Trim('\0'),
                _ => Encoding.ASCII.GetString(data, 1, data.Length - 1).Trim('\0')
            };
        }

        private static (string? Title, string? Artist, string? Album) ReadFlac(FileStream fs)
        {
            bool isLast = false;
            while (!isLast)
            {
                int b = fs.ReadByte();
                if (b == -1) break;
                isLast = (b & 0x80) != 0;
                int type = b & 0x7F;

                byte[] lenBytes = new byte[3];
                fs.Read(lenBytes, 0, 3);
                int length = (lenBytes[0] << 16) | (lenBytes[1] << 8) | lenBytes[2];

                if (type == 4)
                {
                    return ParseVorbisComment(fs, length);
                }
                else
                {
                    fs.Position += length;
                }
            }
            return (null, null, null);
        }

        private static (string? Title, string? Artist, string? Album) ParseVorbisComment(FileStream fs, int length)
        {
            long startPos = fs.Position;
            byte[] vLengthBytes = new byte[4];
            fs.Read(vLengthBytes, 0, 4);
            int vLength = BitConverter.ToInt32(vLengthBytes, 0);
            fs.Position += vLength;

            fs.Read(vLengthBytes, 0, 4);
            int commentsCount = BitConverter.ToInt32(vLengthBytes, 0);

            string? title = null, artist = null, album = null;

            for (int i = 0; i < commentsCount; i++)
            {
                if (fs.Position >= startPos + length) break;
                fs.Read(vLengthBytes, 0, 4);
                int commentLen = BitConverter.ToInt32(vLengthBytes, 0);
                byte[] commentBytes = new byte[commentLen];
                fs.Read(commentBytes, 0, commentLen);

                string comment = Encoding.UTF8.GetString(commentBytes);
                int eqIndex = comment.IndexOf('=');
                if (eqIndex > 0)
                {
                    string key = comment.Substring(0, eqIndex).ToUpperInvariant();
                    string value = comment.Substring(eqIndex + 1);
                    if (key == "TITLE") title = value;
                    else if (key == "ARTIST") artist = value;
                    else if (key == "ALBUM") album = value;
                }
            }
            return (title, artist, album);
        }
    }
}
/*
Author : s*rp
Purpose Of File : Defines custom exceptions for the Spotify client.
Date : 24.04.2025
Supervisor : Dixiz 3A Neural (Coder MoE)
*/

using System;

namespace CSharpSpotiLyrics.Core.Exceptions
{
    public class NotValidSpDcException : Exception
    {
        public NotValidSpDcException() { }

        public NotValidSpDcException(string message)
            : base(message) { }

        public NotValidSpDcException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class CorruptedConfigException : Exception
    {
        public CorruptedConfigException() { }

        public CorruptedConfigException(string message)
            : base(message) { }

        public CorruptedConfigException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class NoSongPlayingException : Exception
    {
        public NoSongPlayingException() { }

        public NoSongPlayingException(string message)
            : base(message) { }

        public NoSongPlayingException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class LyricsNotFoundException : Exception
    {
        public LyricsNotFoundException() { }

        public LyricsNotFoundException(string message)
            : base(message) { }

        public LyricsNotFoundException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class ApiException : Exception
    {
        public ApiException() { }

        public ApiException(string message)
            : base(message) { }

        public ApiException(string message, Exception inner)
            : base(message, inner) { }
    }
}

using System.Text.Json.Serialization;

namespace CSharpSpotiLyrics.Console.App
{
    [JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(Config))]
    internal partial class CliJsonContext : JsonSerializerContext
    {
    }
}
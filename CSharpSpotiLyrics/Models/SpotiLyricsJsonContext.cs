using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CSharpSpotiLyrics.Core.Api;

namespace CSharpSpotiLyrics.Core.Models
{
#if NET8_0_OR_GREATER
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(GraphQLBody))]
    [JsonSerializable(typeof(GraphQLResponse<Data>))]
    [JsonSerializable(typeof(GraphQLResponse<MeData>))]
    [JsonSerializable(typeof(GraphQLResponse<PlaylistData>))]
    [JsonSerializable(typeof(GraphQLResponse<AlbumData>))]
    [JsonSerializable(typeof(MetadataTrackResponse))]
    [JsonSerializable(typeof(CurrentlyPlayingContext))]
    [JsonSerializable(typeof(LyricsResponse))]
    [JsonSerializable(typeof(TracksResponse))]
    [JsonSerializable(typeof(JsonElement))]
    [JsonSerializable(typeof(JsonObject))]
    [JsonSerializable(typeof(Dictionary<string, string>))]
    [JsonSerializable(typeof(SpotifyTotp.SecretVersionJSON))]
    public partial class SpotiLyricsJsonContext : JsonSerializerContext
    {
    }

    public static class JsonHelper
    {
        public static string Serialize<T>(T value, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo) =>
            JsonSerializer.Serialize(value, jsonTypeInfo);

        public static T? Deserialize<T>(string json, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo) =>
            JsonSerializer.Deserialize(json, jsonTypeInfo);

        public static ValueTask<T?> DeserializeAsync<T>(Stream stream, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo) =>
            JsonSerializer.DeserializeAsync(stream, jsonTypeInfo);

        public static Task<T?> ReadFromJsonAsync<T>(HttpContent content, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> jsonTypeInfo) =>
            content.ReadFromJsonAsync(jsonTypeInfo);
    }
#else
    // .NET Standard 2.0 için Type-safe JsonTypeInfo sarmalayıcısı (CS0411 hatasını önler)
    public class JsonTypeInfo<T>
    {
        public Type TargetType => typeof(T);
    }

    public class SpotiLyricsJsonContext
    {
        public static readonly SpotiLyricsJsonContext Default = new SpotiLyricsJsonContext();

        public JsonTypeInfo<GraphQLBody> GraphQLBody => new JsonTypeInfo<GraphQLBody>();
        public JsonTypeInfo<GraphQLResponse<Data>> GraphQLResponseData => new JsonTypeInfo<GraphQLResponse<Data>>();
        public JsonTypeInfo<GraphQLResponse<MeData>> GraphQLResponseMeData => new JsonTypeInfo<GraphQLResponse<MeData>>();
        public JsonTypeInfo<GraphQLResponse<PlaylistData>> GraphQLResponsePlaylistData => new JsonTypeInfo<GraphQLResponse<PlaylistData>>();
        public JsonTypeInfo<GraphQLResponse<AlbumData>> GraphQLResponseAlbumData => new JsonTypeInfo<GraphQLResponse<AlbumData>>();
        public JsonTypeInfo<MetadataTrackResponse> MetadataTrackResponse => new JsonTypeInfo<MetadataTrackResponse>();
        public JsonTypeInfo<CurrentlyPlayingContext> CurrentlyPlayingContext => new JsonTypeInfo<CurrentlyPlayingContext>();
        public JsonTypeInfo<LyricsResponse> LyricsResponse => new JsonTypeInfo<LyricsResponse>();
        public JsonTypeInfo<TracksResponse> TracksResponse => new JsonTypeInfo<TracksResponse>();
        public JsonTypeInfo<JsonElement> JsonElement => new JsonTypeInfo<JsonElement>();
        public JsonTypeInfo<JsonObject> JsonObject => new JsonTypeInfo<JsonObject>();
        public JsonTypeInfo<Dictionary<string, string>> DictionaryStringString => new JsonTypeInfo<Dictionary<string, string>>();
        public JsonTypeInfo<SpotifyTotp.SecretVersionJSON> SecretVersionJSON => new JsonTypeInfo<SpotifyTotp.SecretVersionJSON>();
    }

    public static class JsonHelper
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static string Serialize<T>(T value, JsonTypeInfo<T> jsonTypeInfo) =>
            JsonSerializer.Serialize(value, _options);

        public static T? Deserialize<T>(string json, JsonTypeInfo<T> jsonTypeInfo) =>
            JsonSerializer.Deserialize<T>(json, _options);

        public static ValueTask<T?> DeserializeAsync<T>(Stream stream, JsonTypeInfo<T> jsonTypeInfo) =>
            JsonSerializer.DeserializeAsync<T>(stream, _options);

        public static Task<T?> ReadFromJsonAsync<T>(HttpContent content, JsonTypeInfo<T> jsonTypeInfo) =>
            content.ReadFromJsonAsync<T>(_options);
    }
#endif
}
// Deliberately minimal stand-ins for UnityEngine APIs used by the game CORE.
// This checks actual EchoData/Narrative/Saves/Preferences C# on stock .NET 8.
// It is NOT a replacement for Unity's JsonUtility, engine, Editor, or Player QA.
using System;
using System.Text.Json;
namespace UnityEngine
{
    public static class Application
    {
        public static string streamingAssetsPath = string.Empty;
        public static string persistentDataPath = System.IO.Path.GetTempPath();
        public static string temporaryCachePath = System.IO.Path.GetTempPath();
    }
    public static class Debug
    {
        public static void LogWarning(object value) => Console.Error.WriteLine(value);
    }
    public static class Mathf
    {
        public static float Clamp(float value, float low, float high) => Math.Min(high, Math.Max(low, value));
    }
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Options = new JsonSerializerOptions {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
            AllowTrailingCommas = false
        };
        public static T FromJson<T>(string input) => JsonSerializer.Deserialize<T>(input, Options);
        public static string ToJson(object input, bool pretty = false) =>
            JsonSerializer.Serialize(input, new JsonSerializerOptions(Options) { WriteIndented = pretty });
    }
}

using System;
using System.IO;
using UnityEngine;

namespace Echo0317.Core
{
    [Serializable]
    public sealed class EchoPlayerOptions
    {
        public int version = 1;
        public bool captionsEnabled = true;
        public bool muted = false;
        public bool fullscreen = true;
        public float speed = 1f;
    }

    // Video and accessibility settings are separate from story checkpoints.
    // Starting a new story never resets the user's preferences.
    public sealed class EchoPreferences
    {
        readonly string file;
        public EchoPlayerOptions Current { get; private set; }

        public EchoPreferences(string directory)
        {
            Directory.CreateDirectory(directory);
            file = Path.Combine(directory, "player-options.json");
            Current = ReadOrDefault();
        }

        public static EchoPreferences OpenDefault() =>
            new EchoPreferences(Path.Combine(Application.persistentDataPath, "Settings"));

        static EchoPlayerOptions Sanitized(EchoPlayerOptions value)
        {
            if (value == null || value.version != 1) return new EchoPlayerOptions();
            if (float.IsNaN(value.speed) || float.IsInfinity(value.speed)) value.speed = 1f;
            value.speed = Mathf.Clamp(value.speed, 0.75f, 1.5f);
            return value;
        }

        EchoPlayerOptions Parse(string path)
        {
            var json = File.ReadAllText(path);
            if (json.Length > 16384 || string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException("设置文件长度无效");
            var value = JsonUtility.FromJson<EchoPlayerOptions>(json);
            if (value == null || value.version != 1)
                throw new InvalidDataException("不支持的设置版本");
            return Sanitized(value);
        }

        EchoPlayerOptions ReadOrDefault()
        {
            try
            {
                if (File.Exists(file)) return Parse(file);
                if (File.Exists(file + ".bak")) return Parse(file + ".bak");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[Echo0317] 设置读取失败，尝试备份或默认值: " + ex.Message);
                try { if (File.Exists(file + ".bak")) return Parse(file + ".bak"); }
                catch (Exception backupError) { Debug.LogWarning("[Echo0317] 设置备份读取失败: " + backupError.Message); }
            }
            return new EchoPlayerOptions();
        }

        public void Update(bool captionsEnabled, bool muted, bool fullscreen, float speed)
        {
            var next = Sanitized(new EchoPlayerOptions {
                captionsEnabled = captionsEnabled, muted = muted,
                fullscreen = fullscreen, speed = speed
            });
            var tmp = file + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(next, true), System.Text.Encoding.UTF8);
            if (File.Exists(file)) File.Replace(tmp, file, file + ".bak");
            else File.Move(tmp, file);
            Current = next;
        }
    }
}

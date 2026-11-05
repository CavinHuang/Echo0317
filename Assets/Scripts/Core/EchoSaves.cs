using System;
using System.IO;
using UnityEngine;

namespace Echo0317.Core
{
    public sealed class EchoSaves
    {
        readonly GameCatalog catalog;
        readonly string folder;
        public EchoSaves(GameCatalog catalog, string path)
        {
            this.catalog = catalog;
            folder = path;
            Directory.CreateDirectory(folder);
        }
        public string Folder => folder;
        string AutoPath => Path.Combine(folder, "autosave.json");
        string SlotPath(int number)
        {
            if (number < 1 || number > 3) throw new ArgumentOutOfRangeException(nameof(number));
            return Path.Combine(folder, "slot-" + number + ".json");
        }
        SaveEnvelope Envelope(GameProgress progress) => new SaveEnvelope {
            game = "0317-echo", format = 6, storyRevision = catalog.Story.revision,
            exportedAt = DateTime.UtcNow.ToString("o"), state = EchoNarrative.Normalize(catalog, progress)
        };
        static void WriteAtomic(string file, string content)
        {
            var temp = file + ".tmp";
            File.WriteAllText(temp, content, System.Text.Encoding.UTF8);
            // Preserve the previous generation as a recovery candidate. On Windows
            // File.Replace is an atomic same-volume replacement with a backup.
            if (File.Exists(file)) File.Replace(temp, file, file + ".bak");
            else File.Move(temp, file);
        }
        void SaveTo(string filename, GameProgress progress) => WriteAtomic(filename, JsonUtility.ToJson(Envelope(progress), true));
        public void AutoSave(GameProgress progress) => SaveTo(AutoPath, progress);
        public void SaveSlot(int number, GameProgress progress) => SaveTo(SlotPath(number), progress);
        public bool HasAuto => File.Exists(AutoPath) || File.Exists(AutoPath + ".bak");
        public bool HasSlot(int number) => File.Exists(SlotPath(number)) || File.Exists(SlotPath(number) + ".bak");
        public void DeleteSlot(int number)
        {
            var path = SlotPath(number);
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
        }
        public GameProgress LoadAuto() => LoadWithBackup(AutoPath, true);
        public GameProgress LoadSlot(int number) => LoadWithBackup(SlotPath(number), false);
        GameProgress LoadWithBackup(string filename, bool allowFresh)
        {
            if (!File.Exists(filename) && !File.Exists(filename + ".bak"))
            {
                if (allowFresh) return EchoNarrative.Fresh();
                throw new FileNotFoundException("未找到存档", filename);
            }
            try { return LoadFrom(filename); }
            catch (Exception primaryError) when (primaryError is IOException || primaryError is InvalidDataException || primaryError is InvalidOperationException)
            {
                if (!File.Exists(filename + ".bak")) throw;
                try
                {
                    var recovered = LoadFrom(filename + ".bak");
                    Debug.LogWarning("[Echo0317] 主存档损坏，已从上一版本恢复，原文件未删除: " + filename);
                    return recovered;
                }
                catch (Exception recoveryError)
                {
                    throw new InvalidDataException("主存档与备份都无法读取: " + filename,
                        new AggregateException(primaryError, recoveryError));
                }
            }
        }
        GameProgress LoadFrom(string filename)
        {
            if (!File.Exists(filename)) throw new FileNotFoundException("未找到存档", filename);
            return Parse(File.ReadAllText(filename));
        }
        public GameProgress Parse(string json)
        {
            if (string.IsNullOrEmpty(json) || json.Length > 1024 * 1024) throw new InvalidDataException("存档大小不合理");
            SaveEnvelope env;
            try { env = JsonUtility.FromJson<SaveEnvelope>(json); }
            catch (Exception ex) { throw new InvalidDataException("存档 JSON 无效", ex); }
            if (env == null || env.game != "0317-echo" || (env.format != 5 && env.format != 6))
                throw new InvalidDataException("不支持的存档格式");
            if (env.storyRevision != catalog.Story.revision)
                throw new InvalidDataException("剧情版本不匹配，不能安全恢复");
            return EchoNarrative.Normalize(catalog, env.state);
        }
        public GameProgress ImportOldStage8Save(string absolutePath)
        {
            // Stage 8 JSON exported via the archive, not browser-localStorage internals.
            return Parse(File.ReadAllText(absolutePath));
        }
        public string Export(GameProgress state)
        {
            string path = Path.Combine(folder, "export-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json");
            SaveTo(path, state);
            return path;
        }
        public static EchoSaves OpenDefault(GameCatalog g) => new EchoSaves(g, Path.Combine(Application.persistentDataPath, "Saves"));
    }
}

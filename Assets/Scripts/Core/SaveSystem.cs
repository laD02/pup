using System;
using System.IO;
using UnityEngine;

namespace SealCatapult.Core
{
    [Serializable]
    public class SaveData
    {
        public const int LevelCount = 18;    // Frost Bay: 10 levels + bonus + boss; Glacier River: 6 levels (old saves grow in Normalize)
        public const int UpgradeCount = 5;

        public int version = 1;
        public int[] stars = new int[LevelCount];     // best stars per level, 0..3
        public int[] upgrades = new int[UpgradeCount]; // level per UpgradeId, 0..3
        public int[] perfect = new int[LevelCount];   // 1 = every bear knocked out at least once
        public int spentStars;
        public const float DefaultMusic = 0.6f;
        public const float DefaultSfx = 0.9f;
        public float musicVolume = DefaultMusic;
        public float sfxVolume = DefaultSfx;
        public bool muted;
        public string language = "";                   // "" = follow system language
        // Statistics (GDD §R5): every bear ever knocked out, and pups that knocked out 2, 3, or 4+ bears in one dive.
        public int totalBears;
        public int combo2;
        public int combo3;
        public int combo4;
        public bool seenIntro;                         // the opening story played once already
        public bool seenHowTo;                         // unused since 15/11 (the first Play starts level 1); kept so old saves still load
        public bool reviewAsked;                       // Play took the one in-app review request (kept by Erase)

        /// <summary>Repairs data from older or hand-edited files so the game never indexes out of range.</summary>
        public void Normalize()
        {
            stars = Resize(stars, LevelCount, 0, 3);
            upgrades = Resize(upgrades, UpgradeCount, 0, 3);
            perfect = Resize(perfect, LevelCount, 0, 1);
            musicVolume = float.IsNaN(musicVolume) ? DefaultMusic : Mathf.Clamp01(musicVolume);   // Clamp01 keeps NaN
            sfxVolume = float.IsNaN(sfxVolume) ? DefaultSfx : Mathf.Clamp01(sfxVolume);
            int earned = 0;
            foreach (int s in stars) earned += s;
            spentStars = Mathf.Clamp(spentStars, 0, earned);   // the shop balance never goes negative
            totalBears = Mathf.Max(0, totalBears);
            combo2 = Mathf.Max(0, combo2);
            combo3 = Mathf.Max(0, combo3);
            combo4 = Mathf.Max(0, combo4);
            if (Array.IndexOf(Loc.Codes, language) < 0) language = "";
        }

        /// <summary>Records one pup's dive: how many bears it knocked out (combos of 2+ are counted by size).</summary>
        public void AddDive(int bears)
        {
            if (bears >= 4) combo4++;
            else if (bears == 3) combo3++;
            else if (bears == 2) combo2++;
        }

        public int PerfectCount()
        {
            int n = 0;
            foreach (int p in perfect) n += p;
            return n;
        }

        static int[] Resize(int[] source, int length, int min, int max)
        {
            var result = new int[length];
            if (source != null)
                for (int i = 0; i < Mathf.Min(length, source.Length); i++)
                    result[i] = Mathf.Clamp(source[i], min, max);
            return result;
        }
    }

    /// <summary>JSON save in Application.persistentDataPath/save.json (readable, hand-editable for debugging).</summary>
    public static class SaveSystem
    {
        public static SaveData Data { get; private set; } = new SaveData();

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "save.json");

        static bool loaded;

        public static void Load()
        {
            Data = LoadFrom(DefaultPath);
            loaded = true;
        }

        /// <summary>Lets any scene be started directly in the Editor without passing through Boot.</summary>
        public static void EnsureLoaded()
        {
            if (!loaded) Load();
        }

        public static bool Save() => SaveTo(DefaultPath, Data);

        public static void ResetProgress()
        {
            var keep = Data;
            Data = new SaveData
            {
                musicVolume = keep.musicVolume,
                sfxVolume = keep.sfxVolume,
                muted = keep.muted,
                language = keep.language,
                reviewAsked = keep.reviewAsked,
            };
            Save();
        }

        public static SaveData LoadFrom(string path)
        {
            // The .tmp is a complete save written just before the swap: use it when the swap never happened.
            var data = TryRead(path) ?? TryRead(path + ".tmp") ?? new SaveData();
            data.Normalize();
            return data;
        }

        /// <summary>Null when the file is missing or unreadable (a broken file is kept as a timestamped .corrupt copy).</summary>
        static SaveData TryRead(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                // Keep the broken file for inspection instead of silently overwriting it.
                Debug.LogWarning($"[SealCatapult] Save file unreadable ({path}). {e.Message}");
                TryCopy(path, $"{path}.{DateTime.UtcNow:yyyyMMddHHmmss}.corrupt");
                return null;
            }
        }

        /// <summary>Writes a temp file, then swaps it in atomically, so a kill mid-save never leaves no save at all.</summary>
        public static bool SaveTo(string path, SaveData data)
        {
            try
            {
                string temp = path + ".tmp";
                // Flushed to the disk before the swap: a rename can outlive a power cut that its data blocks did not,
                // which would leave an empty save.
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                if (File.Exists(path))
                {
                    try { File.Replace(temp, path, null); }
                    catch (Exception e) when (e is PlatformNotSupportedException || e is IOException)
                    {
                        // Some runtimes lack an atomic replace; a kill between these two lines is recovered from .tmp.
                        File.Delete(path);
                        File.Move(temp, path);
                    }
                }
                else File.Move(temp, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SealCatapult] Could not write save file at {path}: {e.Message}");
                return false;
            }
        }

        static void TryCopy(string from, string to)
        {
            try { File.Copy(from, to, true); }
            catch (Exception e) { Debug.LogWarning($"[SealCatapult] Could not back up corrupt save: {e.Message}"); }
        }
    }
}

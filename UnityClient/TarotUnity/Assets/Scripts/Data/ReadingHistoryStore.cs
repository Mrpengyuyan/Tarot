using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TarotUnity.Data
{
    [Serializable]
    public sealed class ReadingHistoryEntry
    {
        public string historyId;
        public string savedAtUtc;
        public ReadingSessionSnapshot snapshot;
    }

    [Serializable]
    internal sealed class ReadingHistoryEnvelope
    {
        public ReadingHistoryEntry[] entries = Array.Empty<ReadingHistoryEntry>();
    }

    /// <summary>Persistent local history of completed tarot readings stored as JSON.</summary>
    public static class ReadingHistoryStore
    {
        public const int MaxEntries = 50;

        private static string customStoragePath;
        private static List<ReadingHistoryEntry> cached;

        public static string StoragePath => !string.IsNullOrEmpty(customStoragePath)
            ? customStoragePath
            : Path.Combine(Application.persistentDataPath, "reading_history.json");

        public static void SetStoragePathForTesting(string path)
        {
            customStoragePath = path;
            cached = null;
        }

        public static void ResetStoragePathForTesting()
        {
            customStoragePath = null;
            cached = null;
        }

        public static IReadOnlyList<ReadingHistoryEntry> LoadAll()
        {
            EnsureLoaded();
            return cached;
        }

        public static ReadingHistoryEntry Record(ReadingSessionSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            EnsureLoaded();
            var clone = Clone(snapshot);
            if (clone == null)
            {
                return null;
            }

            ReadingHistoryEntry existing = null;
            if (clone.predictionId > 0)
            {
                for (var i = 0; i < cached.Count; i++)
                {
                    if (cached[i]?.snapshot != null && cached[i].snapshot.predictionId == clone.predictionId)
                    {
                        existing = cached[i];
                        cached.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                for (var i = 0; i < cached.Count; i++)
                {
                    var s = cached[i]?.snapshot;
                    if (s != null && s.predictionId == 0
                        && string.Equals(s.question, clone.question, StringComparison.Ordinal)
                        && string.Equals(s.summary, clone.summary, StringComparison.Ordinal)
                        && s.cardCount == clone.cardCount)
                    {
                        existing = cached[i];
                        cached.RemoveAt(i);
                        break;
                    }
                }
            }

            var entry = existing ?? new ReadingHistoryEntry
            {
                historyId = Guid.NewGuid().ToString("N"),
            };
            entry.savedAtUtc = DateTime.UtcNow.ToString("o");
            entry.snapshot = clone;
            cached.Insert(0, entry);

            if (cached.Count > MaxEntries)
            {
                cached.RemoveRange(MaxEntries, cached.Count - MaxEntries);
            }

            Persist();
            return entry;
        }

        public static bool Remove(string historyId)
        {
            if (string.IsNullOrEmpty(historyId))
            {
                return false;
            }

            EnsureLoaded();
            for (var i = 0; i < cached.Count; i++)
            {
                if (string.Equals(cached[i]?.historyId, historyId, StringComparison.Ordinal))
                {
                    cached.RemoveAt(i);
                    Persist();
                    return true;
                }
            }

            return false;
        }

        public static void Clear()
        {
            cached = new List<ReadingHistoryEntry>();
            Persist();
        }

        public static void Reload()
        {
            cached = null;
            EnsureLoaded();
        }

        private static void EnsureLoaded()
        {
            if (cached != null)
            {
                return;
            }

            cached = new List<ReadingHistoryEntry>();
            var path = StoragePath;
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var envelope = JsonUtility.FromJson<ReadingHistoryEnvelope>(json);
                if (envelope?.entries == null)
                {
                    return;
                }

                for (var i = 0; i < envelope.entries.Length && cached.Count < MaxEntries; i++)
                {
                    var entry = envelope.entries[i];
                    if (entry != null && entry.snapshot != null)
                    {
                        cached.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load reading history from {path}: {ex.Message}");
            }
        }

        private static void Persist()
        {
            var path = StoragePath;
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var envelope = new ReadingHistoryEnvelope
                {
                    entries = cached != null ? cached.ToArray() : Array.Empty<ReadingHistoryEntry>(),
                };
                File.WriteAllText(path, JsonUtility.ToJson(envelope, true));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save reading history to {path}: {ex.Message}");
            }
        }

        private static ReadingSessionSnapshot Clone(ReadingSessionSnapshot snapshot)
        {
            var json = JsonUtility.ToJson(snapshot);
            return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<ReadingSessionSnapshot>(json);
        }
    }
}

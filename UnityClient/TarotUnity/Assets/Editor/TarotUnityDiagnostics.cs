using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TarotUnity.Editor
{
    public static class TarotUnityDiagnostics
    {
        private const string DiagnosticsPath = "Temp/TarotUnityDiagnostics.txt";

        [MenuItem("Tools/Tarot Unity/Clear Console")]
        public static void ClearConsole()
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
            var clearMethod = logEntriesType?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            clearMethod?.Invoke(null, null);
        }

        [MenuItem("Tools/Tarot Unity/Write Diagnostics Snapshot")]
        public static void WriteDiagnosticsSnapshot()
        {
            var counts = GetConsoleCounts();

            Directory.CreateDirectory(Path.GetDirectoryName(DiagnosticsPath) ?? "Temp");
            File.WriteAllText(
                DiagnosticsPath,
                $"ConsoleErrors: {counts.errors}{Environment.NewLine}" +
                $"ConsoleWarnings: {counts.warnings}{Environment.NewLine}" +
                $"ConsoleLogs: {counts.logs}{Environment.NewLine}");

            Debug.Log($"Tarot Unity diagnostics snapshot written to {DiagnosticsPath}");
        }

        private static (int errors, int warnings, int logs) GetConsoleCounts()
        {
            var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
            var method = logEntriesType?.GetMethod(
                "GetCountsByType",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null)
            {
                return (0, 0, 0);
            }

            var parameters = new object[] { 0, 0, 0 };
            method.Invoke(null, parameters);
            return ((int)parameters[0], (int)parameters[1], (int)parameters[2]);
        }
    }
}

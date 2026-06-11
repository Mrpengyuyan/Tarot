using System;
using System.IO;
using UnityEditor;

namespace TarotUnity.Editor
{
    [InitializeOnLoad]
    public static class Phase1AutoBootstrapper
    {
        private const string MarkerPath = "ProjectSettings/TarotUnityPhase1Bootstrap.done";

        static Phase1AutoBootstrapper()
        {
            EditorApplication.delayCall += RunIfNeeded;
        }

        private static void RunIfNeeded()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += RunIfNeeded;
                return;
            }

            if (File.Exists(MarkerPath) || File.Exists("Assets/Scenes/Boot.unity"))
            {
                return;
            }

            Phase1AssetBootstrapper.Run();
            File.WriteAllText(MarkerPath, DateTime.UtcNow.ToString("O"));
            AssetDatabase.Refresh();
        }
    }
}


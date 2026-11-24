using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 48 was asked to hang a lamp, a window or a picture in the backdrop.
    /// Its diagnostic proved none of them can exist at this camera: pitched 27
    /// degrees down, the highest world Y visible at the backdrop's depth is 0.34
    /// - barely above the table. There is no wall in this shot, and only the
    /// bottom ~29% of the backdrop is ever on screen. That finding is the phase's
    /// real output, and it is why Phase48BackdropDiagnostic is kept.
    ///
    /// The attempted answer - moonlight through an unseen window, thrown as a
    /// mullioned spot cookie across the far cloth - is NOT shipped. An A/B
    /// capture proved the light rendered but contributed about 3% brightness at
    /// its own focus, and its response to intensity stopped being explicable
    /// (26 -> visible lift, 170 -> none). Rather than ship an effect that cannot
    /// be accounted for, it is removed. Anything staged in that band has to be
    /// measured against the diagnostic first.
    ///
    /// What does ship: the backdrop regenerated with its drapery folds inside the
    /// visible band. Phase 45 faded them out by 72% down from the texture top,
    /// which lands at world Y 0.12 - above the 0.34 ceiling - so the drapery it
    /// claimed to add was never on screen at all.
    /// </summary>
    public static class Phase48MoonlightBootstrapper
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        private const string SpriteFolder = Phase37AssetFoundationBootstrapper.SpriteFolder;
        public const string MoonlightName = "MP_Moonlight";

        [MenuItem("Tools/Tarot Unity/Run Phase 48 Moonlight Bootstrap")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            ConfigureTextures();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RemoveMoonlight();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Tarot Unity Phase 48 moonlight complete.");
        }

        private static void ConfigureTextures()
        {
            var cookiePath = $"{SpriteFolder}/MoonCookie.png";
            if (AssetImporter.GetAtPath(cookiePath) is TextureImporter cookie)
            {
                cookie.textureType = TextureImporterType.Default;
                cookie.sRGBTexture = false;
                cookie.mipmapEnabled = true;
                cookie.maxTextureSize = 512;
                // Clamp, or the cookie tiles the window across the whole table.
                cookie.wrapMode = TextureWrapMode.Clamp;
                cookie.SaveAndReimport();
            }
            else
            {
                Debug.LogError($"Phase 48: missing {cookiePath}; run gen_moonlight.py first.");
            }

            // The backdrop was regenerated with its folds in the visible band.
            var backdropPath = $"{SpriteFolder}/ParlorBackdrop.png";
            if (AssetImporter.GetAtPath(backdropPath) is TextureImporter backdrop)
            {
                backdrop.SaveAndReimport();
            }
        }

        /// <summary>
        /// Strips the moonlight rig if an earlier run of this bootstrap staged it.
        /// </summary>
        private static void RemoveMoonlight()
        {
            var moon = GameObject.Find("MP_MenuStage")?.transform.Find(MoonlightName);
            if (moon != null)
            {
                Object.DestroyImmediate(moon.gameObject);
                Debug.Log("Phase 48: removed the unverifiable moonlight rig.");
            }
        }
    }
}

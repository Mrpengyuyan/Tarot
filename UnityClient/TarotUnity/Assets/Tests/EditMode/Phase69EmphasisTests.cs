using NUnit.Framework;
using TarotUnity.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Tests.EditMode
{
    /// <summary>Phase 69: only the current step and the chosen spread wear card stock.</summary>
    public sealed class Phase69EmphasisTests
    {
        private GameObject root;

        [TearDown]
        public void Clean()
        {
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void OnlyTheCurrentStepIsEmphasized()
        {
            root = new GameObject("Hud");
            var indicator = root.AddComponent<RitualStepIndicator>();
            var skins = new UiSkinState[5];
            var so = new SerializedObject(indicator);
            var chips = so.FindProperty("chips");
            chips.arraySize = skins.Length;
            for (var i = 0; i < skins.Length; i++)
            {
                var chip = new GameObject($"Chip{i}", typeof(RectTransform));
                chip.transform.SetParent(root.transform);
                var plate = new GameObject("Plate", typeof(RectTransform)).AddComponent<Image>();
                plate.transform.SetParent(chip.transform);
                var label = new GameObject("Label", typeof(RectTransform)).AddComponent<TMPro.TextMeshProUGUI>();
                label.transform.SetParent(chip.transform);
                skins[i] = chip.AddComponent<UiSkinState>();
                skins[i].Configure(plate, label, null, Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), false);
                var element = chips.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("root").objectReferenceValue = chip.transform;
                element.FindPropertyRelative("plate").objectReferenceValue = plate;
                element.FindPropertyRelative("label").objectReferenceValue = label;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            for (var step = 0; step < skins.Length; step++)
            {
                indicator.SetStep(step);
                for (var i = 0; i < skins.Length; i++)
                {
                    Assert.That(skins[i].IsEmphasized, Is.EqualTo(i == step), $"step {step}, chip {i}");
                }
            }
        }

        [Test]
        public void OnlyTheChosenSpreadButtonIsEmphasized()
        {
            root = new GameObject("Room");
            var controller = root.AddComponent<ReadingRoomController>();
            var so = new SerializedObject(controller);
            var skins = new System.Collections.Generic.Dictionary<int, UiSkinState>();
            foreach (var (field, count) in new[] { ("oneCardButton", 1), ("threeCardButton", 3), ("celticCrossButton", 10) })
            {
                var go = new GameObject(field, typeof(RectTransform));
                go.transform.SetParent(root.transform);
                var image = go.AddComponent<Image>();
                var button = go.AddComponent<Button>();
                var skin = go.AddComponent<UiSkinState>();
                skin.Configure(image, null, Sprite.Create(Texture2D.blackTexture, new Rect(0, 0, 4, 4), Vector2.zero),
                    Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero), true);
                so.FindProperty(field).objectReferenceValue = button;
                skins[count] = skin;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var chosen in new[] { 1, 3, 10, 3 })
            {
                controller.ApplySpreadEmphasis(chosen);
                foreach (var pair in skins)
                {
                    Assert.That(pair.Value.IsEmphasized, Is.EqualTo(pair.Key == chosen), $"chosen {chosen}, button {pair.Key}");
                }
            }
        }
    }
}

using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TarotUnity.Editor
{
    /// <summary>
    /// Phase 60: the Result screen used to show only the first drawn card
    /// (ResultPanelPresenter rendered draws[0] into one hero slot), so a
    /// three-card spread displayed a single card under a header that already
    /// promised "三牌阵·过去·现在·建议". This bootstrapper builds a top band of up
    /// to three framed card cells (past → present → advice, left to right) that the
    /// presenter fills for a multi-card reading, and wires the presenter's new
    /// references. A one-card reading is untouched: it keeps the original left-third
    /// hero and right reading column.
    /// </summary>
    public static class Phase60ResultSpreadBandBootstrapper
    {
        private const string ResultScenePath = "Assets/Scenes/Result.unity";
        private const string PanelSpritePath = "Assets/Art/MidnightParlor/Sprites/TarotPanel.png";
        private const string GlowSpritePath = "Assets/Art/MidnightParlor/Sprites/TarotGlow.png";
        private const string FoilMaterialPath = "Assets/Materials/MAT_HolographicHeroCardUI.mat";

        private const string BandName = "MP_ResultSpreadBand";
        private const int CellCount = 3;

        // Layout (canvas ref 1280x720, centre-anchored). The three cells sit in a
        // row under the header; the reading scroll moves full-width below them.
        private static readonly float[] CellX = { -348f, 0f, 348f };
        private const float CellY = 88f;
        private static readonly Vector2 CellSize = new Vector2(164f, 268f);
        private static readonly Vector2 FrameSize = new Vector2(164f, 234f);
        private static readonly Vector2 ArtworkSize = new Vector2(142f, 214f);
        private static readonly Vector2 GlowSize = new Vector2(258f, 326f);
        private const float PivotY = 18f;   // frame/art centre inside the cell (leaves room for the label)
        private const float LabelY = -118f; // label under the frame

        private static readonly Vector2 SingleReadingPos = new Vector2(178f, 4f);
        private static readonly Vector2 SingleReadingSize = new Vector2(736f, 448f);
        private static readonly Vector2 SpreadReadingPos = new Vector2(0f, -160f);
        private static readonly Vector2 SpreadReadingSize = new Vector2(1128f, 208f);

        private static readonly Color GoldLabel = new Color(0.87f, 0.72f, 0.40f, 1f);
        private static readonly Color GlowWarm = new Color(1f, 0.72f, 0.34f, 0.22f);

        [MenuItem("Tools/Tarot Unity/Run Phase 60 Result Spread Band Bootstrap")]
        public static void Run()
        {
            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);

            var canvas = GameObject.Find("ResultCanvas");
            if (canvas == null)
            {
                Debug.LogError("Phase 60: ResultCanvas not found.");
                return;
            }

            var presenter = Object.FindObjectOfType<ResultPanelPresenter>();
            if (presenter == null)
            {
                Debug.LogError("Phase 60: ResultPanelPresenter not found.");
                return;
            }

            var showcase = GameObject.Find("Phase12_ResultCardShowcase");
            var heroArtworkSlot = GameObject.Find("Phase12_ResultCardArtworkSlot");
            var heroPlaceholder = GameObject.Find("Phase12_ResultCardPlaceholder");
            var scroll = GameObject.Find("ResultReadingScroll");
            var refTmp = GameObject.Find("SpreadNameText")?.GetComponent<TMP_Text>();

            var panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PanelSpritePath);
            var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GlowSpritePath);
            var foil = AssetDatabase.LoadAssetAtPath<Material>(FoilMaterialPath);

            // Idempotent: rebuild the band from scratch each run.
            var existing = FindChild(canvas.transform, BandName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var band = NewUi(BandName, canvas.transform, Vector2.zero, Vector2.zero);
            Stretch(band);

            var cellRoots = new GameObject[CellCount];
            var pivots = new RectTransform[CellCount];
            var artworks = new Image[CellCount];
            var labels = new TMP_Text[CellCount];

            for (var i = 0; i < CellCount; i++)
            {
                var cell = NewUi($"SpreadCell_{i}", band, new Vector2(CellX[i], CellY), CellSize);
                cellRoots[i] = cell.gameObject;

                // Warm candle glow behind the card (matches the hero showcase).
                if (glowSprite != null)
                {
                    var glow = NewUi("Glow", cell, new Vector2(0f, PivotY), GlowSize);
                    var glowImg = glow.gameObject.AddComponent<Image>();
                    glowImg.sprite = glowSprite;
                    glowImg.color = GlowWarm;
                    glowImg.raycastTarget = false;
                }

                // Reversible pivot: a reversed card turns 180deg here, independent of
                // the foil driver's own rect rotation on the artwork.
                var pivot = NewUi("ReversePivot", cell, new Vector2(0f, PivotY), FrameSize);
                pivots[i] = pivot;

                if (panelSprite != null)
                {
                    var frame = NewUi("Frame", pivot, Vector2.zero, FrameSize);
                    var frameImg = frame.gameObject.AddComponent<Image>();
                    frameImg.sprite = panelSprite;
                    frameImg.type = Image.Type.Sliced;
                    frameImg.color = Color.white;
                    frameImg.raycastTarget = false;
                }

                var art = NewUi("Artwork", pivot, Vector2.zero, ArtworkSize);
                var artImg = art.gameObject.AddComponent<Image>();
                artImg.preserveAspect = true;
                artImg.raycastTarget = true; // so the foil driver can hear hovers at runtime
                artworks[i] = artImg;

                // Phase 53 holographic foil, one driver per card. It swaps in a
                // material instance at runtime (Awake); at author time the art shows
                // plain, which is what static captures should render.
                var holo = art.gameObject.AddComponent<HolographicHeroCard>();
                WireHolographic(holo, artImg, foil);

                var label = NewUi("Label", cell, new Vector2(0f, LabelY), new Vector2(180f, 36f));
                var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = i == 0 ? "过去" : i == 1 ? "现在" : "建议";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 22f;
                tmp.color = GoldLabel;
                tmp.raycastTarget = false;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Overflow;
                if (refTmp != null)
                {
                    tmp.font = refTmp.font;
                    if (refTmp.fontSharedMaterial != null)
                    {
                        tmp.fontSharedMaterial = refTmp.fontSharedMaterial;
                    }
                }
                labels[i] = tmp;
            }

            band.gameObject.SetActive(false); // presenter enables it for a multi-card reading

            var singleModeRoots = new[] { showcase, heroArtworkSlot, heroPlaceholder };
            WirePresenter(presenter, singleModeRoots, band.gameObject, scroll, cellRoots, pivots, artworks, labels);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Phase 60: Result spread band built and presenter wired.");
        }

        private static void WirePresenter(
            ResultPanelPresenter presenter, GameObject[] singleModeRoots, GameObject band, GameObject scroll,
            GameObject[] cellRoots, RectTransform[] pivots, Image[] artworks, TMP_Text[] labels)
        {
            var so = new SerializedObject(presenter);
            var sm = so.FindProperty("singleModeRoots");
            sm.arraySize = singleModeRoots.Length;
            for (var i = 0; i < singleModeRoots.Length; i++)
            {
                sm.GetArrayElementAtIndex(i).objectReferenceValue = singleModeRoots[i];
            }
            SetRef(so, "spreadBandRoot", band);
            SetRef(so, "readingScrollRect", scroll != null ? scroll.GetComponent<RectTransform>() : null);
            so.FindProperty("singleReadingPos").vector2Value = SingleReadingPos;
            so.FindProperty("singleReadingSize").vector2Value = SingleReadingSize;
            so.FindProperty("spreadReadingPos").vector2Value = SpreadReadingPos;
            so.FindProperty("spreadReadingSize").vector2Value = SpreadReadingSize;

            var arr = so.FindProperty("spreadCards");
            arr.arraySize = CellCount;
            for (var i = 0; i < CellCount; i++)
            {
                var el = arr.GetArrayElementAtIndex(i);
                el.FindPropertyRelative("root").objectReferenceValue = cellRoots[i];
                el.FindPropertyRelative("reversePivot").objectReferenceValue = pivots[i];
                el.FindPropertyRelative("artwork").objectReferenceValue = artworks[i];
                el.FindPropertyRelative("label").objectReferenceValue = labels[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHolographic(HolographicHeroCard holo, Image image, Material foil)
        {
            var so = new SerializedObject(holo);
            SetRef(so, "heroImage", image);
            if (foil != null)
            {
                SetRef(so, "holographicMaterial", foil);
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRef(SerializedObject so, string prop, Object value)
        {
            var p = so.FindProperty(prop);
            if (p != null)
            {
                p.objectReferenceValue = value;
            }
        }

        private static RectTransform NewUi(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            return rt;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    return child;
                }
            }
            return null;
        }
    }
}

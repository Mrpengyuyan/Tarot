using System.Collections.Generic;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace TarotUnity.Presentation
{
    /// <summary>
    /// Renders real 3D floating tarot card formations above each spread option
    /// (Single, Three-Card arc, and full 10-card miniature Celtic Cross) on the
    /// RitualPaper camera layer using RitualDeckInk and RitualDeckShadow.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1320)]
    public sealed class RitualSpreadPreview3D : MonoBehaviour
    {
        private static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        private static readonly int BorderColorId = Shader.PropertyToID("_BorderColor");
        private static readonly int BorderSizeId = Shader.PropertyToID("_BorderSize");
        private static readonly int PaperId = Shader.PropertyToID("_Paper");
        private static readonly int ShadowOpacityId = Shader.PropertyToID("_ShadowOpacity");

        private static readonly Color RestBorderColor = new(0.76f, 0.61f, 0.33f, 1f);
        private static readonly Color HoverBorderColor = new(0.98f, 0.87f, 0.61f, 1f);

        private const float CardAspect = 1.70f;
        private const float ProjectionDepth = 2.45f;
        private const float BorderX = 0.018f;
        private const float BorderY = 0.0105f;

        private struct CardSlotDef
        {
            public Vector3 Position; // normalized to card height (1.0)
            public Vector3 Euler;    // local pitch, yaw, roll in degrees
            public float Phase;
            public float HoverExpand;
            public float HoverExtraLift;
            public float HoverExtraForward;
            public int SortingOffset;
        }

        private sealed class CardNode
        {
            public Transform Transform;
            public MeshRenderer Renderer;
            public CardSlotDef Def;
        }

        private sealed class Formation
        {
            public int SpreadIndex;
            public Button Button;
            public RectTransform SymbolRect;
            public CanvasGroup ButtonGroup;
            public RitualControlFeedback Feedback;
            public Transform Root;
            public MeshRenderer ShadowRenderer;
            public Mesh ShadowMesh;
            public readonly List<CardNode> Cards = new();
            public float CardHeightRatio;
            public float BasePitch;
            public float HoverPitch;
            public float ShadowWidth;
            public float ShadowHeight;
            public float PhaseOffset;
        }

        private RitualReadingView view;
        private Camera viewCamera;
        private Canvas uiCanvas;
        private Transform stageRoot;
        private Material cardMaterial;
        private Material shadowMaterial;
        private Mesh cardMesh;
        private MaterialPropertyBlock cardBlock;
        private MaterialPropertyBlock shadowBlock;
        private readonly List<Formation> formations = new();
        private bool visible;

        public bool IsReady => isActiveAndEnabled && stageRoot != null && formations.Count == 3;
        public int TotalCardCount
        {
            get
            {
                var total = 0;
                for (var i = 0; i < formations.Count; i++) total += formations[i].Cards.Count;
                return total;
            }
        }

        public int CardCountForSpread(int index) =>
            index >= 0 && index < formations.Count ? formations[index].Cards.Count : 0;

        public Vector3 GetFormationWorldPosition(int index) =>
            index >= 0 && index < formations.Count && formations[index].Root != null
                ? formations[index].Root.position
                : Vector3.zero;

        public bool AreCardsVisible => visible;

        internal IEnumerable<Renderer> PaperRenderers
        {
            get
            {
                for (var i = 0; i < formations.Count; i++)
                {
                    var f = formations[i];
                    if (f.ShadowRenderer != null) yield return f.ShadowRenderer;
                    for (var c = 0; c < f.Cards.Count; c++)
                    {
                        if (f.Cards[c].Renderer != null) yield return f.Cards[c].Renderer;
                    }
                }
            }
        }

        public static RitualSpreadPreview3D Attach(GameObject host, RitualReadingView view, Camera camera)
        {
            if (host == null || view == null || camera == null) return null;
            var existing = host.GetComponent<RitualSpreadPreview3D>();
            if (existing != null) return existing;
            var owner = host.AddComponent<RitualSpreadPreview3D>();
            owner.view = view;
            owner.viewCamera = camera;
            owner.Build();
            return owner;
        }

        private void Build()
        {
            var inkSource = Resources.Load<Material>("Presentation/RitualDeckInk");
            var shadowSource = Resources.Load<Material>("Presentation/RitualDeckShadow");
            if (inkSource == null || view == null || view.SpreadButtons == null || view.SpreadButtons.Length < 3)
            {
                return;
            }

            cardMaterial = new Material(inkSource) { hideFlags = HideFlags.HideAndDontSave };
            if (shadowSource != null)
            {
                shadowMaterial = new Material(shadowSource) { hideFlags = HideFlags.HideAndDontSave };
            }

            cardMesh = BuildCardMesh();
            cardBlock = new MaterialPropertyBlock();
            shadowBlock = new MaterialPropertyBlock();

            uiCanvas = view.GetComponentInParent<Canvas>();
            stageRoot = new GameObject("RitualSpreadPreview3DStage").transform;
            stageRoot.SetParent(transform, false);

            var buttons = view.SpreadButtons;
            CreateFormation(0, buttons[0], SingleCardDefs(), 1.48f, 24f, 8f, 130f, 190f, 0f);
            CreateFormation(1, buttons[1], ThreeCardDefs(), 1.32f, 26f, 10f, 230f, 190f, 1.35f);
            CreateFormation(2, buttons[2], CelticCrossDefs(), 0.56f, 30f, 12f, 260f, 215f, 2.7f);

            view.Set2DSpreadSymbolsHidden(true);
            RefreshVisuals();
        }

        private void CreateFormation(
            int index,
            Button button,
            CardSlotDef[] defs,
            float cardHeightRatio,
            float basePitch,
            float hoverPitch,
            float shadowWidth,
            float shadowHeight,
            float phaseOffset)
        {
            if (button == null) return;
            var symbol = button.transform.Find("RitualSpreadSymbol") as RectTransform;
            if (symbol == null) return;

            var root = new GameObject($"SpreadFormation3D_{index}").transform;
            root.SetParent(stageRoot, false);

            var formation = new Formation
            {
                SpreadIndex = index,
                Button = button,
                SymbolRect = symbol,
                ButtonGroup = button.GetComponent<CanvasGroup>(),
                Feedback = button.GetComponent<RitualControlFeedback>(),
                Root = root,
                CardHeightRatio = cardHeightRatio,
                BasePitch = basePitch,
                HoverPitch = hoverPitch,
                ShadowWidth = shadowWidth,
                ShadowHeight = shadowHeight,
                PhaseOffset = phaseOffset,
            };

            if (shadowMaterial != null)
            {
                var shadowGo = new GameObject("Shadow", typeof(MeshFilter), typeof(MeshRenderer));
                shadowGo.transform.SetParent(root, false);
                formation.ShadowMesh = BuildShadowMesh(shadowWidth, shadowHeight);
                shadowGo.GetComponent<MeshFilter>().sharedMesh = formation.ShadowMesh;
                var renderer = shadowGo.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = shadowMaterial;
                renderer.sortingOrder = -180;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                RitualPaperRendering.Register(renderer);
                formation.ShadowRenderer = renderer;
            }

            for (var i = 0; i < defs.Length; i++)
            {
                var go = new GameObject($"Card_{i:00}", typeof(MeshFilter), typeof(MeshRenderer));
                go.transform.SetParent(root, false);
                go.GetComponent<MeshFilter>().sharedMesh = cardMesh;
                var renderer = go.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = cardMaterial;
                renderer.sortingOrder = -90 + defs[i].SortingOffset;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                RitualPaperRendering.Register(renderer);
                formation.Cards.Add(new CardNode
                {
                    Transform = go.transform,
                    Renderer = renderer,
                    Def = defs[i],
                });
            }

            formations.Add(formation);
        }

        private void LateUpdate() => RefreshVisuals();

        public void RefreshVisuals()
        {
            if (!IsReady || viewCamera == null || viewCamera.pixelWidth <= 2 || viewCamera.pixelHeight <= 2)
            {
                return;
            }

            var showStage = view.Stage == RitualReadingView.ViewStage.Spread && view.isActiveAndEnabled;
            visible = showStage;
            if (stageRoot.gameObject.activeSelf != showStage)
            {
                stageRoot.gameObject.SetActive(showStage);
            }

            if (!showStage)
            {
                return;
            }

            var camTransform = viewCamera.transform;
            var camPos = camTransform.position;
            var camRot = camTransform.rotation;
            var pixelUnit = 2f * Mathf.Tan(viewCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * ProjectionDepth / viewCamera.pixelHeight;
            var overlayCamera = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? uiCanvas.worldCamera : null;
            var time = Time.unscaledTime;

            for (var f = 0; f < formations.Count; f++)
            {
                var formation = formations[f];
                if (formation.SymbolRect == null || formation.Button == null) continue;

                if (formation.Feedback == null)
                {
                    formation.Feedback = formation.Button.GetComponent<RitualControlFeedback>();
                }
                if (formation.ButtonGroup == null)
                {
                    formation.ButtonGroup = formation.Button.GetComponent<CanvasGroup>();
                }

                var groupAlpha = formation.ButtonGroup != null ? formation.ButtonGroup.alpha : 1f;
                var active = formation.Button.gameObject.activeInHierarchy && groupAlpha > 0.002f;
                if (formation.Root.gameObject.activeSelf != active)
                {
                    formation.Root.gameObject.SetActive(active);
                }
                if (!active) continue;

                var hover = formation.Feedback != null ? Mathf.Clamp01(formation.Feedback.HoverAmount) : 0f;

                var worldCenter = formation.SymbolRect.TransformPoint(formation.SymbolRect.rect.center);
                var worldTop = formation.SymbolRect.TransformPoint(new Vector3(formation.SymbolRect.rect.center.x, formation.SymbolRect.rect.yMax, 0f));
                var worldBottom = formation.SymbolRect.TransformPoint(new Vector3(formation.SymbolRect.rect.center.x, formation.SymbolRect.rect.yMin, 0f));

                var screenCenter = RectTransformUtility.WorldToScreenPoint(overlayCamera, worldCenter);
                var screenTop = RectTransformUtility.WorldToScreenPoint(overlayCamera, worldTop);
                var screenBottom = RectTransformUtility.WorldToScreenPoint(overlayCamera, worldBottom);
                var slotPixelHeight = Mathf.Max(24f, Mathf.Abs(screenTop.y - screenBottom.y));
                var slotWorldHeight = slotPixelHeight * pixelUnit;

                var cardHeight = slotWorldHeight * formation.CardHeightRatio;
                var baseElevationY = slotWorldHeight * 0.16f;
                var levitation = Mathf.Sin(time * 1.35f + formation.PhaseOffset) * (slotWorldHeight * 0.055f);
                var hoverLiftY = hover * slotWorldHeight * 0.11f;
                var hoverForwardZ = hover * 0.13f;

                var depth = ProjectionDepth - hoverForwardZ;
                var depthRatio = depth / ProjectionDepth;
                var localX = (screenCenter.x - viewCamera.pixelWidth * 0.5f) * pixelUnit * depthRatio;
                var localY = (screenCenter.y - viewCamera.pixelHeight * 0.5f) * pixelUnit * depthRatio + baseElevationY + levitation + hoverLiftY;
                formation.Root.position = camPos + camRot * new Vector3(localX, localY, depth);

                var breathPitch = Mathf.Sin(time * 1.35f + formation.PhaseOffset - 0.6f) * Mathf.Lerp(2.4f, 1.0f, hover);
                var pitch = Mathf.Lerp(formation.BasePitch, formation.HoverPitch, hover) + breathPitch;
                var swayYaw = Mathf.Sin(time * 0.65f + formation.PhaseOffset) * Mathf.Lerp(2.5f, 1.2f, hover);
                var swayRoll = Mathf.Cos(time * 0.55f + formation.PhaseOffset) * Mathf.Lerp(1.1f, 0.5f, hover);
                formation.Root.rotation = camRot * Quaternion.Euler(pitch, swayYaw, swayRoll);

                var formationScale = Mathf.Lerp(1f, 1.075f, hover);
                formation.Root.localScale = Vector3.one * formationScale;

                var borderColor = Color.Lerp(RestBorderColor, HoverBorderColor, hover);
                cardBlock.SetFloat(OpacityId, groupAlpha);
                cardBlock.SetColor(BorderColorId, borderColor);
                cardBlock.SetVector(BorderSizeId, new Vector4(BorderX, BorderY, 0f, 0f));

                for (var c = 0; c < formation.Cards.Count; c++)
                {
                    var node = formation.Cards[c];
                    var def = node.Def;
                    var wave = Mathf.Sin(time * 1.55f + formation.PhaseOffset + def.Phase) * (cardHeight * 0.032f);
                    var expand = 1f + hover * def.HoverExpand;
                    var localPos = new Vector3(
                        def.Position.x * cardHeight * expand,
                        def.Position.y * cardHeight * expand + wave + hover * def.HoverExtraLift * cardHeight,
                        def.Position.z * cardHeight - hover * def.HoverExtraForward * cardHeight);

                    var euler = def.Euler;
                    euler.y *= Mathf.Lerp(1f, 0.72f, hover);
                    euler.z *= 1f + hover * 0.14f;

                    node.Transform.localPosition = localPos;
                    node.Transform.localRotation = Quaternion.Euler(euler);
                    node.Transform.localScale = Vector3.one * cardHeight;
                    node.Renderer.SetPropertyBlock(cardBlock);
                }

                if (formation.ShadowRenderer != null)
                {
                    var levitationNorm = Mathf.InverseLerp(-slotWorldHeight * 0.055f, slotWorldHeight * 0.055f, levitation);
                    var shadowScale = slotWorldHeight * (1.28f + levitationNorm * 0.08f + hover * 0.18f);
                    var shadowOffsetY = -slotWorldHeight * (0.36f + hover * 0.10f) - levitation * 1.15f;
                    var shadowDepthZ = 0.24f + hover * 0.10f;
                    formation.ShadowRenderer.transform.localPosition = new Vector3(0f, shadowOffsetY, shadowDepthZ);
                    formation.ShadowRenderer.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
                    formation.ShadowRenderer.transform.localScale = new Vector3(
                        shadowScale * (formation.ShadowWidth / 150f),
                        shadowScale * (formation.ShadowHeight / 150f),
                        1f);

                    shadowBlock.SetVector(PaperId, new Vector4(formation.ShadowWidth, formation.ShadowHeight, 1f, 1f));
                    shadowBlock.SetFloat(ShadowOpacityId, Mathf.Lerp(0.62f, 0.32f, hover) * Mathf.Lerp(1f, 0.84f, levitationNorm) * groupAlpha);
                    formation.ShadowRenderer.SetPropertyBlock(shadowBlock);
                }
            }
        }

        private static CardSlotDef[] SingleCardDefs()
        {
            return new[]
            {
                new CardSlotDef
                {
                    Position = Vector3.zero,
                    Euler = Vector3.zero,
                    Phase = 0f,
                    HoverExpand = 0f,
                    HoverExtraLift = 0.04f,
                    HoverExtraForward = 0.03f,
                    SortingOffset = 0,
                },
            };
        }

        private static CardSlotDef[] ThreeCardDefs()
        {
            return new[]
            {
                new CardSlotDef
                {
                    Position = new Vector3(-0.56f, -0.05f, 0.07f),
                    Euler = new Vector3(0f, 18f, 14f),
                    Phase = 0f,
                    HoverExpand = 0.24f,
                    HoverExtraLift = 0.02f,
                    HoverExtraForward = 0.01f,
                    SortingOffset = 0,
                },
                new CardSlotDef
                {
                    Position = new Vector3(0f, 0.04f, -0.03f),
                    Euler = Vector3.zero,
                    Phase = 0.85f,
                    HoverExpand = 0.10f,
                    HoverExtraLift = 0.09f,
                    HoverExtraForward = 0.05f,
                    SortingOffset = 2,
                },
                new CardSlotDef
                {
                    Position = new Vector3(0.56f, -0.05f, 0.07f),
                    Euler = new Vector3(0f, -18f, -14f),
                    Phase = 1.7f,
                    HoverExpand = 0.24f,
                    HoverExtraLift = 0.02f,
                    HoverExtraForward = 0.01f,
                    SortingOffset = 1,
                },
            };
        }

        private static CardSlotDef[] CelticCrossDefs()
        {
            const float cx = -0.28f;
            const float sx = 1.16f;
            return new[]
            {
                // Cross core & floating horizontal challenge card
                new CardSlotDef { Position = new Vector3(cx, 0f, 0.03f), Euler = Vector3.zero, Phase = 0.0f, HoverExpand = 0.08f, HoverExtraLift = 0.03f, HoverExtraForward = 0.02f, SortingOffset = 2 },
                new CardSlotDef { Position = new Vector3(cx, 0f, -0.08f), Euler = new Vector3(0f, 0f, -90f), Phase = 0.35f, HoverExpand = 0.08f, HoverExtraLift = 0.05f, HoverExtraForward = 0.11f, SortingOffset = 6 },
                // Four cardinal cards around the cross
                new CardSlotDef { Position = new Vector3(cx, 1.08f, 0.01f), Euler = new Vector3(4f, 0f, 0f), Phase = 0.7f, HoverExpand = 0.14f, HoverExtraLift = 0.04f, HoverExtraForward = 0.02f, SortingOffset = 3 },
                new CardSlotDef { Position = new Vector3(cx, -1.08f, 0.04f), Euler = new Vector3(-4f, 0f, 0f), Phase = 1.05f, HoverExpand = 0.14f, HoverExtraLift = 0.01f, HoverExtraForward = 0.01f, SortingOffset = 1 },
                new CardSlotDef { Position = new Vector3(cx - 0.88f, 0f, 0.05f), Euler = new Vector3(0f, 12f, 4f), Phase = 1.4f, HoverExpand = 0.15f, HoverExtraLift = 0.02f, HoverExtraForward = 0.01f, SortingOffset = 1 },
                new CardSlotDef { Position = new Vector3(cx + 0.88f, 0f, 0.05f), Euler = new Vector3(0f, -10f, -4f), Phase = 1.75f, HoverExpand = 0.14f, HoverExtraLift = 0.02f, HoverExtraForward = 0.01f, SortingOffset = 1 },
                // Four-card ascending staff on the right
                new CardSlotDef { Position = new Vector3(sx, -1.08f, 0.06f), Euler = new Vector3(0f, -14f, -2f), Phase = 2.0f, HoverExpand = 0.13f, HoverExtraLift = 0.00f, HoverExtraForward = 0.01f, SortingOffset = 0 },
                new CardSlotDef { Position = new Vector3(sx, -0.36f, 0.03f), Euler = new Vector3(0f, -12f, -1f), Phase = 2.3f, HoverExpand = 0.13f, HoverExtraLift = 0.02f, HoverExtraForward = 0.02f, SortingOffset = 2 },
                new CardSlotDef { Position = new Vector3(sx, 0.36f, 0.00f), Euler = new Vector3(0f, -10f, 0f), Phase = 2.6f, HoverExpand = 0.13f, HoverExtraLift = 0.04f, HoverExtraForward = 0.03f, SortingOffset = 4 },
                new CardSlotDef { Position = new Vector3(sx, 1.08f, -0.03f), Euler = new Vector3(0f, -8f, 1f), Phase = 2.9f, HoverExpand = 0.13f, HoverExtraLift = 0.06f, HoverExtraForward = 0.05f, SortingOffset = 5 },
            };
        }

        private static Mesh BuildCardMesh()
        {
            var halfW = (1f / CardAspect) * 0.5f;
            var halfH = 0.5f;
            var stockUv = 2f * BorderY;
            var stockWorld = stockUv;

            var mesh = new Mesh { name = "RitualSpreadPreviewCardMesh", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                new Vector3(-halfW, halfH, 0f),
                new Vector3(halfW, halfH, 0f),
                new Vector3(halfW, -halfH - stockWorld, 0f),
                new Vector3(-halfW, -halfH - stockWorld, 0f),
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, -stockUv),
                new Vector2(0f, -stockUv),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildShadowMesh(float width, float height)
        {
            var mesh = new Mesh { name = "RitualSpreadPreviewShadowMesh", hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[]
            {
                new Vector3(-0.68f, 0.62f, 0f),
                new Vector3(0.68f, 0.62f, 0f),
                new Vector3(0.68f, -0.62f, 0f),
                new Vector3(-0.68f, -0.62f, 0f),
            };
            const float pad = 36f;
            mesh.uv = new[]
            {
                new Vector2(-pad, -pad),
                new Vector2(width + pad, -pad),
                new Vector2(width + pad, height + pad + 18f),
                new Vector2(-pad, height + pad + 18f),
            };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (view != null)
            {
                view.Set2DSpreadSymbolsHidden(false);
            }

            for (var i = 0; i < formations.Count; i++)
            {
                var f = formations[i];
                if (f.ShadowRenderer != null) RitualPaperRendering.Unregister(f.ShadowRenderer);
                if (f.ShadowMesh != null) Destroy(f.ShadowMesh);
                for (var c = 0; c < f.Cards.Count; c++)
                {
                    if (f.Cards[c].Renderer != null) RitualPaperRendering.Unregister(f.Cards[c].Renderer);
                }
            }

            formations.Clear();
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            if (cardMesh != null) Destroy(cardMesh);
            if (cardMaterial != null) Destroy(cardMaterial);
            if (shadowMaterial != null) Destroy(shadowMaterial);
        }
    }
}

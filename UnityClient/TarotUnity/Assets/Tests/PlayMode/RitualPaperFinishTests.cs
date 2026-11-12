using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualPaperFinishTests
    {
        private ApiClient previous;
        private Camera camera;
        private ReadingFlowController flow;
        private CardView card;
        private Transform probeSlot, backdrop;
        private Sprite white;
        private RenderTexture target;
        private Component finish;
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return new WaitForSeconds(1f);
            camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c => c.isActiveAndEnabled)
                .OrderByDescending(c => c.depth).First();
            Object.FindFirstObjectByType<CameraChoreographyController>().enabled = false;
            flow = Object.FindFirstObjectByType<ReadingFlowController>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Offline;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("PaperFinishCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (white != null) Object.Destroy(white);
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator ActualFinalSlotsAddOneFinishWithoutReplacingPrintOrPointerGeometry()
        {
            var root = GameObject.Find("ReadingRoomCanvas");
            SetWaitingFlow();
            root.GetComponent<RitualSpreadPresentation>().Prepare(1);
            var deck = Object.FindFirstObjectByType<DeckController>();
            var draws = LocalReadingSimulator.CreatePlaceholderDraws(1);
            yield return deck.DealCards(draws, flow.GetSelectedSpreadSlots());
            card = deck.ActiveCards[0];
            finish = RequireFinish(card);
            yield return null;
            var renderer = Frame();
            Assert.That(renderer.enabled, Is.True);
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Tarot/RitualPaperFinish"));
            Assert.That(renderer.sharedMaterials.Select(m => m.renderQueue),Is.EqualTo(new[] { 2999,3001 }));
            Assert.That(renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount,Is.EqualTo(2));
            Assert.That(renderer.gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("RitualPaper")));
            Assert.That(renderer.shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(renderer.receiveShadows, Is.False);
            Assert.That(renderer.GetComponentsInChildren<Collider>(), Is.Empty);
            Assert.That(card.DrawData, Is.SameAs(draws[0]));
            Assert.That(card.transform.parent, Is.SameAs(flow.GetSelectedSpreadSlots()[0]));
            Assert.That(card.GetComponent<Collider>().gameObject.layer, Is.Zero);
            Assert.That(card.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy)
                .Except(new[] { renderer, card.GetComponent<RitualPaperSurface>().Back }), Is.Empty,
                "Only the actual printed back and its new finish may be visible, never old bodies/shadows.");
            Physics.SyncTransforms();
            var point = camera.WorldToScreenPoint(card.transform.position);
            Assert.That(Physics.Raycast(camera.ScreenPointToRay(point), out var hit), Is.True);
            Assert.That(hit.collider.GetComponentInParent<CardView>(), Is.SameAs(card));
        }

        [UnityTest]
        public IEnumerator PrintedCenterStaysExactWhileGoldEdgeAndSoftShadowMatchReferencePixels()
        {
            CreateProbe();
            card.SetFaceUp(true);
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1024,768), new Vector2Int(2560,1440), new Vector2Int(2048,1536) })
            {
                yield return ResizeProbe(size, false);
                var d = size.x >= 2000 ? 2 : 1;
                var cx = size.x / 2; var cy = size.y / 2;
                var right = cx + 80*d; var bottom = cy - 130*d;
                var pixels = Render();
                try
                {
                    AssertRgb(pixels.GetPixel(cx,cy), Color.white, 0.01f);
                    AssertRgb(pixels.GetPixel(right-1,cy), new Color(223f/255f,199f/255f,141f/255f), 0.025f);
                    AssertRgb(pixels.GetPixel(right+8*d,cy), Color.white*0.914f, 0.025f);
                    AssertRgb(pixels.GetPixel(cx,bottom-12*d), Color.white*0.685f, 0.025f);
                    AssertRgb(pixels.GetPixel(right+50*d,cy), Color.white, 0.01f);
                    var path = Environment.GetEnvironmentVariable("TAROT_FINISH_REPORT_DIR");
                    if (!string.IsNullOrEmpty(path)) File.WriteAllBytes(Path.Combine(path, $"finish-probe-{size.x}x{size.y}.png"), pixels.EncodeToPNG());
                }
                finally { Object.Destroy(pixels); }
            }
        }

        [UnityTest]
        public IEnumerator UpperRowShadowNeverDarkensTheLowerRowsPrintedInterior()
        {
#if UNITY_EDITOR
            CreateProbe();
            card.SetFaceUp(true);
            yield return ResizeProbe(new Vector2Int(1280,720),false);
            probeSlot.localScale *= .5f;
            probeSlot.position = camera.ScreenToWorldPoint(new Vector3(640f,430f,2.5f));
            var lowerSlot = new GameObject("LowerRowPaperSlot",typeof(RitualSpreadSlot)).transform;
            lowerSlot.SetPositionAndRotation(camera.ScreenToWorldPoint(new Vector3(640f,272f,2.5f)),probeSlot.rotation);
            lowerSlot.localScale = probeSlot.localScale;
            var lower = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab")).GetComponent<CardView>();
            lower.Bind(LocalReadingSimulator.CreatePlaceholderDraws(1)[0]);
            lower.SetFaceArtwork(white);
            lowerSlot.GetComponent<RitualSpreadSlot>().Dock(lower);
            FinishType().GetMethod("Attach").Invoke(null,new object[] { lower,camera,flow });
            lower.SetFaceUp(true);
            yield return null;
            yield return null;
            var pixels = Render();
            try
            {
                // A 28px row gap matches the compact ten-card layout; the first white pixels must stay white.
                AssertRgb(pixels.GetPixel(640,334),Color.white,0.0015f);
                AssertRgb(pixels.GetPixel(640,336),new Color(223f/255f,199f/255f,141f/255f),0.025f);
            }
            finally { Object.Destroy(pixels); }
#else
            Assert.Ignore("Requires the actual prefab in the editor.");
            yield break;
#endif
        }

        [UnityTest]
        public IEnumerator BackAndReversedFaceKeepTheirOwnInkThroughViewportChanges()
        {
            CreateProbe();
            var material = card.FaceArtwork.sharedMaterial;
            var draw = card.DrawData;
            foreach (var reversed in new[] { false, true })
            {
                card.DrawData.is_reversed = reversed;
                card.SetFaceUp(true);
                card.GetComponent<RitualPaperSurface>().EnforcePaper();
                foreach (var subrect in new[] { false, true, false })
                {
                    yield return ResizeProbe(new Vector2Int(1280,720), subrect);
                    var bounds = CardBounds();
                    var x = Mathf.RoundToInt(bounds.xMax) - 1;
                    var pixels = Render();
                    var gold = new Color(223f/255f,199f/255f,141f/255f);
                    // The 80%-width viewport covers this white pixel with 0.8 physical pixels of border.
                    var expected = subrect ? Color.Lerp(Color.white,gold.linear,0.8f).gamma : gold;
                    try { AssertRgb(pixels.GetPixel(x,Mathf.RoundToInt(bounds.center.y)), expected, 0.03f); }
                    finally { Object.Destroy(pixels); }
                    Assert.That(card.DrawData, Is.SameAs(draw));
                    Assert.That(card.ShowsReversed, Is.EqualTo(reversed));
                    Assert.That(card.FaceArtwork.sharedMaterial, Is.SameAs(material));
                }
            }
            card.SetFaceUp(false);
            yield return ResizeProbe(new Vector2Int(1280,720), false);
            var backPixels = Render();
            try { AssertRgb(backPixels.GetPixel(719,360), new Color(181f/255f,139f/255f,74f/255f), 0.03f); }
            finally { Object.Destroy(backPixels); }
        }

        [UnityTest]
        public IEnumerator FinishHidesDuringInspectionAndNonFinalStagesThenReturns()
        {
            CreateProbe();
            card.SetFaceUp(true);
            yield return ResizeProbe(new Vector2Int(1280,720), false);
            var inspection = Object.FindFirstObjectByType<CardInspectionController>();
            Assert.That(inspection.Show(card, false), Is.True);
            yield return null;
            Assert.That(Frame().enabled, Is.False);
            inspection.CloseImmediate();
            yield return null;
            Assert.That(Frame().enabled, Is.True);
            flow.EnterSpreadSelect();
            yield return null;
            Assert.That(Frame().enabled, Is.False);
            SetWaitingFlow();
            yield return null;
            Assert.That(Frame().enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator RealFlipHidesTheFinishUntilInspectionAndLandingEnd()
        {
            CreateProbe();
            yield return ResizeProbe(new Vector2Int(1280,720),false);
            var draw = card.DrawData;
            var material = card.FaceArtwork.sharedMaterial;
            var flip = card.GetComponent<CardFlipController>();
            flip.Flip(card);
            Assert.That(flip.IsFlipping,Is.True);
            yield return null;
            Assert.That(Frame().enabled,Is.False);
            var deadline = Time.realtimeSinceStartup+10f;
            while (flip.IsFlipping && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(flip.IsFlipping,Is.False);
            yield return null;
            Assert.That(Frame().enabled,Is.True);
            Assert.That(card.IsFaceUp,Is.True);
            Assert.That(card.DrawData,Is.SameAs(draw));
            Assert.That(card.FaceArtwork.sharedMaterial,Is.SameAs(material));
        }

        [UnityTest]
        public IEnumerator RebuildAndRemovalReleaseOnlyOwnedMeshesAndRoomRegistrations()
        {
            var paper = GameObject.Find("ReadingRoomCanvas").GetComponent<RitualPaperRendering>();
            var registry = (System.Collections.IDictionary)typeof(RitualPaperRendering).GetField("layers", Instance).GetValue(paper);
            var baseline = registry.Count;
            CreateProbe();
            yield return ResizeProbe(new Vector2Int(1280,720), false);
            Assert.That(registry.Count, Is.EqualTo(baseline+3));
            for (var i = 0; i < 2; i++)
            {
                var mesh = Frame().GetComponent<MeshFilter>().sharedMesh;
                ((Behaviour)finish).enabled = false;
                yield return null;
                yield return null;
                Assert.That(mesh == null, Is.True);
                Assert.That(registry.Count, Is.EqualTo(baseline+2));
                ((Behaviour)finish).enabled = true;
                yield return null;
                Assert.That(Frame().enabled, Is.True);
                Assert.That(registry.Count, Is.EqualTo(baseline+3));
            }
            paper.enabled = false;
            yield return null;
            Assert.That(Frame().enabled, Is.False);
            paper.enabled = true;
            yield return null;
            Assert.That(Frame().enabled, Is.True);
            Assert.That(Frame().gameObject.layer, Is.EqualTo(LayerMask.NameToLayer("RitualPaper")));
            Object.Destroy(finish);
            yield return null;
            yield return null;
            Assert.That(card != null, Is.True);
            Assert.That(card.GetComponentsInChildren<Renderer>(true).Any(r => r.name == "RitualPaperFinishMesh"), Is.False);
            Assert.That(registry.Count, Is.EqualTo(baseline+2));
            Object.Destroy(card.gameObject);
            yield return null;
            yield return null;
            Assert.That(registry.Count, Is.EqualTo(baseline));
        }

        private void CreateProbe()
        {
#if UNITY_EDITOR
            GameObject.Find("ReadingRoomCanvas").GetComponent<Canvas>().enabled = false;
            SetWaitingFlow();
            white = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.one*.5f, 1f);
            backdrop = new GameObject("FinishWhiteBackdrop", typeof(SpriteRenderer)).transform;
            backdrop.GetComponent<SpriteRenderer>().sprite = white;
            backdrop.GetComponent<SpriteRenderer>().sortingOrder = -10000;
            backdrop.gameObject.layer = LayerMask.NameToLayer("RitualPaper");
            probeSlot = new GameObject("FinishProbeSlot", typeof(RitualSpreadSlot)).transform;
            card = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab")).GetComponent<CardView>();
            card.Bind(LocalReadingSimulator.CreatePlaceholderDraws(1)[0]);
            card.SetFaceArtwork(white);
            probeSlot.GetComponent<RitualSpreadSlot>().Dock(card);
            var type = FinishType();
            finish = (Component)type.GetMethod("Attach").Invoke(null, new object[] { card, camera, flow });
            Assert.That(finish, Is.Not.Null);
#else
            Assert.Ignore("Requires the actual prefab in the editor.");
#endif
        }

        private IEnumerator ResizeProbe(Vector2Int size, bool subrect)
        {
            var old = target;
            target = new RenderTexture(size.x,size.y,24);
            camera.targetTexture = target;
            camera.rect = subrect ? new Rect(.1f,.1f,.8f,.8f) : new Rect(0f,0f,1f,1f);
            camera.aspect = (float)size.x/size.y;
            if (old != null) { old.Release(); Object.Destroy(old); }
            var width = camera.pixelWidth; var height = camera.pixelHeight;
            var d = width >= 2000 ? 2f : 1f;
            var unit = 5f*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/height;
            probeSlot.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(.5f,.5f,2.5f)),camera.transform.rotation*Quaternion.Euler(-90f,0f,0f));
            probeSlot.localScale = new Vector3(160f*d*unit/.64f,1f,260f*d*unit);
            backdrop.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(.5f,.5f,3.5f)),camera.transform.rotation);
            var h = 7f*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
            backdrop.localScale = new Vector3(h*camera.aspect*2f,h*2f,1f);
            yield return null;
            yield return null;
        }

        private Rect CardBounds()
        {
            var image = card.IsFaceUp ? card.FaceArtwork : card.GetComponent<RitualPaperSurface>().Back;
            var b = image.sprite.bounds;
            var min = Vector2.one*float.PositiveInfinity; var max = Vector2.one*float.NegativeInfinity;
            foreach (var x in new[] { b.min.x,b.max.x }) foreach (var y in new[] { b.min.y,b.max.y })
            {
                var p = camera.WorldToScreenPoint(image.transform.TransformPoint(new Vector3(x,y,0f)));
                min = Vector2.Min(min,p); max = Vector2.Max(max,p);
            }
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }

        private Texture2D Render()
        {
            var old = RenderTexture.active;
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                for (var i = 0; i < 4; i++) RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active = target;
                var result = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                result.ReadPixels(new Rect(0,0,target.width,target.height),0,0); result.Apply();
                return result;
            }
            finally { RenderTexture.active = old; }
        }

        private void SetWaitingFlow()
        { flow.EnterSpreadSelect(); flow.SelectSpread(1,1); flow.SetQuestion("An offline paper finish test","general"); flow.BeginShuffle(); flow.BeginDeal(); flow.WaitForCardFlips(); }
        private static Type FinishType()
        {
            var type = typeof(RitualPaperSurface).Assembly.GetType("TarotUnity.Presentation.RitualPaperFinish");
            Assert.That(type, Is.Not.Null, "The native card-owned finish is not implemented.");
            return type;
        }
        private static Component RequireFinish(CardView value)
        {
            var result = value.GetComponent(FinishType());
            Assert.That(result, Is.Not.Null, "Actual final slots must attach the native finish automatically.");
            return result;
        }
        private Renderer Frame() => (Renderer)finish.GetType().GetProperty("FrameRenderer").GetValue(finish);
        private static void AssertRgb(Color actual,Color expected,float tolerance)
        { Assert.That(actual.r,Is.EqualTo(expected.r).Within(tolerance)); Assert.That(actual.g,Is.EqualTo(expected.g).Within(tolerance)); Assert.That(actual.b,Is.EqualTo(expected.b).Within(tolerance)); }
        private static void Offline(Scene scene,LoadSceneMode mode)
        { if(scene.name=="ReadingRoom") typeof(ReadingRoomController).GetField("backendMode",Instance).SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation); }
    }
}

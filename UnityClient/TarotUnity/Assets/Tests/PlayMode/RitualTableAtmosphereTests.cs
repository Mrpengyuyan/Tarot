using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualTableAtmosphereTests
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        private ApiClient previous;
        private Camera camera;
        private RenderTexture target;
        private Sprite white;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return new WaitForSeconds(1f);
            camera = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c => c.isActiveAndEnabled).OrderByDescending(c => c.depth).First();
            Object.FindFirstObjectByType<CameraChoreographyController>().enabled = false;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Offline;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("AtmosphereCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if (white != null) Object.Destroy(white);
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator ActualRoomAddsOneUnlitBackdropWithoutChangingTheCandleOrWorldGrade()
        {
            var frame = Frame();
            Assert.That(frame.transform.parent.name,Is.EqualTo("RitualPaperCamera"));
            Assert.That(frame.sharedMaterial.shader.name,Is.EqualTo("Tarot/RitualTableAtmosphere"));
            Assert.That(frame.sharedMaterial.renderQueue,Is.LessThan(2999));
            Assert.That(frame.sortingOrder,Is.EqualTo(short.MinValue));
            Assert.That(frame.gameObject.layer,Is.EqualTo(LayerMask.NameToLayer("RitualPaper")));
            Assert.That(frame.GetComponentsInChildren<Collider>(),Is.Empty);
            Assert.That(frame.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(frame.receiveShadows,Is.False);
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Count(c => c.name=="RitualPaperCamera"),Is.EqualTo(1));
            var profile = Object.FindObjectsByType<Volume>(FindObjectsSortMode.None)
                .First(v => v.sharedProfile != null && v.sharedProfile.name=="Phase20_CinematicVolumeProfile").sharedProfile;
            Assert.That(profile.TryGet<Tonemapping>(out var tone),Is.True);
            Assert.That(tone.mode.value,Is.EqualTo(TonemappingMode.ACES));
            Assert.That(profile.TryGet<ColorAdjustments>(out var grade),Is.True);
            Assert.That(grade.postExposure.value,Is.EqualTo(0.45f));
            Assert.That(profile.TryGet<Bloom>(out var bloom),Is.True);
            Assert.That(bloom.intensity.value,Is.EqualTo(0.95f));
            foreach (var name in new[] { "MP_RoomCandle_L","MP_RoomCandle_R","MP_RoomCandle_BackL","MP_RoomCandle_BackR" })
            {
                var flicker = GameObject.Find(name).GetComponent<CandleFlickerController>();
                Assert.That(flicker.enabled,Is.True);
                Assert.That((float)typeof(CandleFlickerController).GetField("baseIntensity",Instance).GetValue(flicker),
                    Is.EqualTo(name.Contains("Back") ? 1.5f : 4.2f));
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SourceEllipseStopsRemainStableAcrossAspectHdAndSubViewports()
        {
            var frame = PrepareProbe();
            foreach (var size in new[] { new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(2560,1440) })
            foreach (var subrect in new[] { false,true })
            {
                Resize(size,subrect);
                camera.backgroundColor = Color.white;
                yield return null;
                frame.enabled = false;
                var plain = Render();
                frame.enabled = true;
                var shaded = Render();
                try
                {
                    // Literal v6 gradient stops, using farthest-corner radii (.5,.54)*sqrt(2).
                    AssertRgb(Sample(shaded,.669706f,.54f),Sample(plain,.669706f,.54f),0.01f);
                    AssertRgb(Sample(shaded,.938406f,.54f),Color.Lerp(Color.white,new Color(8f/255f,3f/255f,0f),34f/255f),0.025f);
                    AssertRgb(Sample(shaded,.001f,.001f),Color.Lerp(Color.white,new Color(8f/255f,3f/255f,0f),144f/255f),0.025f);
                    if (subrect) AssertRgb(shaded.GetPixel(2,2),Color.magenta,0.001f);
                }
                finally { Object.Destroy(plain); Object.Destroy(shaded); }
            }
        }

        [UnityTest]
        public IEnumerator WarmVignetteDoesNotTurnTheBlackRoomIntoAnEmissiveVeil()
        {
            PrepareProbe();
            Resize(new Vector2Int(1280,720),false);
            camera.backgroundColor = Color.black;
            yield return null;
            var shaded = Render();
            try
            {
                var tint = new Color(8f/255f,3f/255f,0f);
                AssertRgb(Sample(shaded,.5f,.75f),new Color(.00503f,.0037f,.00134f),0.0035f);
                AssertRgb(Sample(shaded,.938406f,.54f),tint*(34f/255f),0.0025f);
                AssertRgb(Sample(shaded,.001f,.001f),tint*(144f/255f),0.0025f);
            }
            finally { Object.Destroy(shaded); }
        }

        [UnityTest]
        public IEnumerator CentralWarmWashStaysSubtleAndNeverTintsPrintedWhitePaper()
        {
            var frame = PrepareProbe();
            Resize(new Vector2Int(1280,720),false);
            camera.backgroundColor = new Color(.27f,.075f,.025f);
            yield return null;
            frame.enabled = false;
            var plain = Render();
            frame.enabled = true;
            var shaded = Render();
            try
            {
                var expected = Color.Lerp(Sample(plain,.5f,.475f),new Color(229f/255f,169f/255f,66f/255f),11f/255f);
                AssertRgb(Sample(shaded,.5f,.475f),expected,0.025f);
                AssertRgb(Sample(shaded,.669706f,.54f),Sample(plain,.669706f,.54f),0.01f);
            }
            finally { Object.Destroy(plain); Object.Destroy(shaded); }

            white = Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1f);
            var print = new GameObject("AtmosphereWhitePaper",typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            print.sprite = white;
            print.gameObject.layer = LayerMask.NameToLayer("RitualPaper");
            print.transform.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(.5f,.475f,1f)),camera.transform.rotation);
            print.transform.localScale = Vector3.one*.1f;
            yield return null;
            var paper = Render();
            try { AssertRgb(Sample(paper,.5f,.475f),Color.white,0.005f); }
            finally { Object.Destroy(paper); }
        }

        [UnityTest]
        public IEnumerator OrthographicAndPerspectiveFramesUseTheSameGradientCoordinates()
        {
            PrepareProbe();
            Resize(new Vector2Int(1024,768),false);
            camera.backgroundColor = Color.white;
            yield return null;
            var perspective = Render();
            camera.orthographic = true;
            camera.orthographicSize = 2f;
            yield return null;
            var orthographic = Render();
            try
            {
                foreach (var point in new[] { new Vector2(.1f,.3f),new Vector2(.5f,.475f),new Vector2(.938406f,.54f) })
                    AssertRgb(Sample(orthographic,point.x,point.y),Sample(perspective,point.x,point.y),0.005f);
            }
            finally { Object.Destroy(perspective); Object.Destroy(orthographic); }
        }

        [UnityTest]
        public IEnumerator PassRebuildDisposesOnlyItsBackdropAndLeavesNoAtmosphereInTheMenu()
        {
            var pass = GameObject.Find("ReadingRoomCanvas").GetComponent<RitualPaperRendering>();
            var shared = Frame().sharedMaterial;
            var stack = camera.GetUniversalAdditionalCameraData().cameraStack;
            for (var i=0; i<2; i++)
            {
                var frame = Frame();
                var mesh = frame.GetComponent<MeshFilter>().sharedMesh;
                pass.enabled = false;
                yield return null;
                yield return null;
                Assert.That(frame==null,Is.True);
                Assert.That(mesh==null,Is.True);
                Assert.That(stack.Count,Is.Zero);
                Assert.That(shared!=null,Is.True);
                pass.enabled = true;
                yield return null;
                Assert.That(Frame().sharedMaterial,Is.SameAs(shared));
                Assert.That(stack.Count,Is.EqualTo(1));
            }
            var finalMesh = Frame().GetComponent<MeshFilter>().sharedMesh;
            yield return SceneManager.LoadSceneAsync("MainMenu");
            yield return null;
            Assert.That(GameObject.Find("RitualTableAtmosphere"),Is.Null);
            Assert.That(finalMesh==null,Is.True);
            Assert.That(shared!=null,Is.True);
        }

        private static MeshRenderer Frame()
        {
            var node = GameObject.Find("RitualTableAtmosphere");
            Assert.That(node,Is.Not.Null,"The native table atmosphere is not attached to the real room yet.");
            return node.GetComponent<MeshRenderer>();
        }

        private MeshRenderer PrepareProbe()
        {
            var frame = Frame();
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                if (renderer!=frame) renderer.enabled = false;
            GameObject.Find("ReadingRoomCanvas").GetComponent<Canvas>().enabled = false;
            camera.cullingMask = 0;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
            return frame;
        }

        private void Resize(Vector2Int size,bool subrect)
        {
            var old = target;
            target = new RenderTexture(size.x,size.y,24);
            camera.targetTexture = target;
            camera.aspect = (float)size.x/size.y;
            camera.rect = subrect ? new Rect(.1f,.1f,.8f,.8f) : new Rect(0,0,1,1);
            if (old!=null) { old.Release(); Object.Destroy(old); }
        }

        private Texture2D Render()
        {
            var old = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                GL.Clear(true,true,Color.magenta);
                var request = new RenderPipeline.StandardRequest { destination=target };
                for (var i=0; i<4; i++) RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active = target;
                var pixels = new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply();
                return pixels;
            }
            finally { RenderTexture.active = old; }
        }

        private Color Sample(Texture2D pixels,float x,float y)
        {
            var rect = camera.pixelRect;
            return pixels.GetPixel(Mathf.FloorToInt(rect.x+x*rect.width),Mathf.FloorToInt(rect.y+y*rect.height));
        }

        private static void AssertRgb(Color actual,Color expected,float tolerance)
        {
            Assert.That(actual.r,Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(actual.g,Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(actual.b,Is.EqualTo(expected.b).Within(tolerance));
        }

        private static void Offline(Scene scene,LoadSceneMode mode)
        {
            if (scene.name=="ReadingRoom") typeof(ReadingRoomController).GetField("backendMode",Instance)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation);
        }
    }
}

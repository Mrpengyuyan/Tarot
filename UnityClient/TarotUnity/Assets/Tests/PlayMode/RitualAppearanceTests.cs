using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualAppearanceTests
    {
        private ApiClient previous;
        private Camera camera;
        private Canvas canvas;
        private RenderTexture target;
        private readonly List<Object> owned = new();
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly Vector2Int[] Sizes = { new(1280,720),new(1024,768),new(2560,1440),new(2048,1536) };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous = ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded += Offline;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded -= Offline;
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); Object.Destroy(target); }
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("AppearanceCleanup"));
            if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            foreach (var item in owned) if (item != null) Object.Destroy(item);
            owned.Clear();
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CoverOwnsOneBackgroundShadeAndPrivateInkWithoutChangingFontOrButtonTargets()
        {
            yield return Load("MainMenu");
            var finish = CoverFinish();
            var shade = CoverShade();
            var title = canvas.transform.Find("TitleText").GetComponent<TMP_Text>();
            var source = title.font.material;
            var ink = title.fontSharedMaterial;
            var atlas = source.mainTexture;
            var start = canvas.transform.Find("StartReadingButton").GetComponent<Button>();
            var listener = start.onClick;
            var font = title.font;
            Assert.That(shade.transform.GetSiblingIndex(), Is.Zero);
            Assert.That(shade.raycastTarget, Is.False);
            Assert.That(shade.material.shader.name, Is.EqualTo("Tarot/RitualCoverShade"));
            Assert.That(ink, Is.Not.SameAs(source));
            Assert.That(ink.mainTexture, Is.SameAs(atlas));
            Assert.That(ink.shader.name, Is.EqualTo("TextMeshPro/Distance Field"));
            Assert.That(ink.GetFloat("_FaceDilate"),Is.EqualTo(source.GetFloat("_FaceDilate")));
            Assert.That(ink.GetFloat("_WeightNormal"),Is.EqualTo(source.GetFloat("_WeightNormal")));
            Assert.That(ink.GetTexture("_FaceTex"),Is.Not.Null);
            foreach (var size in Sizes)
            {
                using (var frame = new RitualFrameCapture(camera,canvas,size.x,size.y))
                {
                    yield return null;
                    canvas.GetComponent<TarotUiTheme>().Apply();
                    yield return null;
                    Canvas.ForceUpdateCanvases();
                    title.ForceMeshUpdate();
                    Assert.That(title.font, Is.SameAs(font));
                    Assert.That(title.fontSharedMaterial, Is.SameAs(ink));
                    Assert.That(start.onClick, Is.SameAs(listener));
                    var rect = (RectTransform)shade.transform;
                    Assert.That(rect.rect.size, Is.EqualTo(((RectTransform)canvas.transform).rect.size));
                    Assert.That(canvas.GetComponentsInChildren<Image>(true).Count(i=>i.name=="RitualCoverShade"), Is.EqualTo(1));
                    Assert.That(title.textInfo.characterCount, Is.EqualTo(4));
                }
            }
            Assert.That(((Behaviour)finish).enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator CoverShadeMatchesSourceStopsWithoutDarkeningLaterPrintedUi()
        {
            yield return Load("MainMenu");
            var shade = CoverShade();
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>()) if (graphic!=shade) graphic.enabled=false;
            camera.cullingMask=0;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            camera.clearFlags=CameraClearFlags.SolidColor;
            var paper = new GameObject("LaterUiPrint",typeof(RectTransform),typeof(Image));
            paper.transform.SetParent(canvas.transform,false);
            var paperRect=(RectTransform)paper.transform;
            paperRect.anchorMin=paperRect.anchorMax=new Vector2(.5f,.5f);
            paperRect.sizeDelta=new Vector2(30,30);
            paper.GetComponent<Image>().color=Color.white;
            paper.GetComponent<Image>().raycastTarget=false;
            foreach (var size in Sizes)
            foreach (var backdrop in new[] { Color.white,new Color(.24f,.12f,.055f) })
            {
                camera.backgroundColor=backdrop;
                using (var frame=new RitualFrameCapture(camera,canvas,size.x,size.y))
                {
                    yield return null;
                    var pixels=frame.Render();
                    try
                    {
                        var tint=new Color(3f/255,3f/255,2f/255);
                        foreach (var row in new[] { new Vector2(.01f,141.15556f/255),new Vector2(.45f,16f/255),
                            new Vector2(.55f,16f/255),new Vector2(.65f,16f/255),new Vector2(.99f,149.08571f/255) })
                            AssertRgb(pixels.GetPixel(size.x/4,Mathf.FloorToInt((1-row.x)*size.y)),
                                Color.Lerp(backdrop,tint,row.y),backdrop==Color.white?.014f:.03f);
                        AssertRgb(pixels.GetPixel(size.x/2,size.y/2),Color.white,.005f);
                    }
                    finally { Object.Destroy(pixels); }
                }
            }
        }

        [UnityTest]
        public IEnumerator CoverShadeHonorsCanvasGroupFadesInsteadOfRemainingAsAnOpaqueOverlay()
        {
            yield return Load("MainMenu");
            var shade=CoverShade();
            foreach(var graphic in canvas.GetComponentsInChildren<Graphic>()) if(graphic!=shade) graphic.enabled=false;
            camera.cullingMask=0;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.white;
            var group=canvas.gameObject.AddComponent<CanvasGroup>();
            using(var frame=new RitualFrameCapture(camera,canvas,1280,720))
            {
                yield return null;
                var full=frame.Render();
                group.alpha=.5f;
                yield return null;
                var faded=frame.Render();
                try
                {
                    var expected=Color.Lerp(Color.white.linear,full.GetPixel(320,712).linear,.5f).gamma;
                    AssertRgb(faded.GetPixel(320,712),expected,.015f);
                }
                finally { Object.Destroy(full); Object.Destroy(faded); }
            }
        }

        [UnityTest]
        public IEnumerator FiveStopGoldRunsAcrossRealGlyphsInsteadOfRepeatingForEachCharacter()
        {
            yield return Load("MainMenu");
            CoverFinish();
            var title=canvas.transform.Find("TitleText").GetComponent<TMP_Text>();
            foreach (var graphic in canvas.GetComponentsInChildren<Graphic>()) if (graphic!=title) graphic.enabled=false;
            camera.cullingMask=0;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.black;
            var ink=title.fontSharedMaterial;
            var white=new Material(ink);
            owned.Add(white);
            white.SetTexture("_FaceTex",Texture2D.whiteTexture);
            var stops=new[] { .08f,.32f,.56f,.74f,1f };
            var colors=new[] { new Color(1,240f/255,207f/255),new Color(226f/255,198f/255,141f/255),
                new Color(183f/255,139f/255,76f/255),new Color(244f/255,219f/255,168f/255),new Color(197f/255,157f/255,96f/255) };
            foreach (var size in Sizes)
            using (var frame=new RitualFrameCapture(camera,canvas,size.x,size.y))
            {
                yield return null;
                Canvas.ForceUpdateCanvases();
                title.ForceMeshUpdate();
                var vertices=title.textInfo.meshInfo[0].vertices.ToArray();
                var gold=frame.Render();
                title.fontSharedMaterial=white;
                title.ForceMeshUpdate();
                var mask=frame.Render();
                try
                {
                    Assert.That(title.textInfo.meshInfo[0].vertices,Is.EqualTo(vertices),"Gold ink cannot alter the brush outline or layout.");
                    var rect=title.rectTransform.rect;
                    var checks=new int[4];
                    for(var y=0;y<size.y;y+=2)
                    for(var x=0;x<size.x;x+=2)
                    {
                        if(mask.GetPixel(x,y).r<.99f) continue;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(title.rectTransform,new Vector2(x+.5f,y+.5f),
                            canvas.worldCamera,out var local);
                        var p=local-rect.center;
                        var t=.5f+(p.x*.25881905f-p.y*.96592583f)/(rect.width*.25881905f+rect.height*.96592583f);
                        var band=0;
                        while(band<3 && t>stops[band+1]) band++;
                        var expected=Color.Lerp(colors[band],colors[band+1],Mathf.InverseLerp(stops[band],stops[band+1],t));
                        AssertRgb(gold.GetPixel(x,y),expected,.022f);
                        checks[band]++;
                    }
                    Assert.That(checks.Count(c=>c>15),Is.GreaterThanOrEqualTo(3),"Probe actual letters in multiple gold bands, not empty pixels.");
                }
                finally { Object.Destroy(gold); Object.Destroy(mask); title.fontSharedMaterial=ink; title.ForceMeshUpdate(); }
            }
        }

        [UnityTest]
        public IEnumerator CoverDisableRebuildAndSceneExitReleaseOnlyOwnedResources()
        {
            yield return Load("MainMenu");
            var finish=(Behaviour)CoverFinish();
            var title=canvas.transform.Find("TitleText").GetComponent<TMP_Text>();
            var source=title.font.material;
            var atlas=source.mainTexture;
            for(var i=0;i<2;i++)
            {
                var ink=title.fontSharedMaterial;
                var gradient=ink.GetTexture("_FaceTex");
                var shade=CoverShade();
                finish.enabled=false;
                yield return null;
                Assert.That(ink==null,Is.True);
                Assert.That(gradient==null,Is.True);
                Assert.That(shade==null,Is.True);
                Assert.That(title.fontSharedMaterial,Is.SameAs(source));
                Assert.That(source.mainTexture,Is.SameAs(atlas));
                finish.enabled=true;
                yield return null;
                Assert.That(title.fontSharedMaterial,Is.Not.SameAs(source));
                Assert.That(canvas.GetComponentsInChildren<Image>(true).Count(g=>g.name=="RitualCoverShade"),Is.EqualTo(1));
            }
            var finalInk=title.fontSharedMaterial;
            yield return Load("ReadingRoom");
            Assert.That(finalInk==null,Is.True);
            Assert.That(GameObject.Find("RitualCoverShade"),Is.Null);
            Assert.That(source!=null && atlas!=null,Is.True);
        }

        [UnityTest]
        public IEnumerator DeckStockAddsTwoNarrowBandsWithoutStretchingOrDoubleBlendingItsPrint()
        {
            yield return Load("ReadingRoom");
            var material=new Material(Resources.Load<Material>("Presentation/RitualDeckInk"));
            owned.Add(material);
            material.mainTexture=Texture2D.whiteTexture;
            material.SetVector("_BorderSize",new Vector4(2f/128,2f/200,0,0));
            material.SetFloat("_Opacity",.48f);
            Probe(material,new Rect(0,0,128,204),new Rect(0,-.02f,1,1.02f),new Vector2(576,256));
            yield return null;
            var pixels=RenderWorld();
            try
            {
                AssertRgb(pixels.GetPixel(640,360),Color.white,.005f);
                AssertRgb(pixels.GetPixel(640,259),Color.Lerp(Color.white.linear,new Color(169f/255,135f/255,73f/255).linear,.48f).gamma,.03f);
                AssertRgb(pixels.GetPixel(640,257),Color.Lerp(Color.white.linear,new Color(50f/255,32f/255,11f/255).linear,.48f).gamma,.03f);
                AssertRgb(pixels.GetPixel(640,254),Color.white,.005f);
            }
            finally { Object.Destroy(pixels); }
        }

        [UnityTest]
        public IEnumerator DeckContactShadowMatchesSourceBlurAndDoesNotTintTheFlattenedPaperFootprint()
        {
            yield return Load("ReadingRoom");
            var source=Resources.Load<Material>("Presentation/RitualDeckShadow");
            Assert.That(source,Is.Not.Null,"Native source-shaped deck contact shadow is missing.");
            var material=new Material(source);
            owned.Add(material);
            material.SetVector("_Paper",new Vector4(100,200,1,.48f));
            material.SetFloat("_ShadowOpacity",.7f);
            Probe(material,new Rect(-32,-19,164,262),new Rect(-32,-19,164,262),new Vector2(558,197),true);
            yield return null;
            var pixels=RenderWorld();
            try
            {
                // Shadow rectangle x[-5,105], y[8,216], sigma9, group alpha.48; y grows downward.
                AssertRgb(pixels.GetPixel(640,340),Color.white,.005f);
                AssertRgb(pixels.GetPixel(585,340),new Color(.8967f,.8950f,.8946f),.02f);
                AssertRgb(pixels.GetPixel(576,340),new Color(.9672f,.9667f,.9666f),.02f);
                AssertRgb(pixels.GetPixel(640,224),new Color(.8967f,.8950f,.8946f),.02f);
                AssertRgb(pixels.GetPixel(550,340),Color.white,.006f);
            }
            finally { Object.Destroy(pixels); }
        }

        [UnityTest]
        public IEnumerator ActualShadowFollowsDeckVisibilityAndRebuildWithoutAddingCardsOrLegacySlabs()
        {
            yield return Load("ReadingRoom");
            var deck=Object.FindFirstObjectByType<RitualDeckPresentation>();
            var shadow=DeckShadow();
            Assert.That(shadow.transform.IsChildOf(deck.transform),Is.True);
            Assert.That(shadow.gameObject.layer,Is.EqualTo(LayerMask.NameToLayer("RitualPaper")));
            Assert.That(shadow.GetComponentsInChildren<Collider>(),Is.Empty);
            Assert.That(shadow.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
            Assert.That(shadow.receiveShadows,Is.False);
            var source=shadow.sharedMaterial;
            var oldMesh=shadow.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(Object.FindObjectsByType<CardView>(FindObjectsSortMode.None),Is.Empty);
            Assert.That(deck.transform.childCount,Is.EqualTo(9));
            var root=canvas.transform;
            root.Find("OneCardButton").GetComponent<Button>().onClick.Invoke();
            root.Find("QuestionInput").GetComponent<TMP_InputField>().text="A local shadow lifecycle test";
            root.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            var fan=Object.FindFirstObjectByType<SpreadFanController>();
            yield return Until(()=>fan.AcceptingPicks);
            Assert.That(shadow.enabled,Is.False,"No shadow can remain when the fan has consumed the parked stack.");
            deck.enabled=false;
            yield return null;
            Assert.That(shadow==null && oldMesh==null,Is.True);
            Assert.That(source!=null,Is.True);
            deck.enabled=true;
            yield return null;
            shadow=DeckShadow();
            Assert.That(shadow.enabled,Is.False);
            Assert.That(fan.FanCards.Count,Is.EqualTo(78));
            fan.RequestPick(fan.FanCards[39]);
            var flow=Object.FindFirstObjectByType<ReadingFlowController>();
            yield return Until(()=>flow.State==ReadingFlowState.WaitingForFlip && !deck.IsReturning);
            Assert.That(shadow.enabled,Is.True);
            Assert.That(deck.CurrentPose.Opacity,Is.EqualTo(.48f).Within(.001f));
            Assert.That(Object.FindFirstObjectByType<DeckController>().ActiveCards.Count,Is.EqualTo(1));
            Assert.That(deck.LayerRenderers.Count,Is.EqualTo(12));
            Assert.That(deck.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.name=="RitualDeckShadow"),Is.EqualTo(1));
        }

        private IEnumerator Load(string name)
        {
            yield return SceneManager.LoadSceneAsync(name);
            yield return new WaitForSeconds(.35f);
            canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.isRootCanvas && c.gameObject.scene==SceneManager.GetActiveScene());
            camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled && c.gameObject.scene==SceneManager.GetActiveScene())
                .OrderByDescending(c=>c.depth).First();
        }

        private Component CoverFinish()
        {
            var type=typeof(RitualMenuView).Assembly.GetType("TarotUnity.UI.RitualCoverFinish");
            Assert.That(type,Is.Not.Null,"Native cover finishing is not implemented yet.");
            var component=canvas.GetComponent(type);
            Assert.That(component,Is.Not.Null);
            return component;
        }

        private Image CoverShade()
        {
            var node=canvas.transform.Find("RitualCoverShade");
            Assert.That(node,Is.Not.Null,"The reference cover's background shade is missing.");
            return node.GetComponent<Image>();
        }

        private static MeshRenderer DeckShadow()
        {
            var node=GameObject.Find("RitualDeckShadow");
            Assert.That(node,Is.Not.Null,"The actual deck has no native contact shadow.");
            return node.GetComponent<MeshRenderer>();
        }

        private void Probe(Material material,Rect bounds,Rect uv,Vector2 origin,bool localPixels=false)
        {
            foreach(var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) renderer.enabled=false;
            canvas.enabled=false;
            Object.FindFirstObjectByType<CameraChoreographyController>().enabled=false;
            var deck=Object.FindFirstObjectByType<RitualDeckPresentation>();
            deck.enabled=false;
            camera.cullingMask=0;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.white;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            target=new RenderTexture(1280,720,24);
            camera.targetTexture=target;
            camera.aspect=1280f/720;
            var node=new GameObject("AppearancePixelProbe",typeof(MeshFilter),typeof(MeshRenderer));
            node.layer=LayerMask.NameToLayer("RitualPaper");
            node.GetComponent<MeshRenderer>().sharedMaterial=material;
            var mesh=new Mesh();
            owned.Add(mesh);
            var top=origin.y+bounds.height;
            mesh.vertices=new[] { camera.ScreenToWorldPoint(new Vector3(origin.x,top,1.5f)),
                camera.ScreenToWorldPoint(new Vector3(origin.x+bounds.width,top,1.5f)),
                camera.ScreenToWorldPoint(new Vector3(origin.x+bounds.width,origin.y,1.5f)),
                camera.ScreenToWorldPoint(new Vector3(origin.x,origin.y,1.5f)) };
            mesh.uv=localPixels ? new[] { new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMax,uv.yMin),
                new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMin,uv.yMax) }
                : new[] { new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMax,uv.yMax),
                    new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMin,uv.yMin) };
            mesh.triangles=new[] { 0,1,2,0,2,3 };
            mesh.RecalculateBounds();
            node.GetComponent<MeshFilter>().sharedMesh=mesh;
        }

        private Texture2D RenderWorld()
        {
            var active=RenderTexture.active;
            try
            {
                var request=new RenderPipeline.StandardRequest { destination=target };
                for(var i=0;i<4;i++) RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                pixels.Apply();
                return pixels;
            }
            finally { RenderTexture.active=active; }
        }

        private static void AssertRgb(Color value,Color expected,float tolerance)
        {
            Assert.That(value.r,Is.EqualTo(expected.r).Within(tolerance),"red");
            Assert.That(value.g,Is.EqualTo(expected.g).Within(tolerance),"green");
            Assert.That(value.b,Is.EqualTo(expected.b).Within(tolerance),"blue");
        }

        private static IEnumerator Until(Func<bool> condition)
        {
            var until=Time.realtimeSinceStartup+35;
            while(!condition()) { Assert.That(Time.realtimeSinceStartup,Is.LessThan(until)); yield return null; }
        }

        private static void Offline(Scene scene,LoadSceneMode mode)
        {
            if(scene.name=="ReadingRoom") typeof(ReadingRoomController).GetField("backendMode",Instance)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation);
        }
    }
}

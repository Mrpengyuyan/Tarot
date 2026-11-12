using System;
using System.Collections;
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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualPickFinishTests
    {
        private const BindingFlags Instance = BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private ApiClient previous;
        private Camera camera;
        private Transform canvas,backdrop;
        private SpreadFanController fan;
        private DeckController deck;
        private RitualPickPresentation owner;
        private ReadingFlowController flow;
        private RenderTexture target;
        private Sprite white;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous=ApiClient.Shared; ApiClient.ClearShared(); ReadingSessionStore.Clear();
            SceneManager.sceneLoaded+=Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return null;
            yield return null;
            canvas=GameObject.Find("ReadingRoomCanvas").transform;
            owner=canvas.GetComponent<RitualPickPresentation>();
            fan=Object.FindFirstObjectByType<SpreadFanController>();
            deck=Object.FindFirstObjectByType<DeckController>();
            flow=Object.FindFirstObjectByType<ReadingFlowController>();
            camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Where(c=>c.isActiveAndEnabled&&c.gameObject.scene==canvas.gameObject.scene)
                .OrderByDescending(c=>c.depth).First();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded-=Offline;
            if(camera!=null) camera.targetTexture=null;
            if(target!=null) { target.Release(); Object.Destroy(target); }
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("PickFinishCleanup"));
            if(scene.IsValid()&&scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            if(white!=null) Object.Destroy(white);
            ReadingSessionStore.Clear(); ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator All78CardsHaveOneSharedFinishWithoutReplacingPrintsOrPointerGeometry()
        {
            yield return Begin(3);
            var cards=fan.FanCards.ToArray();
            var finishes=cards.Select(Require).ToArray();
            Assert.That(finishes.SelectMany(f=>f.FrameRenderer.sharedMaterials).Distinct().Count(),Is.EqualTo(2));
            foreach(var size in new[] {new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(2560,1440)})
            {
                Resize(size);
                yield return null;
                yield return null;
                var d=size.x>=2000?2f:1f;
                Assert.That(fan.FanCards.ToArray(),Is.EqualTo(cards));
                for(var i=0;i<cards.Length;i++)
                {
                    var card=cards[i]; var finish=finishes[i];
                    Assert.That(finish.FrameRenderer.enabled,Is.True);
                    AssertStyle(finish,new Vector4(d,5*d,6*d,.6f),Gold("Fan"));
                    Assert.That(finish.FrameRenderer.sortingOrder,Is.EqualTo(Back(card).sortingOrder));
                    Assert.That(card.DrawData,Is.Null); Assert.That(card.IsFaceUp,Is.False);
                    Assert.That(Back(card).sprite.texture.name,Is.EqualTo("TarotCardBack"));
                    Assert.That(card.GetComponent<Collider>().gameObject.layer,Is.Zero);
                    Assert.That(finish.FrameRenderer.GetComponentsInChildren<Collider>(),Is.Empty);
                    Assert.That(finish.FrameRenderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.Off));
                    Assert.That(card.GetComponentsInChildren<Renderer>(true).Count(r=>r.enabled&&r.gameObject.activeInHierarchy),Is.EqualTo(2));
                }
            }
        }

        [UnityTest]
        public IEnumerator FanFlightAndLandedProfilesMatchLiteralReferencePixelsAtBothRatiosAndHd()
        {
            yield return Begin(3);
            HideFanForProbe();
            var card=Probe(0); var finish=Require(card);
            foreach(var size in new[] {new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(2560,1440),new Vector2Int(2048,1536)})
            {
                Resize(size);
                yield return null;
                var d=size.x>=2000?2f:1f;
                foreach(var stage in new[] {"Fan","Flying","Selected"})
                {
                    Phase(card,stage);
                    Pose(card,size.x/2f,size.y/2f,d);
                    Refresh(finish);
                    var pixels=Render();
                    try
                    {
                        var profile=Profile(stage,d);
                        var x=size.x/2; var y=size.y/2; var right=x+80*(int)d; var bottom=y-130*(int)d;
                        AssertStyle(finish,profile,Gold(stage));
                        AssertRgb(pixels.GetPixel(x,y),Color.white,.01f);
                        AssertRgb(pixels.GetPixel(right-1,y),Gold(stage),.025f);
                        var expectedBottom=stage=="Fan" ? .68f : stage=="Flying" ? .656f : .718f;
                        AssertRgb(pixels.GetPixel(x,bottom-Mathf.RoundToInt(profile.y)),Color.white*expectedBottom,.025f);
                        var expectedSide=stage=="Fan" ? .915f : stage=="Flying" ? .896f : .925f;
                        AssertRgb(pixels.GetPixel(right+Mathf.RoundToInt(profile.z),y),Color.white*expectedSide,.025f);
                        AssertRgb(pixels.GetPixel(right+Mathf.RoundToInt((Mathf.Abs(profile.y)+3*profile.z+12)),y),Color.white,.01f);
                    }
                    finally { Object.Destroy(pixels); }
                }
            }
        }

        [UnityTest]
        public IEnumerator FrontFanShadowCanShadeRearPaperButNeverItsOwnPrint()
        {
            yield return Begin(3);
            HideFanForProbe();
            var rear=Probe(0); var front=Probe(1);
            Resize(new Vector2Int(1280,720));
            yield return null;
            Pose(rear,640,360,1); Pose(front,720,360,1);
            Refresh(Require(rear)); Refresh(Require(front));
            var pixels=Render();
            try
            {
                AssertRgb(pixels.GetPixel(630,360),Color.white*.966f,.015f);
                AssertRgb(pixels.GetPixel(720,360),Color.white,.01f);
                Assert.That(Require(front).FrameRenderer.sortingOrder,Is.GreaterThan(Require(rear).FrameRenderer.sortingOrder));
            }
            finally { Object.Destroy(pixels); }
        }

        [UnityTest]
        public IEnumerator ActualFlightLandingAndFinalDockReuseTheSameMeshCardAndDraw()
        {
            yield return Begin(1);
            Resize(new Vector2Int(1280,720));
            yield return null;
            var card=fan.FanCards[58]; var finish=Require(card);
            var mesh=finish.FrameRenderer.GetComponent<MeshFilter>().sharedMesh;
            var material=Back(card).sharedMaterial;
            var pointer=card.GetComponent<Collider>();
            fan.RequestPick(card);
            yield return Until(()=>finish!=null&&finish.FrameRenderer!=null&&Mathf.Abs(Style(finish).y-16)<.01f);
            AssertStyle(finish,Profile("Flying",1),Gold("Flying"));
            Assert.That(finish.FrameRenderer.sortingOrder,Is.EqualTo(300));
            Assert.That(card.DrawData,Is.Null,"A flying back must not bind its draw ahead of the existing selection flow.");
            yield return Until(()=>deck.ActiveCards.Count==1&&Mathf.Abs(Style(finish).y-8)<.01f);
            AssertStyle(finish,Profile("Selected",1),Gold("Selected"));
            Assert.That(card.DrawData,Is.Null);
            yield return Until(()=>flow.State==ReadingFlowState.WaitingForFlip&&finish.FrameRenderer.enabled);
            AssertStyle(finish,new Vector4(1,12,8,.6f),new Color(181f/255f,139f/255f,74f/255f));
            Assert.That(deck.ActiveCards[0],Is.SameAs(card));
            var draw=card.DrawData;
            Assert.That(draw,Is.Not.Null);
            Refresh(finish);
            yield return null;
            Assert.That(card.DrawData,Is.SameAs(draw));
            Assert.That(card.GetComponent<Collider>(),Is.SameAs(pointer));
            Assert.That(Back(card).sharedMaterial,Is.SameAs(material));
            Assert.That(finish.FrameRenderer.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(mesh));
            Assert.That(card.GetComponents<RitualPaperFinish>().Length,Is.EqualTo(1));
            Assert.That(card.GetComponentsInChildren<Renderer>(true).Count(r=>r.name=="RitualPaperFinishMesh"),Is.EqualTo(1));
            Assert.That(finish.FrameRenderer.sortingOrder,Is.Zero);
        }

        [UnityTest]
        public IEnumerator SourceOpacityAndHoverPriorityReachOnlyTheOwnedFinishingMesh()
        {
            yield return Begin(3);
            HideFanForProbe();
            var card=Probe(0); var finish=Require(card); var source=Back(card);
            var material=source.sharedMaterial;
            Resize(new Vector2Int(1280,720));
            yield return null;
            Phase(card,"Selected"); Pose(card,640,360,1);
            source.color=new Color(1,1,1,.5f); source.sortingOrder=300;
            Refresh(finish);
            Assert.That(Style(finish).w,Is.EqualTo(4f/15f).Within(.001f));
            var block=new MaterialPropertyBlock(); finish.FrameRenderer.GetPropertyBlock(block);
            Assert.That(block.GetVector("_Stroke").w,Is.EqualTo(.5f).Within(.001f));
            var pixels=Render();
            try
            {
                AssertRgb(pixels.GetPixel(719,360),Color.Lerp(Color.white,Gold("Selected").linear,.5f).gamma,.025f);
                AssertRgb(pixels.GetPixel(640,222),Color.white*.86f,.025f);
                AssertRgb(pixels.GetPixel(640,360),Color.white,.01f);
            }
            finally { Object.Destroy(pixels); }
            Phase(card,"Fan"); source.color=new Color(1.25f,1.25f,1.25f,1); source.sortingOrder=200;
            Refresh(finish); finish.FrameRenderer.GetPropertyBlock(block);
            Assert.That(Vector3.Distance((Vector3)block.GetVector("_Stroke"),(Vector3)(Vector4)(Gold("Fan").linear*1.25f)),Is.LessThan(.001f));
            Assert.That(finish.FrameRenderer.sortingOrder,Is.EqualTo(200));
            Assert.That(source.sharedMaterial,Is.SameAs(material));
            Assert.That(source.color,Is.EqualTo(new Color(1.25f,1.25f,1.25f,1)));
        }

        [UnityTest]
        public IEnumerator FanRebuildAndOwnerRemovalKeepRealCardsAndReleaseDestroyedCardMeshes()
        {
            yield return Begin(3);
            var chosen=fan.FanCards[58]; Require(chosen);
            fan.RequestPick(chosen);
            yield return Until(()=>deck.ActiveCards.Count==1);
            var props=fan.FanCards.ToArray();
            var meshes=props.Select(c=>Require(c).FrameRenderer.GetComponent<MeshFilter>().sharedMesh).ToArray();
            fan.gameObject.SetActive(false);
            yield return null; yield return null;
            Assert.That(meshes.All(m=>m==null),Is.True);
            Assert.That(Require(chosen).FrameRenderer.enabled,Is.False);
            fan.gameObject.SetActive(true);
            yield return null; yield return null;
            Assert.That(props.All(c=>Require(c).FrameRenderer.enabled),Is.True);
            Assert.That(Require(chosen).FrameRenderer.enabled,Is.True);
            var all=props.Concat(new[] {chosen}).ToArray();
            meshes=all.Select(c=>Require(c).FrameRenderer.GetComponent<MeshFilter>().sharedMesh).ToArray();
            canvas.GetComponent<ReadingRoomController>().StopAllCoroutines();
            Object.Destroy(owner);
            yield return null; yield return null;
            Assert.That(chosen!=null,Is.True);
            Assert.That(deck.ActiveCards[0],Is.SameAs(chosen));
            Assert.That(all.All(c=>!Require(c).FrameRenderer.enabled),Is.True);
            fan.Abandon(); deck.Clear();
            yield return null; yield return null;
            Assert.That(meshes.All(m=>m==null),Is.True);
        }

        private IEnumerator Begin(int count)
        {
            canvas.Find(count==1?"OneCardButton":"ThreeCardButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text="An offline pick finish check";
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return Until(()=>fan.AcceptingPicks);
        }

        private void HideFanForProbe()
        {
            foreach(var card in fan.FanCards) card.gameObject.SetActive(false);
            canvas.GetComponent<Canvas>().enabled=false;
            white=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.one*.5f,1);
            backdrop=new GameObject("PickFinishWhiteBackdrop",typeof(SpriteRenderer)).transform;
            var source=backdrop.GetComponent<SpriteRenderer>(); source.sprite=white; source.sortingOrder=-10000;
            backdrop.gameObject.layer=LayerMask.NameToLayer("RitualPaper");
        }

        private CardView Probe(int index)
        {
#if UNITY_EDITOR
            var card=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/PF_TarotCard.prefab")).GetComponent<CardView>();
            card.Bind(LocalReadingSimulator.CreatePlaceholderDraws(1)[0]); card.SetFaceUp(false);
            owner.PrepareFanCard(card,index);
            Require(card);
            var back=Back(card); back.sprite=white;
            back.transform.localPosition=Vector3.zero; back.transform.localScale=new Vector3(.64f,1,1);
            return card;
#else
            Assert.Ignore("Requires the real prefab in the editor."); return null;
#endif
        }

        private void Resize(Vector2Int size)
        {
            var old=target; target=new RenderTexture(size.x,size.y,24);
            camera.targetTexture=target; camera.aspect=(float)size.x/size.y;
            if(old!=null) { old.Release(); Object.Destroy(old); }
            if(backdrop==null) return;
            backdrop.SetPositionAndRotation(camera.ViewportToWorldPoint(new Vector3(.5f,.5f,3.5f)),camera.transform.rotation);
            var h=7f*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f);
            backdrop.localScale=new Vector3(h*camera.aspect*2,h*2,1);
        }

        private void Pose(CardView card,float x,float y,float density)
        {
            var unit=5f*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/camera.pixelHeight;
            card.transform.SetPositionAndRotation(camera.ScreenToWorldPoint(new Vector3(x,y,2.5f)),camera.transform.rotation*Quaternion.Euler(-90,0,0));
            card.transform.localScale=new Vector3(160*density*unit/.64f,1,260*density*unit);
        }

        private void Phase(CardView card,string phase)
        {
            var states=(IDictionary)typeof(RitualPickPresentation).GetField("ink",Instance).GetValue(owner);
            var value=states[card]; var field=value.GetType().GetField("Stage",Instance);
            Assert.That(field,Is.Not.Null,"The presentation must distinguish fan, flight and landed finishing.");
            field.SetValue(value,Enum.Parse(field.FieldType,phase));
        }

        private static RitualPaperFinish Require(CardView card)
        {
            var finish=card.GetComponent<RitualPaperFinish>();
            Assert.That(finish,Is.Not.Null,"The real pick card is missing its native border/shadow finish.");
            return finish;
        }
        private static void Refresh(RitualPaperFinish finish)=>typeof(RitualPaperFinish).GetMethod("Refresh",Instance).Invoke(finish,null);
        private static SpriteRenderer Back(CardView card)=>card.GetComponent<RitualPaperSurface>().Back;
        private static Vector4 Style(RitualPaperFinish finish)
        { var block=new MaterialPropertyBlock(); finish.FrameRenderer.GetPropertyBlock(block); return block.GetVector("_Style"); }
        private static Vector4 Profile(string stage,float d)=>stage=="Fan"?new Vector4(d,5*d,6*d,.6f)
            :stage=="Flying"?new Vector4(d,16*d,12.5f*d,10f/15f):new Vector4(d,8*d,7*d,8f/15f);
        private static Color Gold(string stage)=>stage=="Fan"?new Color(186f/255f,137f/255f,67f/255f)
            :stage=="Flying"?new Color(208f/255f,178f/255f,120f/255f):new Color(192f/255f,161f/255f,104f/255f);
        private static void AssertStyle(RitualPaperFinish finish,Vector4 expected,Color gold)
        {
            Assert.That(Vector4.Distance(Style(finish),expected),Is.LessThan(.001f));
            var block=new MaterialPropertyBlock(); finish.FrameRenderer.GetPropertyBlock(block);
            var ink=block.GetVector("_Stroke"); var color=gold.linear;
            Assert.That(Vector3.Distance((Vector3)ink,new Vector3(color.r,color.g,color.b)),Is.LessThan(.001f));
        }
        private static void AssertRgb(Color actual,Color expected,float tolerance)
        { Assert.That(actual.r,Is.EqualTo(expected.r).Within(tolerance)); Assert.That(actual.g,Is.EqualTo(expected.g).Within(tolerance)); Assert.That(actual.b,Is.EqualTo(expected.b).Within(tolerance)); }

        private Texture2D Render()
        {
            var old=RenderTexture.active;
            try
            {
                var request=new RenderPipeline.StandardRequest { destination=target };
                for(var i=0;i<4;i++) RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;
                var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0); pixels.Apply(); return pixels;
            }
            finally { RenderTexture.active=old; }
        }
        private static IEnumerator Until(Func<bool> ready)
        { var deadline=Time.realtimeSinceStartup+40; while(!ready()) { Assert.That(Time.realtimeSinceStartup,Is.LessThan(deadline)); yield return null; } }
        private static void Offline(Scene scene,LoadSceneMode mode)
        { if(scene.name=="ReadingRoom") typeof(ReadingRoomController).GetField("backendMode",Instance).SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation); }
    }
}

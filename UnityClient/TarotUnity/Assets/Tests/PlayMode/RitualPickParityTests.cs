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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualPickParityTests
    {
        private ApiClient previous;
        private Camera camera;
        private RenderTexture target;
        private Transform canvas;
        private SpreadFanController fan;
        private DeckController deck;
        private Component adapter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous=ApiClient.Shared;ApiClient.ClearShared();ReadingSessionStore.Clear();
            SceneManager.sceneLoaded+=Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");yield return null;yield return null;
            canvas=GameObject.Find("ReadingRoomCanvas").transform;
            fan=Object.FindFirstObjectByType<SpreadFanController>();deck=Object.FindFirstObjectByType<DeckController>();
            camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled&&c.gameObject.scene==canvas.gameObject.scene).OrderByDescending(c=>c.depth).First();
            var type=typeof(RitualReadingView).Assembly.GetType("TarotUnity.Presentation.RitualPickPresentation");
            Assert.That(type,Is.Not.Null,"The approved pick phase needs a real-card presentation adapter.");
            adapter=canvas.GetComponent(type);
            Assert.That(adapter,Is.Not.Null);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded-=Offline;ReadingSessionStore.Clear();
            if(camera!=null)camera.targetTexture=null;
            if(target!=null){target.Release();Object.Destroy(target);}
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("PickParityCleanup"));
            if(scene.IsValid()&&scene.isLoaded)yield return SceneManager.UnloadSceneAsync(scene);
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator All78CardsAndTheRealPickedCardMatchTheReferenceThroughResize()
        {
            yield return Begin(3);
            var original=fan.FanCards.ToArray();
            foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(2560,1440),new Vector2Int(2048,1536),new Vector2Int(1280,720)})
            {
                yield return Resize(size);
                var frame=Frame(3,size);var poses=(Array)frame.GetType().GetField("Fan").GetValue(frame);
                Assert.That(fan.FanCards.Count,Is.EqualTo(78));
                for(var i=0;i<78;i++)
                {
                    var card=original[i];var back=Back(card);
                    Assert.That(card.IsFaceUp,Is.False);Assert.That(card.DrawData,Is.Null);
                    AssertRect(Bounds(back),Read<Rect>(poses.GetValue(i),"Bounds"));
                    Assert.That(Reachable(card,Bounds(back)),Is.True,$"card {i} needs a real visible pointer strip at {size}");
                }
            }
            var chosen=fan.FanCards[58];
            fan.RequestPick(chosen);yield return Until(()=>deck.ActiveCards.Count==1);
            yield return new WaitForSeconds(0.35f);
            foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,720)})
            {
                yield return Resize(size);var poses=(Array)Frame(3,size).GetType().GetField("Selected").GetValue(Frame(3,size));
                Assert.That(deck.ActiveCards[0],Is.SameAs(chosen));
                Assert.That(chosen.GetComponentsInChildren<SpriteRenderer>(true).Count(r=>r.name=="RitualPaperBack"),Is.EqualTo(1));
                AssertRect(Bounds(Back(chosen)),Read<Rect>(poses.GetValue(0),"Bounds"));
            }
        }

        [UnityTest]
        public IEnumerator NativeFlightQueueHoldAndFinalSpreadKeepThoseSameCards()
        {
            yield return Begin(3);
            var a=fan.FanCards[58];var b=fan.FanCards[59];var c=fan.FanCards[60];
            var began=-1f;var landed=-1f;
            deck.CardFlightStarted+=(card,seconds)=>{if(card==a){began=Time.time;Assert.That(seconds,Is.EqualTo(0.9f).Within(0.001f));}};
            deck.CardDealt+=card=>{if(card==a)landed=Time.time;};
            fan.RequestPick(a);yield return Until(()=>began>=0f);
            fan.RequestPick(b);fan.RequestPick(c);
            yield return Until(()=>deck.ActiveCards.Count==2);
            Assert.That(landed-began,Is.EqualTo(0.9f).Within(0.1f));
            Assert.That(deck.ActiveCards[0],Is.SameAs(a));Assert.That(deck.ActiveCards[1],Is.SameAs(b));
            Assert.That(fan.FanCards,Does.Contain(c),"Only one click queues while a card is flying.");
            fan.RequestPick(c);yield return Until(()=>deck.ActiveCards.Count==3);
            var holdStart=Time.time;
            yield return new WaitForSeconds(0.6f);
            yield return Resize(new Vector2Int(1024,768));
            var position=c.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.That(Vector3.Distance(position,c.transform.position),Is.LessThan(0.03f),"No snap back during the full-selection hold.");
            var flow=Object.FindFirstObjectByType<ReadingFlowController>();
            yield return Until(()=>flow.State==ReadingFlowState.WaitingForFlip);
            Assert.That(Time.time-holdStart,Is.GreaterThanOrEqualTo(1.8f),"Full hold and transfer remain visible.");
            Assert.That(deck.ActiveCards.ToArray(),Is.EqualTo(new[]{a,b,c}));
            foreach(var card in deck.ActiveCards)
            {
                Assert.That(card.GetComponentsInChildren<SpriteRenderer>(true).Count(r=>r.name=="RitualPaperBack"),Is.EqualTo(1));
                Assert.That(card.DrawData,Is.Not.Null);
            }
        }

        [UnityTest]
        public IEnumerator FinalSpreadBacksCannotCoverTheInspectedFace()
        {
            yield return Begin(10);
            for(var i=0;i<10;i++)
            {
                fan.RequestPick(fan.FanCards[fan.FanCards.Count/2]);
                var picked=i+1;yield return Until(()=>deck.ActiveCards.Count==picked);
            }
            yield return Until(()=>Object.FindFirstObjectByType<ReadingFlowController>().State==ReadingFlowState.WaitingForFlip);
            var inspected=deck.ActiveCards[1];inspected.SetFaceUp(true);
            Assert.That(Object.FindFirstObjectByType<CardInspectionController>().Show(inspected,false),Is.True);
            foreach(var card in deck.ActiveCards.Where(c=>c!=inspected))
                Assert.That(Back(card).sortingOrder,Is.LessThan(inspected.FaceArtwork.sortingOrder),
                    "Flight priority must not paint a distant back over an inspected face.");
        }

        [UnityTest]
        public IEnumerator DisablingAndDestroyingTheFanOnlyReleasesUnpickedProps()
        {
            yield return Begin(3);
            var chosen=fan.FanCards[58];fan.RequestPick(chosen);yield return Until(()=>deck.ActiveCards.Count==1);
            var props=fan.FanCards.ToArray();
            fan.gameObject.SetActive(false);yield return null;
            Assert.That(props.All(c=>!c.gameObject.activeInHierarchy),Is.True);
            fan.RequestPick(props[0]);yield return new WaitForSeconds(1.1f);
            Assert.That(deck.ActiveCards.Count,Is.EqualTo(1));
            fan.gameObject.SetActive(true);yield return null;
            Assert.That(props.All(c=>c.gameObject.activeInHierarchy),Is.True);
            canvas.GetComponent<ReadingRoomController>().StopAllCoroutines();
            Object.Destroy(fan.gameObject);yield return null;yield return null;
            Assert.That(props.All(c=>c==null),Is.True);
            Assert.That(chosen!=null,Is.True);Assert.That(deck.ActiveCards[0],Is.SameAs(chosen));
        }

        [UnityTest]
        public IEnumerator ClearingStagingDuringFlightAbortsInsteadOfReportingALanding()
        {
            yield return Begin(1);
            var card=fan.FanCards[58];var flying=false;var dealt=0;
            deck.CardFlightStarted+=(c,_)=>flying=true;deck.CardDealt+=_=>dealt++;
            fan.RequestPick(card);yield return Until(()=>flying);
            LogAssert.Expect(LogType.Exception,new System.Text.RegularExpressions.Regex("InvalidOperationException: The picked-card flight was interrupted"));
            canvas.GetComponent<PickedCardStaging>().Clear();
            var flow=Object.FindFirstObjectByType<ReadingFlowController>();
            yield return Until(()=>flow.State!=ReadingFlowState.Drawing);
            Assert.That(flow.State,Is.EqualTo(ReadingFlowState.ReadyToDraw));
            Assert.That(dealt,Is.EqualTo(0));Assert.That(deck.ActiveCards.Count,Is.Zero);
            yield return null;Assert.That(card==null,Is.True,"An interrupted, unregistered card must not leak beside the fan.");
        }

        [UnityTest]
        public IEnumerator NativeGuidesHaveVisibleInkAndCannotBlockTheFan()
        {
            yield return Begin(3);yield return Resize(new Vector2Int(1280,720));
            var guides=canvas.GetComponentsInChildren<RitualPickGuideGraphic>();
            Assert.That(guides.Length,Is.EqualTo(6));
            Canvas.ForceUpdateCanvases();camera.Render();
            foreach(var guide in guides)
            {
                Assert.That(guide.raycastTarget,Is.False);
                var mesh=guide.canvasRenderer.GetMesh();
                Assert.That(mesh.vertexCount,Is.GreaterThan(0),guide.name);
                Assert.That(guide.canvasRenderer.cull,Is.False,guide.name);
                Assert.That(guide.canvasRenderer.GetAlpha(),Is.GreaterThan(0.1f),guide.name);
            }
        }

        [UnityTest]
        public IEnumerator ClearingTheRailAndRemovingTheAdapterDoesNotDeleteDeckOwnedCards()
        {
            yield return Begin(3);var chosen=fan.FanCards[58];fan.RequestPick(chosen);
            yield return Until(()=>deck.ActiveCards.Count==1);
            canvas.GetComponent<ReadingRoomController>().StopAllCoroutines();
            canvas.GetComponent<PickedCardStaging>().Clear();yield return null;
            Assert.That(chosen!=null,Is.True);Assert.That(deck.ActiveCards[0],Is.SameAs(chosen));
            Object.Destroy(adapter);yield return null;
            Assert.That(chosen!=null,Is.True);
            fan.Abandon();deck.Clear();yield return null;yield return null;
            Assert.That(GameObject.Find("RitualPickWorld"),Is.Null);
        }

        private IEnumerator Begin(int count)
        {
            canvas.Find(count==1?"OneCardButton":count==3?"ThreeCardButton":"CelticCrossButton").GetComponent<Button>().onClick.Invoke();
            canvas.Find("QuestionInput").GetComponent<TMP_InputField>().text="A pick parity question";
            canvas.Find("DrawButton").GetComponent<Button>().onClick.Invoke();
            yield return Until(()=>fan.AcceptingPicks);
        }
        private IEnumerator Resize(Vector2Int size)
        {
            var next=new RenderTexture(size.x,size.y,24);camera.targetTexture=next;camera.aspect=(float)size.x/size.y;
            if(target!=null){target.Release();Object.Destroy(target);}target=next;
            var root=canvas.GetComponent<Canvas>();root.renderMode=RenderMode.ScreenSpaceCamera;root.worldCamera=camera;root.planeDistance=camera.nearClipPlane+0.05f;
            yield return null;adapter.GetType().GetMethod("RefreshLayout").Invoke(adapter,null);
            Canvas.ForceUpdateCanvases();Physics.SyncTransforms();
        }
        private object Frame(int count,Vector2Int size)=>typeof(RitualReadingView).Assembly.GetType("TarotUnity.UI.RitualPickLayout").GetMethod("Compute").Invoke(null,new object[]{count,(float)size.x,(float)size.y});
        private static T Read<T>(object o,string key)=>(T)(o.GetType().GetField(key)?.GetValue(o)??o.GetType().GetProperty(key)?.GetValue(o));
        private static SpriteRenderer Back(CardView card)=>card.GetComponentsInChildren<SpriteRenderer>(true).Single(r=>r.name=="RitualPaperBack");
        private Rect Bounds(SpriteRenderer image)
        {
            var min=Vector2.one*float.PositiveInfinity;var max=Vector2.one*float.NegativeInfinity;
            foreach(var v in image.sprite.vertices){var p=camera.WorldToScreenPoint(image.transform.TransformPoint(v));var point=new Vector2(p.x,camera.pixelHeight-p.y);min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
            return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
        }
        private bool Reachable(CardView card,Rect rect)
        {
            for(var y=0;y<16;y++)for(var x=0;x<16;x++)
            {
                var ray=camera.ScreenPointToRay(new Vector3(rect.x+(x+0.5f)*rect.width/16f,camera.pixelHeight-rect.y-(y+0.5f)*rect.height/16f));
                if(Physics.Raycast(ray,out var hit,100f)&&hit.collider.GetComponentInParent<CardView>()==card)return true;
            }
            return false;
        }
        private static void AssertRect(Rect a,Rect b){Assert.That(a.x,Is.EqualTo(b.x).Within(1f));Assert.That(a.y,Is.EqualTo(b.y).Within(1f));Assert.That(a.width,Is.EqualTo(b.width).Within(1f));Assert.That(a.height,Is.EqualTo(b.height).Within(1f));}
        private static IEnumerator Until(Func<bool> predicate){var end=Time.realtimeSinceStartup+45f;while(!predicate()){Assert.That(Time.realtimeSinceStartup,Is.LessThan(end));yield return null;}}
        private static void Offline(Scene scene,LoadSceneMode mode){if(scene.name=="ReadingRoom")typeof(ReadingRoomController).GetField("backendMode",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation);}
    }
}

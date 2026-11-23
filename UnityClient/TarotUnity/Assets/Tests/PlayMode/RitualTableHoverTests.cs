using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.Presentation;
using TarotUnity.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualTableHoverTests
    {
        private ApiClient previous;
        private Camera camera;
        private ReadingFlowController flow;
        private DeckController deck;
        private RitualSpreadPresentation spread;
        private RenderTexture target;
        private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous=ApiClient.Shared; ApiClient.ClearShared(); ReadingSessionStore.Clear();
            SceneManager.sceneLoaded+=Offline;
            yield return SceneManager.LoadSceneAsync("ReadingRoom");
            yield return new WaitForSeconds(.75f);
            camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled).OrderByDescending(c=>c.depth).First();
            flow=Object.FindFirstObjectByType<ReadingFlowController>();
            deck=Object.FindFirstObjectByType<DeckController>();
            spread=Object.FindFirstObjectByType<RitualSpreadPresentation>();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded-=Offline;
            Object.FindFirstObjectByType<CardInspectionController>()?.CloseImmediate();
            if(camera!=null) camera.targetTexture=null;
            if(target!=null) { target.Release(); Object.Destroy(target); }
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("TableHoverCleanup"));
            if(scene.IsValid()&&scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ReadingSessionStore.Clear(); ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator RealBacksAndReversedFacesFollowNativeHoverWithoutMovingTheCardRootOrRedrawing()
        {
            foreach(var count in new[]{1,3,10})
            {
                yield return Prepare(count);
                var card=deck.ActiveCards[0];
                var draw=card.DrawData;
                var parent=card.transform.parent;
                var collider=card.GetComponent<Collider>();
                var material=card.GetComponent<RitualPaperSurface>().Back.sharedMaterial;
                var original=parent.localPosition;
                Enter(card);
                yield return new WaitForSeconds(.4f);
                var expected=Quaternion.Euler(count==10?12f:8f,0,0)*Quaternion.Euler(-90,0,0);
                Assert.That(Quaternion.Angle(parent.localRotation,expected),Is.LessThan(.1f));
                Assert.That(card.transform.localPosition,Is.EqualTo(Vector3.zero));
                Assert.That(card.transform.localRotation,Is.EqualTo(Quaternion.identity));
                Assert.That(card.GetComponent<RitualPaperSurface>().Back.color.r,Is.EqualTo(1.1f).Within(.003f));
                if(count==10) Assert.That(parent.localPosition,Is.EqualTo(original));
                else Assert.That(parent.localPosition.y,Is.GreaterThan(original.y+1));
                Exit(card);
                yield return new WaitForSeconds(.4f);
                Assert.That(Vector3.Distance(parent.localPosition,original),Is.LessThan(.01f));
                card.DrawData.is_reversed=true;
                card.SetFaceUp(true);
                Enter(card);
                yield return new WaitForSeconds(.4f);
                Assert.That(card.FaceArtwork.color.r,Is.EqualTo(1.1f).Within(.003f));
                Assert.That(card.ShowsReversed,Is.True);
                Assert.That(card.DrawData,Is.SameAs(draw));
                Assert.That(card.GetComponent<Collider>(),Is.SameAs(collider));
                Assert.That(card.GetComponent<RitualPaperSurface>().Back.sharedMaterial,Is.SameAs(material));
                Assert.That(card.GetComponentsInChildren<Renderer>(true).Count(r=>r.name=="RitualPaperFinishMesh"),Is.EqualTo(1));
                Exit(card);
                yield return new WaitForSeconds(.4f);
                Assert.That(card.FaceArtwork.color,Is.EqualTo(Color.white));
            }
        }

        [UnityTest]
        public IEnumerator HoverCannotFightTheRealFlipOrTintAnInspectedCardDuringResize()
        {
            yield return Prepare(3);
            var card=deck.ActiveCards[0];
            var draw=card.DrawData;
            Enter(card);
            yield return new WaitForSeconds(.4f);
            var parent=card.transform.parent;
            card.GetComponent<CardFlipController>().Flip(card);
            yield return null;
            yield return null;
            Assert.That(Quaternion.Angle(parent.localRotation,Quaternion.Euler(18,0,-4)*Quaternion.Euler(-90,0,0)),Is.LessThan(.1f));
            var inspector=Object.FindFirstObjectByType<CardInspectionController>();
            var until=Time.realtimeSinceStartup+10;
            while(card.GetComponent<CardFlipController>().IsFlipping || inspector.IsOpen)
            { Assert.That(Time.realtimeSinceStartup,Is.LessThan(until)); yield return null; }
            card.SetFaceUp(true);
            Enter(card);
            yield return new WaitForSeconds(.4f);
            Assert.That(card.FaceArtwork.color.r,Is.EqualTo(1.1f).Within(.003f));
            Assert.That(inspector.Show(card,false),Is.True);
            yield return new WaitForSeconds(.4f);
            foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(2560,1440),new Vector2Int(1280,720)})
            {
                var old=target;
                target=new RenderTexture(size.x,size.y,24);
                camera.targetTexture=target; camera.aspect=(float)size.x/size.y;
                if(old!=null) { old.Release(); Object.Destroy(old); }
                yield return null;
                Assert.That(card.FaceArtwork.color,Is.EqualTo(Color.white));
                var center=camera.WorldToViewportPoint(card.FaceArtwork.transform.TransformPoint(card.FaceArtwork.sprite.bounds.center));
                Assert.That(center.x,Is.EqualTo(.5f).Within(.003f));
                Assert.That(center.y,Is.EqualTo(.5f).Within(.003f));
            }
            inspector.CloseImmediate();
            Exit(card);
            yield return new WaitForSeconds(.45f);
            Assert.That(card.transform.localPosition,Is.EqualTo(Vector3.zero));
            Assert.That(card.transform.localRotation,Is.EqualTo(Quaternion.identity));
            Assert.That(card.FaceArtwork.color,Is.EqualTo(Color.white));
            Assert.That(card.DrawData,Is.SameAs(draw));
            Assert.That(card.ShowsReversed,Is.EqualTo(draw.is_reversed));
        }

        private IEnumerator Prepare(int count)
        {
            deck.Clear();
            yield return null;
            flow.EnterSpreadSelect(); flow.SelectSpread(count,count);
            flow.SetQuestion("An offline native hover test","general");
            spread.Prepare(count);
            flow.BeginShuffle(); flow.BeginDeal(); flow.WaitForCardFlips();
            yield return deck.DealCards(LocalReadingSimulator.CreatePlaceholderDraws(count),flow.GetSelectedSpreadSlots());
            yield return new WaitForSeconds(.7f);
        }
        private static void Enter(CardView card)=>ExecuteEvents.Execute(card.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
        private static void Exit(CardView card)=>ExecuteEvents.Execute(card.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
        private static void Offline(Scene scene,LoadSceneMode mode)
        {
            if(scene.name=="ReadingRoom") typeof(ReadingRoomController).GetField("backendMode",Instance)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation);
        }
    }
}

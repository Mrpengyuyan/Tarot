using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TarotUnity.Data;
using TarotUnity.Gameplay;
using TarotUnity.Network;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace TarotUnity.Tests.PlayMode
{
    public sealed class RitualControlFeedbackTests
    {
        private ApiClient previous;
        private Canvas canvas;
        private Camera camera;
        private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static readonly Vector2Int[] Sizes={new(1280,720),new(1024,768),new(2560,1440),new(2048,1536)};

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            previous=ApiClient.Shared;
            ApiClient.ClearShared();
            ReadingSessionStore.Clear();
            SceneManager.sceneLoaded+=Offline;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SceneManager.sceneLoaded-=Offline;
            var scene=SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("ControlFeedbackCleanup"));
            if(scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            ReadingSessionStore.Clear();
            ApiClient.SetShared(previous);
        }

        [UnityTest]
        public IEnumerator PrimaryHoverMatchesInkLiftAndRulesWithoutMovingItsHitTargetOrDrifting()
        {
            yield return Load("MainMenu");
            var button=Button("StartReadingButton");
            RequireFeedback(button);
            var label=button.GetComponentInChildren<TMP_Text>();
            var listeners=button.onClick;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            foreach(var size in Sizes)
            using(var frame=new RitualFrameCapture(camera,canvas,size.x,size.y))
            {
                canvas.scaleFactor = 1f;
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;
                var root=(RectTransform)button.transform;
                var rootPosition=root.anchoredPosition;
                var labelPosition=label.rectTransform.anchoredPosition;
                var density=size.x>2000?2f:1f;
                for(var cycle=0;cycle<2;cycle++)
                {
                    Enter(button.gameObject);
                    canvas.GetComponent<TarotUiTheme>().Apply();
                    yield return new WaitForSecondsRealtime(.24f);
                    AssertRgb(label.color,new Color(1,1,206.25f/255),.006f);
                    Assert.That(label.rectTransform.anchoredPosition.y-labelPosition.y,Is.EqualTo(2*density).Within(.15f));
                    Assert.That(root.anchoredPosition,Is.EqualTo(rootPosition));
                    foreach(var key in new[]{"RitualActionRuleTop","RitualActionRuleBottom"})
                    {
                        var rule=button.transform.Find(key).GetComponent<Graphic>();
                        Assert.That(rule.color.a,Is.EqualTo(1).Within(.005f));
                        Assert.That(rule.transform.localScale.x,Is.EqualTo(1.08f).Within(.002f));
                        Assert.That(rule.raycastTarget,Is.False);
                    }
                    AssertPointer(button);
                    Exit(button.gameObject);
                    yield return new WaitForSecondsRealtime(.24f);
                    Assert.That(Vector2.Distance(label.rectTransform.anchoredPosition,labelPosition),Is.LessThan(.1f));
                    AssertRgb(label.color,new Color(240f/255,214f/255,165f/255),.003f);
                }
                Assert.That(button.onClick,Is.SameAs(listeners));
            }
        }

        [UnityTest]
        public IEnumerator KeyboardFocusIsVisibleButPointerSelectionDoesNotLeaveAFocusRing()
        {
            yield return Load("MainMenu");
            var button=Button("QuitButton");
            RequireFeedback(button);
            var label=button.GetComponentInChildren<TMP_Text>();
            Enter(button.gameObject);
            yield return null;
            AssertRgb(label.color,new Color(246f/255,221f/255,176f/255),.004f);
            Exit(button.gameObject);
            EventSystem.current.SetSelectedGameObject(button.gameObject,new PointerEventData(EventSystem.current));
            yield return null;
            Assert.That(VisibleInk(button,"RitualFocusRing"),Is.False);
            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject,new BaseEventData(EventSystem.current));
            yield return null;
            var ring=button.transform.Find("RitualFocusRing").GetComponent<Graphic>();
            Assert.That(VisibleInk(button,"RitualFocusRing"),Is.True);
            Assert.That(ring.raycastTarget,Is.False);
            AssertRgb(ring.color,new Color(240f/255,213f/255,160f/255),.004f);
            ExecuteEvents.Execute(button.gameObject,new AxisEventData(EventSystem.current){moveDir=MoveDirection.Up,moveVector=Vector2.up},ExecuteEvents.moveHandler);
            yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject,Is.EqualTo(Button("StartReadingButton").gameObject));
            Assert.That(VisibleInk(Button("StartReadingButton"),"RitualFocusRing"),Is.True);
            Assert.That(VisibleInk(button,"RitualFocusRing"),Is.False);
            AssertPointer(Button("StartReadingButton"));
        }

        [UnityTest]
        public IEnumerator SpreadHoverAndQuestionFocusUseNativeInkAndDisabledActionsStayQuiet()
        {
            yield return Load("ReadingRoom");
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            using(var frame=new RitualFrameCapture(camera,canvas,1280,720))
            {
                canvas.scaleFactor = 1f;
                yield return null;
                Canvas.ForceUpdateCanvases();
                yield return null;
                var button=Button("ThreeCardButton");
                RequireFeedback(button);
                var symbol=(RectTransform)button.transform.Find("RitualSpreadSymbol");
                var rest=symbol.anchoredPosition;
                Enter(button.gameObject);
                yield return new WaitForSecondsRealtime(.45f);
                Assert.That(symbol.anchoredPosition.y-rest.y,Is.EqualTo(718*.014f).Within(.25f));
                var halo=button.transform.Find("RitualSpreadHalo").GetComponent<Graphic>();
                Assert.That(halo.color.a,Is.EqualTo(28f/255).Within(.005f));
                Assert.That(halo.raycastTarget,Is.False);
                button.onClick.Invoke();
                yield return new WaitForSecondsRealtime(.7f);
                Assert.That(halo.color.a,Is.Zero.Within(.001f));
                var input=canvas.transform.Find("QuestionInput").GetComponent<TMP_InputField>();
                input.ActivateInputField();
                yield return null;
                yield return null;
                var underline=input.transform.Find("RitualInputFocus").GetComponent<Graphic>();
                Assert.That(underline.color.a,Is.GreaterThan(.99f));
                Assert.That(underline.raycastTarget,Is.False);
                AssertRgb(underline.color,new Color(229f/255,195f/255,136f/255),.004f);
                var draw=Button("DrawButton");
                var label=draw.GetComponentInChildren<TMP_Text>();
                var position=label.rectTransform.anchoredPosition;
                Enter(draw.gameObject);
                EventSystem.current.SetSelectedGameObject(draw.gameObject,new BaseEventData(EventSystem.current));
                yield return new WaitForSecondsRealtime(.3f);
                Assert.That(draw.interactable,Is.False);
                Assert.That(label.rectTransform.anchoredPosition,Is.EqualTo(position));
                Assert.That(VisibleInk(draw,"RitualFocusRing"),Is.False);
                Assert.That(draw.GetComponent<CanvasGroup>().alpha,Is.EqualTo(.35f).Within(.005f));
                input.text="A valid question";
                yield return new WaitForSecondsRealtime(.25f);
                Assert.That(draw.interactable,Is.True);
                AssertRgb(label.color,new Color(1,1,206.25f/255),.006f);
            }
        }

        [UnityTest]
        public IEnumerator PhaseEntranceFadesAndRisesThenSettlesWithoutLosingQuestionOrInputGating()
        {
            yield return Load("ReadingRoom");
            var first=Button("OneCardButton");
            var input=canvas.transform.Find("QuestionInput").GetComponent<TMP_InputField>();
            input.text="保留我的问题";
            var expected=RitualStaticLayout.Compute("question",((RectTransform)canvas.transform).rect.width,
                ((RectTransform)canvas.transform).rect.height)["QuestionInput"].Rect;
            var root=(RectTransform)canvas.transform;
            var settledY=root.rect.height*.5f-expected.center.y;
            first.onClick.Invoke();
            yield return null;
            var group=input.GetComponent<CanvasGroup>();
            Assert.That(group.alpha,Is.InRange(.001f,.5f),"A new phase must not instantly pop to fully opaque.");
            Assert.That(first.GetComponent<CanvasGroup>().blocksRaycasts,Is.False);
            Assert.That(input.GetComponent<RectTransform>().anchoredPosition.y,Is.LessThan(settledY-1f));
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(group.alpha,Is.EqualTo(1).Within(.002f));
            Assert.That(input.GetComponent<RectTransform>().anchoredPosition.y,Is.EqualTo(settledY).Within(.1f));
            for(var i=0;i<3;i++)
            {
                Button("RitualChangeSpread").onClick.Invoke();
                yield return null;
                Assert.That(group.blocksRaycasts,Is.False);
                first.onClick.Invoke();
                yield return null;
                Assert.That(input.text,Is.EqualTo("保留我的问题"));
            }
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(input.GetComponent<RectTransform>().anchoredPosition.y,Is.EqualTo(settledY).Within(.1f));
            input.text="\u3000 ";
            yield return null;
            Assert.That(Button("DrawButton").interactable,Is.False);
            Assert.That(Button("DrawButton").GetComponent<CanvasGroup>().alpha,Is.EqualTo(.35f).Within(.005f));
        }

        [UnityTest]
        public IEnumerator RemovingFeedbackRestoresOnlyItsInkAndOffsetsAndKeepsTheOriginalButton()
        {
            yield return Load("MainMenu");
            var button=Button("StartReadingButton");
            var feedback=RequireFeedback(button);
            var label=button.GetComponentInChildren<TMP_Text>();
            var position=label.rectTransform.anchoredPosition;
            var material=label.fontSharedMaterial;
            var listeners=button.onClick;
            Enter(button.gameObject);
            yield return new WaitForSecondsRealtime(.25f);
            Object.Destroy(feedback);
            yield return null;
            Assert.That(button.transform.Find("RitualFocusRing"),Is.Null);
            Assert.That(Vector2.Distance(label.rectTransform.anchoredPosition,position),Is.LessThan(.1f));
            AssertRgb(label.color,new Color(240f/255,214f/255,165f/255),.003f);
            Assert.That(label.fontSharedMaterial,Is.SameAs(material));
            Assert.That(button.onClick,Is.SameAs(listeners));
            Assert.That(button.interactable,Is.True);
            AssertPointer(button);
        }

        [UnityTest]
        public IEnumerator ResultControlsGainFeedbackWithoutReplacingTheirActionsOrScrollArea()
        {
            ReadingSessionStore.Save(LocalReadingSimulator.CreateSession(2,"三张牌","A result feedback test","general",
                LocalReadingSimulator.CreatePlaceholderDraws(3)));
            yield return Load("Result");
            var button=Button("BackToMenuButton");
            RequireFeedback(button);
            var action=button.onClick;
            var scroll=canvas.GetComponentInChildren<ScrollRect>(true);
            Assert.That(scroll.viewport,Is.Not.Null);
            Assert.That(scroll.GetComponent<Image>().raycastTarget,Is.True);
            var label=button.GetComponentInChildren<TMP_Text>();
            var normal=label.color;
            Enter(button.gameObject);
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(label.color.b,Is.GreaterThan(normal.b));
            Assert.That(button.onClick,Is.SameAs(action));
            Assert.That(button.targetGraphic.color.a,Is.Zero.Within(.001f));
            Exit(button.gameObject);
            yield return new WaitForSecondsRealtime(.25f);
            AssertRgb(label.color,normal,.004f);
        }

        private IEnumerator Load(string name)
        {
            yield return SceneManager.LoadSceneAsync(name);
            yield return new WaitForSecondsRealtime(.75f);
            canvas=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.isRootCanvas&&c.gameObject.scene==SceneManager.GetActiveScene());
            camera=Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Where(c=>c.isActiveAndEnabled&&c.gameObject.scene==SceneManager.GetActiveScene())
                .OrderByDescending(c=>c.depth).First();
        }

        private Button Button(string name)=>canvas.transform.Find(name).GetComponent<Button>();
        private static Component RequireFeedback(Button button)
        {
            var type=typeof(RitualMenuView).Assembly.GetType("TarotUnity.UI.RitualControlFeedback");
            Assert.That(type,Is.Not.Null,"The ritual controls have no replacement hover/focus feedback.");
            var component=button.GetComponent(type);
            Assert.That(component,Is.Not.Null,button.name);
            return component;
        }
        private static bool VisibleInk(Button button,string name)
        {
            var node=button.transform.Find(name);
            Assert.That(node,Is.Not.Null,name);
            return node.gameObject.activeInHierarchy && node.GetComponent<Graphic>().color.a>.001f;
        }
        private static void Enter(GameObject node)=>ExecuteEvents.Execute(node,new PointerEventData(EventSystem.current),ExecuteEvents.pointerEnterHandler);
        private static void Exit(GameObject node)=>ExecuteEvents.Execute(node,new PointerEventData(EventSystem.current),ExecuteEvents.pointerExitHandler);
        private void AssertPointer(Button button)
        {
            var point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,
                button.transform.TransformPoint(((RectTransform)button.transform).rect.center));
            var hits=new List<RaycastResult>();
            canvas.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current){position=point},hits);
            Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<Button>(),Is.SameAs(button));
        }
        private static void AssertRgb(Color value,Color expected,float tolerance)
        {
            Assert.That(value.r,Is.EqualTo(expected.r).Within(tolerance));
            Assert.That(value.g,Is.EqualTo(expected.g).Within(tolerance));
            Assert.That(value.b,Is.EqualTo(expected.b).Within(tolerance));
        }
        private static void Offline(Scene scene,LoadSceneMode mode)
        {
            if(scene.name=="ReadingRoom") typeof(ReadingRoomController).GetField("backendMode",Instance)
                .SetValue(Object.FindFirstObjectByType<ReadingRoomController>(),BackendIntegrationMode.LocalSimulation);
        }
    }
}

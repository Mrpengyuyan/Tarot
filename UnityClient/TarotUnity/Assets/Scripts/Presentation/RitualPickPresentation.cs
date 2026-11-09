using System.Collections;
using System.Collections.Generic;
using TarotUnity.Gameplay;
using TarotUnity.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TarotUnity.Presentation
{
    /// <summary>Opt-in projection of the actual fan and deck cards; never owns a draw or a reading.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(1150)]
    public sealed class RitualPickPresentation : MonoBehaviour
    {
        private sealed class FanInk
        {
            public RitualPaperSurface Surface;
            public float HoverFrom, HoverTarget, HoverSince;
        }
        private readonly Dictionary<CardView, FanInk> ink = new();
        private readonly List<Transform> slots = new();
        private readonly List<RitualPickGuideGraphic> guides = new();
        private readonly List<RitualPickGuideGraphic> ticks = new();
        private readonly List<TMP_Text> labels = new();
        private readonly Dictionary<CardView, float> landed = new();
        private SpreadFanController fan;
        private DeckController deck;
        private PickedCardStaging staging;
        private ReadingFlowController flow;
        private CameraChoreographyController choreography;
        private Camera cameraView;
        private RectTransform canvas;
        private TMP_Text progress;
        private Transform world;
        private GameObject uiRoot;
        private RitualPickFrame frame;
        private Vector2Int size;
        private int count, accepted;
        private bool frozen;
        private bool movingToDraw;

        public static RitualPickPresentation Attach(GameObject host, SpreadFanController fan, DeckController deck,
            PickedCardStaging staging, ReadingFlowController flow, CameraChoreographyController choreography, Camera camera)
        {
            if (host.transform is not RectTransform || fan == null || deck == null || staging == null || camera == null) return null;
            var owner = host.AddComponent<RitualPickPresentation>();
            owner.fan=fan; owner.deck=deck; owner.staging=staging; owner.flow=flow;
            owner.choreography=choreography; owner.cameraView=camera; owner.canvas=(RectTransform)host.transform;
            owner.progress=host.transform.Find("RitualProgress")?.GetComponent<TMP_Text>();
            fan.PickPresentation=owner;
            fan.CardPicked+=owner.Picked;
            deck.CardDealt+=owner.Landed;
            staging.TransferStarted+=owner.Freeze;
            return owner;
        }

        public void Prepare(int cardCount)
        {
            ClearUi();
            count=cardCount; accepted=0; frozen=false; movingToDraw=true; frame=null;
            ink.Clear(); landed.Clear(); slots.Clear();
            if (world == null)
            {
                world=new GameObject("RitualPickWorld",typeof(RitualSpreadCardLease)).transform;
                world.GetComponent<RitualSpreadCardLease>().enabled=false;
                SceneManager.MoveGameObjectToScene(world.gameObject,gameObject.scene);
            }
            staging.Prepare(count,cameraView);
            uiRoot=new GameObject("RitualPickGuides",typeof(RectTransform));
            uiRoot.transform.SetParent(canvas,false);
            for(var i=0;i<count;i++)
            {
                var slot=staging.Slot(i);
                slot.gameObject.AddComponent<RitualPickSlot>().Initialize(this,i);
                slots.Add(slot);
                guides.Add(Graphic("PickedGuide_"+i,false));
                ticks.Add(Graphic("PickedTick_"+i,true));
                var node=new GameObject("PickedOrder_"+i,typeof(RectTransform),typeof(TextMeshProUGUI));
                node.transform.SetParent(uiRoot.transform,false);
                var label=node.GetComponent<TMP_Text>();
                label.font=progress != null ? progress.font : null;
                label.text=(i+1).ToString(); label.alignment=TextAlignmentOptions.Center;
                label.enableAutoSizing=false; label.textWrappingMode=TextWrappingModes.NoWrap;
                label.raycastTarget=false; label.margin=Vector4.zero;
                labels.Add(label);
            }
            RitualTypography.ApplyHierarchy(uiRoot.transform);
            RefreshLayout();
        }

        private RitualPickGuideGraphic Graphic(string name,bool tick)
        {
            var node=new GameObject(name,typeof(RectTransform),typeof(RitualPickGuideGraphic));
            node.transform.SetParent(uiRoot.transform,false);
            var graphic=node.GetComponent<RitualPickGuideGraphic>();graphic.Tick=tick;graphic.raycastTarget=false;
            return graphic;
        }

        public void PrepareFanCard(CardView card,int index)
        {
            card.transform.SetParent(world,true);
            var paper=RitualPaperSurface.Attach(card);
            paper.Back.sortingOrder=index;
            ink[card]=new FanInk { Surface=paper };
            var box=card.GetComponent<BoxCollider>();
            if(box!=null) { box.center=Vector3.zero; box.size=new Vector3(RitualPaperSurface.PaperWidth,0.0001f,1f); }
            SetPose(card,frame.Fan[index],Depth(index));
        }

        private static float Depth(int index) => 2.5f-index*0.0001f;

        public void FanPose(int index,out Vector3 position,out Quaternion rotation)
        {
            Projection(frame.Fan[index],Depth(index),out position,out rotation,out _);
        }

        public void ApplyFanHover(CardView card,int index,bool hovering)
        {
            if(!ink.TryGetValue(card,out var value)) return;
            var target=hovering?1f:0f;
            var amount=Hover(value);
            if(value.HoverTarget!=target)
            { value.HoverFrom=amount; value.HoverTarget=target; value.HoverSince=Time.time; }
            var pose=frame.Fan[index];
            pose=new RitualPickPose(pose.Center-Vector2.up*(cameraView.pixelHeight-2f*frame.Scale)*0.038f*amount,pose.Size,pose.Roll);
            SetPose(card,pose,Depth(index));
            value.Surface.Back.sortingOrder=amount>0.001f?200:index;
            value.Surface.Back.color=Color.white*(1f+0.35f*amount);
            value.Surface.EnforcePaper();
        }

        private static float Hover(FanInk value) => Mathf.Lerp(value.HoverFrom,value.HoverTarget,RitualPickLayout.HoverEase((Time.time-value.HoverSince)/0.3f));

        public IEnumerator Fly(CardView card,int index)
        {
            var from=Capture(card);
            var normalizedCenter=new Vector2(from.Center.x/cameraView.pixelWidth,from.Center.y/cameraView.pixelHeight);
            var normalizedSize=from.Size/cameraView.pixelWidth;
            var surface=RitualPaperSurface.Attach(card);
            surface.Back.sortingOrder=300; surface.Back.color=Color.white;
            var hover=card.GetComponent<CardHoverTiltController>();
            if(hover!=null) hover.Suspend();
            for(var elapsed=0f;elapsed<RitualPickSlot.FlightSeconds;elapsed+=Time.deltaTime)
            {
                if(this==null || card==null || staging==null || staging.StagingRoot==null
                    || index>=slots.Count || slots[index]==null) yield break;
                var start=new RitualPickPose(Vector2.Scale(normalizedCenter,new Vector2(cameraView.pixelWidth,cameraView.pixelHeight)),
                    normalizedSize*cameraView.pixelWidth,from.Roll);
                var target=new RitualPickPose(frame.Slots[index].center,frame.Slots[index].size);
                SetPose(card,RitualPickLayout.Flight(start,target,elapsed/RitualPickSlot.FlightSeconds,cameraView.pixelHeight),2.48f);
                surface.EnforcePaper();
                yield return null;
            }
            if(this==null || card==null || staging==null || staging.StagingRoot==null
                || index>=slots.Count || slots[index]==null) yield break;
            SetPose(card,frame.Selected[index],2.48f);
            slots[index].GetComponent<RitualPickSlot>().ConfirmLanding(card);
        }

        private RitualPickPose Capture(CardView card)
        {
            var p=cameraView.WorldToScreenPoint(card.transform.position);
            var unit=PixelUnit(2.5f,cameraView.fieldOfView);
            var right=cameraView.transform.InverseTransformDirection(card.transform.right);
            return new RitualPickPose(new Vector2(p.x,cameraView.pixelHeight-p.y),
                new Vector2(card.transform.lossyScale.x*RitualPaperSurface.PaperWidth,card.transform.lossyScale.z)/unit,
                -Mathf.Atan2(right.y,right.x)*Mathf.Rad2Deg);
        }

        private void Picked(CardView card) { accepted++; }
        private void Landed(CardView card) { if(!frozen && slots.Count>0) landed[card]=Time.time; }
        private void Freeze() { frozen=true; if(uiRoot!=null)uiRoot.SetActive(false); }
        public void SetFanVisible(bool visible) { if(world!=null)world.gameObject.SetActive(visible); }
        public void DetachFan()
        {
            fan=null;
            if(world!=null)Destroy(world.gameObject);
            if(uiRoot!=null)uiRoot.SetActive(false);
        }
        private void LateUpdate() => RefreshLayout();

        public void RefreshLayout()
        {
            if(cameraView==null || slots.Count==0 || frozen) return;
            var active=fan!=null && fan.isActiveAndEnabled && flow!=null
                && flow.State==ReadingFlowState.Drawing && staging.StagingRoot!=null;
            if(uiRoot!=null) uiRoot.SetActive(active);
            if(!active) return;
            var viewport=new Vector2Int(cameraView.pixelWidth,cameraView.pixelHeight);
            if(viewport.x<=2 || viewport.y<=2) return;
            if(frame==null || size!=viewport) { size=viewport; frame=RitualPickLayout.Compute(count,size.x,size.y); }
            if(movingToDraw && (choreography==null || !choreography.IsMoving)) movingToDraw=false;
            for(var i=0;i<count;i++)
            {
                if(slots[i]==null) continue;
                Projection(frame.Selected[i],2.48f,out var position,out var rotation,out var scale);
                slots[i].SetPositionAndRotation(position,rotation);slots[i].localScale=scale;
                Place(guides[i].rectTransform,frame.Slots[i]);
                guides[i].color=new Color(180f/255f,150f/255f,90f/255f,37f/255f);
                Place(ticks[i].rectTransform,frame.Ticks[i]);
                ticks[i].rectTransform.localRotation=Quaternion.Euler(0f,0f,45f);
                ticks[i].Filled=i<accepted;
                ticks[i].color=i<accepted?new Color(222f/255f,195f/255f,140f/255f):new Color(161f/255f,131f/255f,83f/255f);
                var font=frame.LabelFont;
                Place(labels[i].rectTransform,new Rect(frame.Labels[i]-new Vector2(frame.Slots[i].width,font*1.5f)*0.5f,new Vector2(frame.Slots[i].width,font*1.5f)));
                labels[i].fontSize=font*canvas.rect.height/size.y;
                labels[i].color=i<accepted?new Color(223f/255f,193f/255f,138f/255f):new Color(185f/255f,155f/255f,108f/255f,119f/255f);
                if(i>=deck.ActiveCards.Count) continue;
                var card=deck.ActiveCards[i];
                if(card==null || !landed.TryGetValue(card,out var time)) continue;
                var pulse=1f+0.035f*(1f-RitualPickLayout.HoverEase((Time.time-time)/0.3f));
                SetPose(card,new RitualPickPose(frame.Selected[i].Center,frame.Selected[i].Size*pulse),2.48f);
                ink[card].Surface.EnforcePaper();
            }
            fan.RefreshPresentation();
            if(progress!=null)
            {
                progress.text=accepted<count?$"还可选 <size={frame.NumberFont*canvas.rect.height/size.y}> {count-accepted} </size> 张":$"已选 {count} / {count}";
                Place(progress.rectTransform,frame.Progress);
                progress.fontSize=frame.ProgressFont*canvas.rect.height/size.y;
                progress.characterSpacing=progress.fontSize*0.1f;
                var group=progress.GetComponent<CanvasGroup>();if(group!=null)group.alpha=1f;
            }
        }

        private float PixelUnit(float depth,float fov) => depth*2f*Mathf.Tan(fov*0.5f*Mathf.Deg2Rad)/cameraView.pixelHeight;
        private void Projection(RitualPickPose pose,float depth,out Vector3 position,out Quaternion rotation,out Vector3 scale)
        {
            var axes=cameraView.transform;var fov=cameraView.fieldOfView;
            if(movingToDraw && choreography!=null) choreography.TryGetDrawPose(count,out axes,out fov);
            var unit=PixelUnit(depth,fov);
            position=axes.position+axes.rotation*new Vector3((pose.Center.x-cameraView.pixelWidth*0.5f)*unit,
                (cameraView.pixelHeight*0.5f-pose.Center.y)*unit,depth);
            rotation=axes.rotation*Quaternion.AngleAxis(-pose.Roll,Vector3.forward)*Quaternion.Euler(-90f,0f,0f);
            scale=new Vector3(pose.Size.x/RitualPaperSurface.PaperWidth,pose.Size.x/RitualPaperSurface.PaperWidth,pose.Size.y)*unit;
        }
        private void SetPose(CardView card,RitualPickPose pose,float depth)
        {
            Projection(pose,depth,out var p,out var r,out var scale);
            card.transform.SetPositionAndRotation(p,r);
            var parent=card.transform.parent!=null?card.transform.parent.lossyScale:Vector3.one;
            card.transform.localScale=new Vector3(scale.x/parent.x,scale.y/parent.y,scale.z/parent.z);
        }
        private void Place(RectTransform rect,Rect pixels)
        {
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*0.5f;
            rect.anchoredPosition=new Vector2((pixels.center.x/size.x-0.5f)*canvas.rect.width,(0.5f-pixels.center.y/size.y)*canvas.rect.height);
            rect.sizeDelta=new Vector2(pixels.width/size.x*canvas.rect.width,pixels.height/size.y*canvas.rect.height);
        }
        private void ClearUi()
        {
            if(uiRoot!=null) { uiRoot.SetActive(false); Destroy(uiRoot); }
            guides.Clear();ticks.Clear();labels.Clear();
        }
        private void OnDestroy()
        {
            if(fan!=null) { fan.CardPicked-=Picked; if(fan.PickPresentation==this)fan.PickPresentation=null; }
            if(deck!=null)deck.CardDealt-=Landed;
            if(staging!=null)staging.TransferStarted-=Freeze;
            ClearUi();
            if(world!=null)
            {
                if(world.GetComponentInChildren<CardView>(true)!=null)world.GetComponent<RitualSpreadCardLease>().enabled=true;
                else Destroy(world.gameObject);
            }
        }
    }
}

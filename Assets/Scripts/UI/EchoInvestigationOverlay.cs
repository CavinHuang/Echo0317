using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Echo0317.Core;

namespace Echo0317.UI
{
    /// <summary>
    /// Screen-space film investigation layer. The film remains visible behind
    /// the left half while players focus, click or Submit real hotspot buttons.
    /// Child buttons are retired immediately on each re-render to avoid
    /// duplicate investigation callbacks in the same frame.
    /// </summary>
    public sealed class EchoInvestigationOverlay
    {
        public readonly GameObject Root;
        public readonly RectTransform HotspotArea;
        public readonly Text Title, Summary, ResultTitle, ResultBody, ResultStatus;
        readonly Image resultArt, notificationArt;
        readonly EchoInvestigationArt assets = new EchoInvestigationArt();
        readonly Font font;
        readonly Button exitButton;
        GameObject focusBeforeOpen;
        readonly List<EchoHotspotVisual> visible = new List<EchoHotspotVisual>();

        static readonly Color32 White = new Color32(244,244,239,255);
        static readonly Color32 Secondary = new Color32(180,192,199,255);
        static readonly Color32 Gold = new Color32(232,192,127,255);

        public EchoInvestigationOverlay(EchoGameUI ui)
        {
            font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "SimHei", "Arial" }, 24);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = Surface("SceneInvestigation", ui.Canvas.transform, Vector2.zero, Vector2.one,
                new Color(.015f,.035f,.052f,.82f));
            Root = root.gameObject;
            // The image-backed stage is on the left, and the info panel is a
            // separate scroll-free reading area instead of covering the film.
            var film = Surface("InvestigationFilmStage", root,
                new Vector2(.022f,.17f), new Vector2(.475f,.86f),
                new Color(.055f,.077f,.10f,.23f));
            HotspotArea = Surface("HotspotArea", film, Vector2.zero, Vector2.one, Color.clear);
            Title = Label("InvestigationTitle", root, "现场调查 / INVESTIGATION", 28, Gold,
                TextAnchor.MiddleLeft, new Vector2(.035f,.89f), new Vector2(.65f,.98f), true);
            Summary = Label("HotspotProgress", root, "", 17, White,
                TextAnchor.MiddleRight, new Vector2(.59f,.89f), new Vector2(.96f,.98f));
            var detail = Surface("InvestigationCard",root,new Vector2(.50f,.17f),
                new Vector2(.975f,.86f),new Color(.09f,.135f,.17f,.97f));
            resultArt = Surface("ResultArtwork",detail,new Vector2(.01f,.24f),
                new Vector2(.99f,.98f), Color.white).GetComponent<Image>();
            resultArt.sprite = assets.Get("inspect-result");
            resultArt.color = new Color(1f,1f,1f,.17f);
            resultArt.raycastTarget = false;
            ResultTitle=Label("InspectHeading",detail,"选择调查目标",28,Gold,TextAnchor.UpperLeft,
                new Vector2(.06f,.78f),new Vector2(.93f,.91f),true);
            ResultBody=Label("InspectText",detail,"使用鼠标点击热点，或使用方向键 / 手柄选择并确认。",21,White,
                TextAnchor.UpperLeft,new Vector2(.06f,.37f),new Vector2(.93f,.76f));
            ResultStatus=Label("InspectStatus",detail,"调查不会擅自改变剧情证据。",17,Secondary,
                TextAnchor.MiddleLeft,new Vector2(.06f,.25f),new Vector2(.93f,.35f));
            notificationArt=Surface("InvestigationNotification",detail,new Vector2(.05f,.035f),
                new Vector2(.95f,.23f),Color.white).GetComponent<Image>();
            notificationArt.raycastTarget=false;
            notificationArt.sprite=assets.Get("notice-repeat");
            notificationArt.color=new Color(1f,1f,1f,.85f);
            exitButton = ActionButton("返回剧情 / ESC", root,
                new Vector2(.49f,.035f),new Vector2(.78f,.13f),null);
            Root.SetActive(false);
        }

        static RectTransform Surface(string name,Transform parent,Vector2 min,Vector2 max,Color color)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(Image));
            var rt=go.GetComponent<RectTransform>();
            rt.SetParent(parent,false);
            rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=color.a>.05f;
            return rt;
        }

        Text Label(string name,Transform parent,string text,int size,Color color,
            TextAnchor alignment,Vector2 min,Vector2 max,bool bold=false)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));
            var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
            rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=Vector2.zero;rt.offsetMax=Vector2.zero;
            var component=go.GetComponent<Text>();
            component.text=text;component.font=font;component.fontSize=size;
            component.fontStyle=bold?FontStyle.Bold:FontStyle.Normal;
            component.alignment=alignment;component.color=color;
            component.horizontalOverflow=HorizontalWrapMode.Wrap;
            component.verticalOverflow=VerticalWrapMode.Overflow;
            component.raycastTarget=false;
            return component;
        }

        Button ActionButton(string label,Transform parent,Vector2 min,Vector2 max,Action onClick)
        {
            var rt=Surface("InvestigationBack",parent,min,max,new Color(.13f,.20f,.23f,.99f));
            var button=rt.gameObject.AddComponent<Button>();
            button.targetGraphic=rt.GetComponent<Image>();
            if(onClick!=null)button.onClick.AddListener(()=>onClick());
            rt.gameObject.AddComponent<EchoFocusOutline>();
            Label("ExitCaption",rt,label,20,White,TextAnchor.MiddleCenter,
                Vector2.zero,Vector2.one,true);
            return button;
        }

        public void Show(HotspotDefinition[] hotspots,GameProgress progress,
            string selectedId,bool newRecord,bool verified,
            Action<string> inspect,Action exit)
        {
            bool entering=!Root.activeSelf;
            if(entering)
                focusBeforeOpen=EventSystem.current!=null ?
                    EventSystem.current.currentSelectedGameObject : null;
            Root.SetActive(true);
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(()=>exit?.Invoke());
            foreach(var old in visible)
                if(old!=null)
                {
                    old.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(old.gameObject);
                }
            visible.Clear();
            int done=0;
            foreach(var hotspot in hotspots)
            {
                bool visited=EchoHotspotInvestigation.IsInspected(progress,hotspot);
                if(visited)done++;
                AddHotspot(hotspot,visited,inspect,exit);
            }
            Summary.text="已检查 " + done + " / " + hotspots.Length +
                "  ·  点击热点 / 方向键选择";
            var selected=hotspots.FirstOrDefault(h=>h.id==selectedId);
            if(selected==null)
            {
                ResultTitle.text="选择调查目标";
                ResultBody.text="热点位于画面左侧。调查能够留下可回溯的观察记录；真正的案件证据仍由剧情选择核实。";
                ResultStatus.text="调查可随时退出，不会自动推进剧情。";
                notificationArt.sprite=assets.Get("notice-repeat");
            }
            else
            {
                ResultTitle.text=selected.label;
                ResultBody.text=selected.description;
                ResultStatus.text=verified ? "档案证据已核实 · " +
                    (selected.evidenceId??"") : newRecord ?
                    "现场观察已记录 · 待剧情证据核实" :
                    "这个目标已经检查过，不会重复获取记录";
                notificationArt.sprite=assets.Get(newRecord ?
                    (verified ? "notice-updated" : "notice-new") : "notice-repeat");
            }
            // Place focus on the inspected hotspot after a rerender, keeping
            // controller play continuous and the evidence position stable.
            if(EventSystem.current!=null)
            {
                var focus=visible.FirstOrDefault(h=>h.HotspotId==selectedId) ??
                    visible.FirstOrDefault();
                EventSystem.current.SetSelectedGameObject(focus!=null ?
                    focus.gameObject : exitButton.gameObject);
            }
        }

        void AddHotspot(HotspotDefinition h,bool inspected,Action<string> inspect,Action exit)
        {
            var rt=Surface("Hotspot-"+h.id,HotspotArea,
                new Vector2(h.x,h.y),new Vector2(h.x+h.w,h.y+h.h),Color.white);
            var image=rt.GetComponent<Image>();
            image.sprite=assets.Get(inspected?"hotspot-complete":"hotspot-idle");
            image.color=Color.white;
            var button=rt.gameObject.AddComponent<Button>();
            button.targetGraphic=image;
            button.onClick.AddListener(()=>inspect?.Invoke(h.id));
            var outline=rt.gameObject.AddComponent<EchoFocusOutline>();
            var cancel=rt.gameObject.AddComponent<EchoCancelAction>();
            cancel.OnBack=()=>exit?.Invoke();
            var visual=rt.gameObject.AddComponent<EchoHotspotVisual>();
            visual.Initialize(h.id,image,assets.Get("hotspot-idle"),
                assets.Get("hotspot-focus"),assets.Get("hotspot-complete"),inspected);
            var icon=assets.Get("icon-"+h.icon);
            if(icon!=null)
            {
                var picture=Surface("HotspotIcon",rt,new Vector2(.32f,.28f),
                    new Vector2(.68f,.80f),Color.white).GetComponent<Image>();
                picture.sprite=icon;picture.preserveAspect=true;picture.raycastTarget=false;
            }
            Label("HotspotLabel",rt,h.label,14,White,TextAnchor.MiddleCenter,
                new Vector2(0,0),new Vector2(1,.25f),true);
            visible.Add(visual);
        }

        public void Hide()
        {
            if(!Root.activeSelf)return;
            Root.SetActive(false);
            if(EventSystem.current==null)return;
            var previous=focusBeforeOpen;
            focusBeforeOpen=null;
            if(previous!=null && previous.activeInHierarchy)
            {
                var button=previous.GetComponent<Button>();
                if(button!=null && button.IsInteractable())
                    EventSystem.current.SetSelectedGameObject(previous);
            }
        }

        public int VisibleHotspots => visible.Count;
    }

    public sealed class EchoHotspotVisual : MonoBehaviour,
        ISelectHandler,IDeselectHandler,IPointerEnterHandler,IPointerExitHandler
    {
        public string HotspotId { get; private set; }
        Image image;
        Sprite idle,focus,done;
        bool inspected,selected,hover;
        public void Initialize(string id,Image target,Sprite normal,Sprite focused,
            Sprite complete,bool alreadyDone)
        {
            HotspotId=id;image=target;idle=normal;focus=focused;
            done=complete;inspected=alreadyDone;Refresh();
        }
        void Refresh()
        {
            if(image==null)return;
            image.sprite=selected||hover?focus:(inspected?done:idle);
        }
        public void OnSelect(BaseEventData data){selected=true;Refresh();}
        public void OnDeselect(BaseEventData data){selected=false;Refresh();}
        public void OnPointerEnter(PointerEventData data){hover=true;Refresh();}
        public void OnPointerExit(PointerEventData data){hover=false;Refresh();}
    }
}

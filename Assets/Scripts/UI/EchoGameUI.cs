using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Echo0317.UI
{
    /// <summary>Native Unity uGUI presentation. Runtime-generated so the project does not rely on HTML assets.</summary>
    public sealed class EchoGameUI
    {
        public readonly Canvas Canvas;
        public readonly GameObject Menu, Game, Modal;
        public readonly RectTransform VideoLayer, MenuBackground;
        public readonly RectTransform ChoiceList, ModalList, MenuActions;
        public readonly ScrollRect StoryScroll, ArchiveScroll;
        public readonly GameObject DiscoveryCard, CaptionBackdrop;
        readonly CanvasGroup gameInput, menuInput;
        GameObject focusBeforeModal;
        public readonly Text DiscoveryText, SaveStatus;
        public readonly Text Chapter, Clock, FilmStatus, Title, Speaker, Body, Subtitle, ModalTitle, Hint, MenuProgress;
        public readonly Button ContinueButton;
        public Action OnAdvance, OnBack;
        public static readonly Color White = Rgb(245, 244, 241);
        public static readonly Color Faded = Rgb(175, 191, 205);
        public static readonly Color Gold = Rgb(226, 185, 126);
        public static readonly Color Red = Rgb(208, 102, 93);
        public static readonly Color Dark = Rgb(11, 17, 25);
        readonly Font font;
        static Color Rgb(byte r, byte g, byte b, byte a=255) => new Color32(r,g,b,a);
        public static Color Ink(float alpha) => new Color(0.035f,0.06f,0.09f,alpha);
        public EchoGameUI()
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "PingFang SC", "Noto Sans CJK SC", "SimHei", "Arial" }, 28);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Echo UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            Canvas = root.GetComponent<Canvas>(); Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventGo = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                UnityEngine.Object.DontDestroyOnLoad(eventGo);
            }
            UnityEngine.Object.DontDestroyOnLoad(root);
            var frame = Panel("Frame", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Rgb(6,12,19));
            Game = Panel("FilmMode", frame, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.clear).gameObject;
            VideoLayer = Panel("VideoStage", Game.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Rgb(15,24,32));
            var stageTint = Panel("StageTint", Game.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Ink(.24f));
            stageTint.GetComponent<Image>().raycastTarget = false;
            // HUD header; the film stays dominant, while story/dialogue remains readable without art.
            Panel("Header", Game.transform, new Vector2(0,1), new Vector2(1,1), new Vector2(0,-96), Vector2.zero, Ink(.86f));
            Label("Mark",Game.transform,"03:17  ·  回声",24,Gold,TextAnchor.MiddleLeft,
                new Vector2(0,1),new Vector2(0,1),new Vector2(48,-74),new Vector2(370,-17),FontStyle.Bold);
            Chapter = Label("Chapter",Game.transform,"第一幕",20,White,TextAnchor.MiddleCenter,
                new Vector2(.36f,1),new Vector2(.65f,1),new Vector2(0,-74),new Vector2(0,-20));
            Clock = Label("Time",Game.transform,"03:17",20,Gold,TextAnchor.MiddleRight,
                new Vector2(.67f,1),new Vector2(.72f,1),new Vector2(0,-70),new Vector2(0,-18));
            var headerButtons = Panel("HeaderButtons",Game.transform,new Vector2(1,1),new Vector2(1,1),new Vector2(-440,-77),new Vector2(-29,-17),Color.clear);
            Horizontal(headerButtons,8);
            Button("回顾",headerButtons,()=>OnHistory?.Invoke(),18, Ink(.85f));
            Button("档案",headerButtons,()=>OnArchive?.Invoke(),18, Ink(.85f));
            Button("存档",headerButtons,()=>OnSaves?.Invoke(),18, Ink(.85f));
            Button("暂停",headerButtons,()=>OnPause?.Invoke(),18, Ink(.85f));
            var storyPanel=Panel("NarrativePane",Game.transform,new Vector2(.49f,.045f),new Vector2(.975f,.89f),Vector2.zero,Vector2.zero,Ink(.88f));
            Panel("Accent",storyPanel,new Vector2(0,0),new Vector2(0,1),Vector2.zero,new Vector2(3,0),Gold);
            Speaker=Label("Speaker",storyPanel,"周序",19,Gold,TextAnchor.MiddleLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(38,-83),new Vector2(-28,-41));
            Title=Label("SceneTitle",storyPanel,"",33,White,TextAnchor.UpperLeft,new Vector2(0,1),new Vector2(1,1),new Vector2(38,-167),new Vector2(-30,-89),FontStyle.Bold);
            StoryScroll=Scroll("SceneScroll",storyPanel,new Vector2(0,0),new Vector2(1,1),new Vector2(35,100),new Vector2(-23,-181),out var content);
            var column=content.gameObject.AddComponent<VerticalLayoutGroup>(); column.spacing=13;column.padding=new RectOffset(4,4,6,10);column.childAlignment=TextAnchor.UpperLeft;
            column.childForceExpandHeight=false;column.childForceExpandWidth=true;
            var fit=content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Body = Label("Narrative",content,"",24,White,TextAnchor.UpperLeft);
            Body.horizontalOverflow=HorizontalWrapMode.Wrap;Body.verticalOverflow=VerticalWrapMode.Overflow;
            var bodyFit=Body.gameObject.AddComponent<ContentSizeFitter>();bodyFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            ChoiceList=Panel("Choices",content,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Color.clear);
            Vertical(ChoiceList,10);
            var chFit=ChoiceList.gameObject.AddComponent<ContentSizeFitter>();chFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            StoryScroll.verticalNormalizedPosition = 1;
            Hint=Label("Hint",storyPanel,"选择会留下痕迹",16,Faded,TextAnchor.MiddleLeft,new Vector2(0,0),new Vector2(1,0),new Vector2(36,21),new Vector2(-24,86));
            FilmStatus=Label("Shot",Game.transform,"",16,Faded,TextAnchor.LowerLeft,new Vector2(0,0),new Vector2(.46f,0),new Vector2(48,28),new Vector2(-10,76));
            CaptionBackdrop=Panel("CaptionBackdrop",Game.transform,new Vector2(.02f,0),new Vector2(.48f,0),
                new Vector2(0,78),new Vector2(0,222),Ink(.76f)).gameObject;
            CaptionBackdrop.GetComponent<Image>().raycastTarget = false;
            Subtitle=Label("Subtitle",Game.transform,"",26,White,TextAnchor.LowerCenter,new Vector2(.03f,0),new Vector2(.47f,0),new Vector2(0,90),new Vector2(0,200),FontStyle.Bold);
            Subtitle.gameObject.AddComponent<Shadow>().effectColor=Color.black;
            var lowerTint=Panel("LeftLowerTint",Game.transform,new Vector2(0,0),new Vector2(.47f,0),new Vector2(0,0),new Vector2(0,84),Ink(.6f));
            lowerTint.SetAsLastSibling(); FilmStatus.transform.SetAsLastSibling(); Subtitle.transform.SetAsLastSibling();
            // Clearly acknowledge player decisions without spoiling unrecovered clues.
            DiscoveryCard=Panel("EvidenceDiscovery",Game.transform,new Vector2(.03f,.68f),new Vector2(.47f,.78f),
                Vector2.zero,Vector2.zero,Ink(.94f)).gameObject;
            DiscoveryText=Label("DiscoveryText",DiscoveryCard.transform,"",21,Gold,TextAnchor.MiddleLeft,
                Vector2.zero,Vector2.one,new Vector2(20,6),new Vector2(-18,-6),FontStyle.Bold);
            DiscoveryCard.SetActive(false);
            SaveStatus=Label("SaveWarning",Game.transform,"",17,Red,TextAnchor.MiddleLeft,
                new Vector2(0,1),new Vector2(.56f,1),new Vector2(48,-146),new Vector2(-6,-101),FontStyle.Bold);
            Menu=Panel("MainMenu",frame,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Ink(.93f)).gameObject;
            MenuBackground=Panel("MenuArt",Menu.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Rgb(15,24,32));
            Panel("MenuShade",Menu.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Ink(.72f));
            Panel("MenuLine",Menu.transform,new Vector2(.075f,.78f),new Vector2(.075f,.93f),Vector2.zero,new Vector2(3,0),Red);
            Label("MenuEyebrow",Menu.transform,"INTERACTIVE CINEMA  /  互动悬疑",23,Gold,TextAnchor.MiddleLeft,
                new Vector2(.10f,.78f),new Vector2(.8f,.9f),Vector2.zero,Vector2.zero);
            Label("MenuTitle",Menu.transform,"03:17",114,White,TextAnchor.MiddleLeft,
                new Vector2(.09f,.55f),new Vector2(.9f,.82f),Vector2.zero,Vector2.zero,FontStyle.Bold);
            Label("MenuSub",Menu.transform,"回 声",53,White,TextAnchor.MiddleLeft,
                new Vector2(.11f,.48f),new Vector2(.76f,.60f),Vector2.zero,Vector2.zero);
            Label("MenuTagline",Menu.transform,"三分钟后有人敲门。你相信的记忆，未必属于你。",23,Faded,TextAnchor.MiddleLeft,
                new Vector2(.11f,.43f),new Vector2(.8f,.50f),Vector2.zero,Vector2.zero);
            MenuActions=Panel("MenuActions",Menu.transform,new Vector2(.115f,.07f),new Vector2(.48f,.41f),Vector2.zero,Vector2.zero,Color.clear);
            Vertical(MenuActions,10);
            ContinueButton=Button("继续游戏",MenuActions,()=>OnContinue?.Invoke(),24,Gold,.46f);
            Button("新的轮回",MenuActions,()=>OnNewGame?.Invoke(),23,Ink(.96f));
            Button("存档管理",MenuActions,()=>OnSaves?.Invoke(),22,Ink(.96f));
            Button("游戏设置",MenuActions,()=>OnSettings?.Invoke(),22,Ink(.96f));
            gameInput=Game.AddComponent<CanvasGroup>();
            menuInput=Menu.AddComponent<CanvasGroup>();
            MenuProgress=Label("MenuMeta",Menu.transform,"五幕 · 四个结局 · 全剧情可体验",18,Faded,TextAnchor.MiddleRight,
                new Vector2(.54f,.06f),new Vector2(.96f,.12f),Vector2.zero,Vector2.zero);
            // Modal is a genuine in-game overlay for evidence, casebook, chapters, saves and settings.
            Modal=Panel("Archive",frame,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,Ink(.94f)).gameObject;
            var modalPanel=Panel("ArchiveCard",Modal.transform,new Vector2(.13f,.08f),new Vector2(.87f,.91f),Vector2.zero,Vector2.zero,Rgb(22,31,43));
            ModalTitle=Label("ModalTitle",modalPanel,"档案",35,White,TextAnchor.MiddleLeft,
                new Vector2(0,1),new Vector2(1,1),new Vector2(38,-112),new Vector2(-100,-33),FontStyle.Bold);
            var closeButton=ButtonAt("关闭",modalPanel,()=>OnCloseModal?.Invoke(),new Vector2(1,1),new Vector2(1,1),new Vector2(-136,-100),new Vector2(-35,-41),19,Ink(.9f));
            closeButton.gameObject.AddComponent<EchoCancelAction>().OnBack = () => OnBack?.Invoke();
            ArchiveScroll=Scroll("ArchiveScroll",modalPanel,Vector2.zero,Vector2.one,new Vector2(43,38),new Vector2(-44,-122),out ModalList);
            Vertical(ModalList,12);
            var modalFit=ModalList.gameObject.AddComponent<ContentSizeFitter>();modalFit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            Modal.SetActive(false);Game.SetActive(false);
        }
        public Action OnArchive,OnHistory,OnSaves,OnPause,OnContinue,OnNewGame,OnCloseModal,OnSettings;
        public void SetGameVisible(bool visible)
        {
            Game.SetActive(visible);
            Menu.SetActive(!visible);
            if (visible && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
            if (!visible && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(ContinueButton.interactable
                    ? ContinueButton.gameObject : MenuActions.GetChild(1).gameObject);
        }
        public void ShowStartupError(string reason)
        {
            SetGameVisible(false);
            SetModal(false);
            // A missing or corrupt story file is a content failure, not an empty save.
            // Disable actions that would otherwise dereference an uninitialized game.
            foreach (var button in MenuActions.GetComponentsInChildren<Button>(true))
                button.interactable = false;
            MenuProgress.alignment = TextAnchor.MiddleLeft;
            MenuProgress.text = "无法启动：" + reason + "\n请检查游戏资源及 Player.log，或重新安装完整游戏。";
        }
        public void SetModal(bool visible)
        {
            var events = EventSystem.current;
            if (visible && !Modal.activeSelf)
                focusBeforeModal = events != null ? events.currentSelectedGameObject : null;
            Modal.SetActive(visible);
            // Merely covering a menu with an Image does NOT remove its buttons
            // from gamepad navigation. Disable both pointer and selectable input
            // behind the modal, then restore them when it closes.
            gameInput.interactable = !visible;
            gameInput.blocksRaycasts = !visible;
            menuInput.interactable = !visible;
            menuInput.blocksRaycasts = !visible;

            if (events == null) return;
            if (visible)
            {
                events.SetSelectedGameObject(null);
                return;
            }
            var previous = focusBeforeModal;
            focusBeforeModal = null;
            if (previous != null)
            {
                var button = previous.GetComponent<Button>();
                if (button != null && button.IsActive() && button.IsInteractable() &&
                    ((Game.activeSelf && previous.transform.IsChildOf(Game.transform)) ||
                     (Menu.activeSelf && previous.transform.IsChildOf(Menu.transform))))
                {
                    events.SetSelectedGameObject(previous);
                    return;
                }
            }
            if (Menu.activeSelf)
                events.SetSelectedGameObject(ContinueButton.interactable
                    ? ContinueButton.gameObject : MenuActions.GetChild(1).gameObject);
            else if (Game.activeSelf)
            {
                foreach (var button in ChoiceList.GetComponentsInChildren<Button>())
                {
                    if (!button.IsActive() || !button.IsInteractable()) continue;
                    events.SetSelectedGameObject(button.gameObject);
                    break;
                }
            }
        }
        public void SetSubtitleOptions(bool enabled, float scale)
        {
            Subtitle.fontSize = Mathf.RoundToInt(26f * Mathf.Clamp(scale, 1f, 1.5f));
            Subtitle.gameObject.SetActive(enabled);
            CaptionBackdrop.SetActive(enabled);
        }
        public int SelectedModalActionIndex()
        {
            if (EventSystem.current == null) return -1;
            var current = EventSystem.current.currentSelectedGameObject;
            var buttons = ModalList.GetComponentsInChildren<Button>();
            for (int i=0;i<buttons.Length;i++)
                if (buttons[i].gameObject == current) return i;
            return -1;
        }
        public void FocusModalAction(int index)
        {
            if (EventSystem.current == null || index < 0) return;
            var buttons = ModalList.GetComponentsInChildren<Button>();
            if (index < buttons.Length && buttons[index].IsInteractable())
                EventSystem.current.SetSelectedGameObject(buttons[index].gameObject);
        }
        public void ClearChoices()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            // Destroy is deferred to the end of the frame in a Unity Player.
            // Immediately disable old buttons so a double-click cannot invoke
            // an action belonging to a previous story node.
            foreach (Transform t in ChoiceList) { t.gameObject.SetActive(false); UnityEngine.Object.Destroy(t.gameObject); }
        }
        public void ClearModal()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            foreach (Transform t in ModalList) { t.gameObject.SetActive(false); UnityEngine.Object.Destroy(t.gameObject); }
        }
        public void ResetStoryScroll()
        {
            UnityEngine.Canvas.ForceUpdateCanvases();
            StoryScroll.StopMovement();
            StoryScroll.verticalNormalizedPosition = 1f;
        }
        public void ResetModalScroll()
        {
            UnityEngine.Canvas.ForceUpdateCanvases();
            ArchiveScroll.StopMovement();
            ArchiveScroll.verticalNormalizedPosition = 1f;
        }
        public void ShowDiscovery(string text)
        {
            DiscoveryText.text = text;
            DiscoveryCard.SetActive(true);
        }
        public void HideDiscovery() { DiscoveryCard.SetActive(false); }
        public void SetSaveWarning(bool error)
        {
            SaveStatus.text = error ? "⚠ 自动存档失败 · 请检查磁盘空间，或手动导出 JSON" : "";
        }
        public void AddChoice(string text,Action action,bool available=true)
        {
            var b=Button(text,ChoiceList,action,22,available?Rgb(45,65,78,245):Rgb(34,41,48,220),0.65f);
            b.interactable=available;
            b.gameObject.AddComponent<EchoScrollSelection>().Target = StoryScroll;
            if (available && EventSystem.current != null &&
                (EventSystem.current.currentSelectedGameObject == null ||
                 !EventSystem.current.currentSelectedGameObject.activeInHierarchy))
                EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
        public void AddModalText(string title,string subtitle=null)
        {
            var l=Label("ArchiveText",ModalList,title,23,White,TextAnchor.MiddleLeft);
            var fit=l.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub=Label("ArchiveNote",ModalList,subtitle,19,Faded,TextAnchor.MiddleLeft);
                sub.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            }
        }
        public void AddModalButton(string text,Action action,bool available=true)
        {
            var b=Button(text,ModalList,action,21,available?Rgb(51,69,83):Rgb(32,38,45),0.48f);b.interactable=available;
            b.gameObject.AddComponent<EchoScrollSelection>().Target = ArchiveScroll;
            b.gameObject.AddComponent<EchoCancelAction>().OnBack = () => OnBack?.Invoke();
            if (available && EventSystem.current != null &&
                (EventSystem.current.currentSelectedGameObject == null ||
                 !EventSystem.current.currentSelectedGameObject.activeInHierarchy))
                EventSystem.current.SetSelectedGameObject(b.gameObject);
        }
        /// <summary>Original textured, selectable evidence/case card.</summary>
        public Button AddDossierCard(string id, string category, string title, string source,
            bool pinned, int slotNumber, Action action)
        {
            var rt = Panel("DossierCard-" + id, ModalList, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, Color.white);
            var image = rt.GetComponent<Image>();
            image.sprite = EchoDossierArtwork.Card;
            image.color = pinned ? new Color(1f, .93f, .78f, 1f) : Color.white;
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 136;
            var button = rt.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => action?.Invoke());
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, .96f, .87f, 1f);
            colors.pressedColor = new Color(.75f, .75f, .73f, 1f);
            colors.selectedColor = new Color(1f, .95f, .83f, 1f);
            button.colors = colors;
            rt.gameObject.AddComponent<EchoDossierCardKey>().Id = id;
            rt.gameObject.AddComponent<EchoFocusOutline>();
            rt.gameObject.AddComponent<EchoScrollSelection>().Target = ArchiveScroll;
            rt.gameObject.AddComponent<EchoCancelAction>().OnBack = () => OnBack?.Invoke();
            Label("DossierCategory",rt,category,14,Gold,TextAnchor.MiddleLeft,
                new Vector2(0,1),new Vector2(1,1),new Vector2(30,-29),new Vector2(-150,-5),FontStyle.Bold);
            Label("DossierTitle",rt,title,22,White,TextAnchor.MiddleLeft,
                new Vector2(0,.26f),new Vector2(1,.85f),new Vector2(30,0),new Vector2(-25,-2),FontStyle.Bold);
            Label("DossierSource",rt,source,15,Faded,TextAnchor.MiddleLeft,
                new Vector2(0,0),new Vector2(1,.26f),new Vector2(30,3),new Vector2(-25,0));
            if (pinned)
            {
                Label("PinStatus",rt,"已固定  " + slotNumber.ToString("00"),15,Gold,TextAnchor.MiddleRight,
                    new Vector2(.74f,1),new Vector2(1,1),new Vector2(0,-29),new Vector2(-20,-5),FontStyle.Bold);
                rt.gameObject.AddComponent<EchoDossierReveal>();
            }
            if (EventSystem.current != null &&
                (EventSystem.current.currentSelectedGameObject == null ||
                 !EventSystem.current.currentSelectedGameObject.activeInHierarchy))
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            return button;
        }

        /// <summary>Two genuine selectable slots; click an occupied slot to remove its clue.</summary>
        public void AddEvidenceSlots(string first, string second, string firstId, string secondId,
            Action removeFirst, Action removeSecond)
        {
            var holder = Panel("EvidenceSlotsRow",ModalList,Vector2.zero,Vector2.one,
                Vector2.zero,Vector2.zero,Color.clear);
            holder.gameObject.AddComponent<LayoutElement>().preferredHeight = 132;
            var row = holder.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 12;
            row.childControlWidth = true; row.childControlHeight = true;
            row.childForceExpandWidth = true; row.childForceExpandHeight = true;
            AddEvidenceSlot(holder,1,first,firstId,removeFirst);
            AddEvidenceSlot(holder,2,second,secondId,removeSecond);
        }

        void AddEvidenceSlot(Transform parent,int number,string title,string id,Action remove)
        {
            bool occupied = !string.IsNullOrEmpty(id);
            var rt=Panel("EvidenceSlot-"+number,parent,Vector2.zero,Vector2.one,
                Vector2.zero,Vector2.zero,Color.white);
            rt.GetComponent<Image>().sprite=EchoDossierArtwork.Slot;
            rt.gameObject.AddComponent<LayoutElement>().flexibleWidth=1f;
            var button=rt.gameObject.AddComponent<Button>();
            button.interactable=occupied;
            button.onClick.AddListener(()=>remove?.Invoke());
            if (occupied)
            {
                rt.gameObject.AddComponent<EchoDossierCardKey>().Id=id;
                rt.gameObject.AddComponent<EchoFocusOutline>();
                rt.gameObject.AddComponent<EchoScrollSelection>().Target=ArchiveScroll;
                rt.gameObject.AddComponent<EchoCancelAction>().OnBack=()=>OnBack?.Invoke();
            }
            Label("SlotNumber",rt,"证据槽 "+number.ToString("00"),15,Gold,TextAnchor.MiddleLeft,
                new Vector2(0,.70f),new Vector2(1,1),new Vector2(18,0),new Vector2(-12,-6),FontStyle.Bold);
            Label("SlotContent",rt,occupied?title:"＋  等待放入线索",occupied?18:16,
                occupied?White:Faded,TextAnchor.MiddleLeft,
                new Vector2(0,.14f),new Vector2(1,.72f),new Vector2(18,0),new Vector2(-14,-2));
            if(occupied)Label("SlotAction",rt,"点击移除",13,Faded,TextAnchor.MiddleRight,
                new Vector2(.6f,0),new Vector2(1,.25f),Vector2.zero,new Vector2(-12,0));
        }

        public void AddJudgementCard(int index,string judgement,Action action)
        {
            var rt=Panel("Judgement-"+index,ModalList,Vector2.zero,Vector2.one,
                Vector2.zero,Vector2.zero,Rgb(41,56,66));
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight=106;
            var button=rt.gameObject.AddComponent<Button>();
            button.onClick.AddListener(()=>action?.Invoke());
            rt.gameObject.AddComponent<EchoFocusOutline>();
            rt.gameObject.AddComponent<EchoScrollSelection>().Target=ArchiveScroll;
            rt.gameObject.AddComponent<EchoCancelAction>().OnBack=()=>OnBack?.Invoke();
            Label("JudgementIndex",rt,"判断 "+index.ToString("00"),15,Gold,TextAnchor.MiddleLeft,
                new Vector2(0,.72f),new Vector2(1,1),new Vector2(22,0),new Vector2(-20,-4),FontStyle.Bold);
            Label("JudgementText",rt,judgement,21,White,TextAnchor.MiddleLeft,
                new Vector2(0,0),new Vector2(1,.76f),new Vector2(22,5),new Vector2(-20,-4));
            if (EventSystem.current != null &&
                (EventSystem.current.currentSelectedGameObject == null ||
                 !EventSystem.current.currentSelectedGameObject.activeInHierarchy))
                EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        public void AddBoardFeedback(string title,string explanation,bool success)
        {
            var rt=Panel("DossierFeedback",ModalList,Vector2.zero,Vector2.one,
                Vector2.zero,Vector2.zero,success?Rgb(31,66,60):Rgb(76,45,49));
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight=
                string.IsNullOrEmpty(explanation)?86:124;
            rt.GetComponent<Image>().raycastTarget=false;
            Panel("FeedbackEdge",rt,new Vector2(0,0),new Vector2(0,1),
                Vector2.zero,new Vector2(5,0),success?Gold:Red)
                .GetComponent<Image>().raycastTarget=false;
            Label("FeedbackHeading",rt,title,21,White,TextAnchor.MiddleLeft,
                new Vector2(0,.58f),Vector2.one,new Vector2(23,0),
                new Vector2(-22,-2),FontStyle.Bold);
            if(!string.IsNullOrEmpty(explanation))
                Label("FeedbackDetail",rt,explanation,17,Faded,TextAnchor.UpperLeft,
                    Vector2.zero,new Vector2(1,.6f),new Vector2(23,5),new Vector2(-20,-3));
            rt.gameObject.AddComponent<EchoDossierReveal>();
        }

        public string SelectedDossierCardId()
        {
            if(EventSystem.current==null)return "";
            var selected=EventSystem.current.currentSelectedGameObject;
            var key=selected!=null?selected.GetComponent<EchoDossierCardKey>():null;
            return key!=null?key.Id:"";
        }

        public void FocusDossierCard(string id)
        {
            if(EventSystem.current==null||string.IsNullOrEmpty(id))return;
            foreach(var key in ModalList.GetComponentsInChildren<EchoDossierCardKey>())
            {
                if(key.Id!=id||!key.name.StartsWith("DossierCard-",StringComparison.Ordinal))continue;
                var button=key.GetComponent<Button>();
                if(button!=null&&button.IsActive()&&button.IsInteractable())
                {
                    EventSystem.current.SetSelectedGameObject(button.gameObject);
                    return;
                }
            }
        }

        static RectTransform Panel(string name,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));
            var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);rt.anchorMin=min;rt.anchorMax=max;rt.offsetMin=offsetMin;rt.offsetMax=offsetMax;
            var image=go.GetComponent<Image>();image.color=color;image.raycastTarget=color.a>.05f;
            return rt;
        }
        static void Vertical(RectTransform parent,int gap)
        {
            var group=parent.gameObject.AddComponent<VerticalLayoutGroup>();group.spacing=gap;
            group.childAlignment=TextAnchor.UpperCenter;group.childControlHeight=true;group.childControlWidth=true;
            group.childForceExpandHeight=false;group.childForceExpandWidth=true;
        }
        static void Horizontal(RectTransform parent,int gap)
        {
            var group=parent.gameObject.AddComponent<HorizontalLayoutGroup>();group.spacing=gap;
            group.childAlignment=TextAnchor.MiddleCenter;group.childForceExpandWidth=true;group.childForceExpandHeight=true;
        }
        Text Label(string name,Transform parent,string content,int size,Color color,TextAnchor alignment,
            Vector2? min=null,Vector2? max=null,Vector2? offsetMin=null,Vector2? offsetMax=null,FontStyle style=FontStyle.Normal)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));var rt=go.GetComponent<RectTransform>();rt.SetParent(parent,false);
            rt.anchorMin=min??new Vector2(0,1);rt.anchorMax=max??new Vector2(1,1);
            rt.offsetMin=offsetMin??new Vector2(0,-60);rt.offsetMax=offsetMax??Vector2.zero;
            var text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=style;text.alignment=alignment;
            text.text=content;text.color=color;text.horizontalOverflow=HorizontalWrapMode.Wrap;
            text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
            return text;
        }
        Button Button(string text,Transform parent,Action action,int size,Color background,float heightRatio=.54f)
        {
            var rt=Panel("Button",parent,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,background);
            var button=rt.gameObject.AddComponent<Button>();button.onClick.AddListener(()=>action?.Invoke());
            rt.gameObject.AddComponent<EchoFocusOutline>();
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight=72f*heightRatio+29;
            Label("ButtonLabel",rt,text,size,White,TextAnchor.MiddleLeft,Vector2.zero,Vector2.one,new Vector2(18,4),new Vector2(-14,-4));
            return button;
        }
        Button ButtonAt(string title,Transform parent,Action action,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,int size,Color color)
        {
            var rt=Panel("Action",parent,min,max,offsetMin,offsetMax,color);
            var b=rt.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>action?.Invoke());
            rt.gameObject.AddComponent<EchoFocusOutline>();
            Label("Label",rt,title,size,White,TextAnchor.MiddleCenter,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            return b;
        }
        static ScrollRect Scroll(string title,Transform parent,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax,out RectTransform content)
        {
            var viewport=Panel(title,parent,min,max,offsetMin,offsetMax,Color.clear);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.GetComponent<Image>().raycastTarget = true;
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.vertical=true;scroll.scrollSensitivity=44;
            content=Panel("Content",viewport, new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero,Color.clear);
            content.pivot=new Vector2(.5f,1);scroll.content=content;scroll.viewport=viewport;
            return scroll;
        }
    }

    /// <summary>Use Unity's Submit/Cancel event, so gamepad B and Escape share one back action.</summary>
    public sealed class EchoCancelAction : MonoBehaviour, ICancelHandler
    {
        public Action OnBack;
        public void OnCancel(BaseEventData eventData)
        {
            eventData.Use();
            OnBack?.Invoke();
        }
    }

    /// <summary>Visible navigation focus for keyboard and controllers.</summary>
    public sealed class EchoFocusOutline : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        Outline outline;
        void Awake()
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = EchoGameUI.Gold;
            outline.effectDistance = new Vector2(2.5f, -2.5f);
            outline.enabled = false;
        }
        public void OnSelect(BaseEventData eventData) { if (outline != null) outline.enabled = true; }
        public void OnDeselect(BaseEventData eventData) { if (outline != null) outline.enabled = false; }
    }

    /// <summary>Keep focused choices visible in scrollable pages when using a controller/keyboard.</summary>
    public sealed class EchoScrollSelection : MonoBehaviour, ISelectHandler
    {
        public ScrollRect Target;
        public void OnSelect(BaseEventData data)
        {
            if (Target == null || Target.viewport == null || Target.content == null) return;
            UnityEngine.Canvas.ForceUpdateCanvases();
            var targetRect = transform as RectTransform;
            if (targetRect == null) return;
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(Target.viewport, targetRect);
            var visible = Target.viewport.rect;
            float move = 0f;
            if (bounds.min.y < visible.yMin + 8f) move = visible.yMin + 8f - bounds.min.y;
            else if (bounds.max.y > visible.yMax - 8f) move = visible.yMax - 8f - bounds.max.y;
            if (Mathf.Abs(move) < .01f) return;
            float range = Mathf.Max(1f, Target.content.rect.height - visible.height);
            Target.StopMovement();
            Target.verticalNormalizedPosition = Mathf.Clamp01(Target.verticalNormalizedPosition - move / range);
        }
    }
}

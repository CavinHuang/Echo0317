using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Echo0317.Core;
using Echo0317.UI;

namespace Echo0317.Runtime
{
    public sealed class EchoGame : MonoBehaviour
    {
        GameCatalog catalog;
        EchoSaves saves;
        EchoPreferences preferences;
        EchoGameUI ui;
        EchoVideoDirector video;
        GameProgress state;
        bool playing, paused, captionsEnabled = true, muted, fullscreen = true;
        float subtitleScale = 1f;
        readonly EchoModalFlow modalFlow = new EchoModalFlow();
        int lastBackFrame = -1;
        string deductionFeedback = "";
        float lastCaptionTick, discoveryUntil;
        bool autosaveFailed;
        int historyPage;
        static readonly string[] EndingIds = { "ending_merge", "ending_loop", "ending_stop", "ending_audit" };
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Startup()
        {
            if (EchoNativeQa.Requested)
            {
                // Run in the *compiled player*, without initializing UI or
                // video decoders that need a desktop graphics device.
                var diagnostic = new GameObject("03:17 · Native Player QA");
                DontDestroyOnLoad(diagnostic);
                diagnostic.AddComponent<EchoNativeQa>();
                return;
            }
            // Automated PlayMode tests create their own isolated UI and video
            // instances. Auto-bootstrapping a second game on the test scene
            // causes duplicate event systems and stale video decoders.
            if (Application.isEditor && Environment.GetCommandLineArgs().Any(a => a == "-runTests")) return;
            if (FindFirstObjectByType<EchoGame>() != null) return;
            var go = new GameObject("03:17 · Echo Runtime");
            DontDestroyOnLoad(go);
            go.AddComponent<EchoGame>();
        }
        void Awake()
        {
            try
            {
                // Construct a visible native UI before reading external data.
                // Missing StreamingAssets must never leave a blank game window.
                ui = new EchoGameUI();
                catalog = GameCatalog.LoadFromStreamingAssets();
                saves = EchoSaves.OpenDefault(catalog);
                preferences = EchoPreferences.OpenDefault();
                captionsEnabled = preferences.Current.captionsEnabled;
                muted = preferences.Current.muted;
                fullscreen = preferences.Current.fullscreen;
                subtitleScale = preferences.Current.subtitleScale;
                ui.SetSubtitleOptions(captionsEnabled, subtitleScale);
                video = gameObject.AddComponent<EchoVideoDirector>();
                video.Initialize(catalog, ui.VideoLayer);
                video.SetMuted(muted);
                video.SetSpeed(preferences.Current.speed);
                ApplyDisplaySettings();
                video.OnClipError = (err) => Debug.LogWarning("[Echo0317] video fallback: " + err);
                video.OnEnded = UpdateCaption;
                ui.OnContinue = Continue;
                ui.OnNewGame = RequestNewGame;
                ui.OnSettings = () => Open("settings");
                ui.OnArchive = () => Open("evidence");
                ui.OnHistory = () => { historyPage = 0; Open("history"); };
                ui.OnSaves = () => Open("saves");
                ui.OnPause = Pause;
                ui.OnCloseModal = HandleBack;
                ui.OnBack = HandleBack;
                try
                {
                    state = saves.HasAuto ? saves.LoadAuto() : EchoNarrative.Fresh();
                }
                catch (Exception saveError)
                {
                    Debug.LogWarning("[Echo0317] 自动存档无效，已回退到新游戏，原存档未删除: " + saveError.Message);
                    state = EchoNarrative.Fresh();
                }
                RefreshMenu();
                ui.SetGameVisible(false);
                video.SetSceneArt("S01");
                AddMenuArtwork();
                Debug.Log("ECHO_GAME_READY: main menu and content initialized");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (ui != null) ui.ShowStartupError(e.Message);
            }
        }
        void AddMenuArtwork()
        {
            var file = Path.Combine(Application.streamingAssetsPath, "Art", "S01.png");
            if (!File.Exists(file)) return;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(file))) { Destroy(texture); return; }
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
            var img = ui.MenuBackground.GetComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = false;
        }
        void RefreshMenu()
        {
            ui.ContinueButton.interactable = state != null && (state.history.Length > 0 || saves.HasAuto);
            var chapter = catalog.Nodes[state.node].chapterIndex;
            ui.MenuProgress.text = "CHAPTER " + chapter.ToString("00") + " / 05    ·    已解锁结局 " + state.endings.Length + " / 4   ·   内部测试版" +
                (autosaveFailed ? "\n⚠ 自动存档写入失败，请检查存储目录" : "");
        }
        void ApplyDisplaySettings()
        {
            // Screen changes apply only to a built player; Editor PlayMode must
            // not change the developer's own desktop settings.
            if (!Application.isEditor)
                Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
        void PersistPreferences()
        {
            try { preferences.Update(captionsEnabled, muted, fullscreen, video.Speed, subtitleScale); }
            catch (Exception e) { Debug.LogWarning("设置无法持久保存: " + e.Message); }
        }
        void RequestNewGame()
        {
            if (state == null || state.history == null || state.history.Length == 0)
            {
                NewGame();
                return;
            }
            if (playing && !paused) { paused = true; video.Pause(); }
            modalFlow.Confirm("");
            ui.SetModal(true);
            ui.ClearModal();
            ui.ModalTitle.text = "开始新的轮回？";
            ui.AddModalText("当前进度将被重新开始", "已经取得的结局图鉴仍会保留。已保存到手动槽位的进度不会删除。请先确认，避免误覆盖自动存档。");
            ui.AddModalButton("确认 · 开始新轮回", NewGame);
            ui.AddModalButton("取消 · 保留当前进度", HandleBack);
        }
        void NewGame()
        {
            var previous = state?.endings ?? Array.Empty<string>();
            ui.SetModal(false); modalFlow.Reset();
            video.Halt(); ui.HideDiscovery(); discoveryUntil = 0f;
            state = EchoNarrative.Fresh(); state.endings = previous.Distinct().ToArray();
            historyPage = 0;
            Persist();
            Begin();
        }
        void Continue()
        {
            if (state == null) return;
            Begin();
        }
        void Begin()
        {
            playing = true; paused = false;
            ui.SetGameVisible(true);
            video.SetMuted(muted);
            Render();
        }
        void Pause()
        {
            if (!playing) return;
            paused = true; video.Pause();
            Open("pause");
        }
        void ReturnToMenu()
        {
            // Returning to the menu must not Resume() the last film for one frame.
            ui.SetModal(false); modalFlow.Reset(); playing = false; paused = false;
            video.Halt(); video.SetSceneArt("S01"); ui.HideDiscovery(); discoveryUntil = 0f;
            ui.SetGameVisible(false); ui.HideDiscovery(); RefreshMenu();
        }
        void CloseModal()
        {
            ui.SetModal(false);
            modalFlow.Reset();
            if (paused) { paused=false;video.Resume(); }
        }
        void HandleBack()
        {
            // Escape can be dispatched by both MonoBehaviour.Update and
            // StandaloneInputModule.Cancel in the same frame. Handle it once.
            if (lastBackFrame == Time.frameCount) return;
            lastBackFrame = Time.frameCount;
            if (ui == null) return;
            if (!ui.Modal.activeSelf)
            {
                if (playing) Pause();
                return;
            }
            var target = modalFlow.Back();
            if (string.IsNullOrEmpty(target)) CloseModal();
            else Open(target);
        }
        void Persist()
        {
            try
            {
                saves.AutoSave(state);
                autosaveFailed = false;
                ui.SetSaveWarning(false);
            }
            catch (Exception e)
            {
                autosaveFailed = true;
                ui.SetSaveWarning(true);
                Debug.LogError("自动保存失败: " + e.Message);
            }
        }
        IEnumerator ScrollStoryTopNextFrame()
        {
            yield return null; // let the vertical layout update after deferred child destruction
            if (ui != null && playing) ui.ResetStoryScroll();
        }
        static string SceneFor(StoryNode n)
        {
            if (n.clip == "V02" || n.clip == "V04" || n.clip == "V13" || n.stage == "peephole") return "S02";
            if (n.clip == "V08" || n.clip == "V07") return "S04";
            if (n.clip == "V10" || n.clip == "V11") return "S05";
            if (n.clip == "V12" || n.clip == "V15" || n.clip == "V17") return "S03";
            if (n.chapterIndex == 5 && n.ending) return "S05";
            return "S01";
        }
        string ClipFor(StoryNode n)
        {
            if (state.node == "c1_password" || state.node == "c1_hook") return state.flags.Contains("pressed_lock") ? "V24" : "V24B";
            if (n.clip == "V13") return state.node == "c4_door" ? "V13A" : "V13B";
            if (n.clip == "V07") return state.flags.Contains("incident_proof") ? "V07S" : "V07P";
            if (n.clip == "V17" && state.node != "c5_sign") return "V15";
            return n.clip;
        }
        void Render()
        {
            var n = catalog.Nodes[state.node];
            ui.Chapter.text = "第 " + n.chapterIndex + " 幕  /  " + catalog.ChapterName(n.chapterIndex);
            ui.Clock.text = n.clock ?? "03:17";
            ui.Speaker.text = n.speaker ?? "";
            ui.Title.text = n.title ?? "";
            ui.Body.text = string.Join("\n\n", n.lines ?? Array.Empty<string>());
            string clip = ClipFor(n);
            video.SetSceneArt(SceneFor(n));
            video.Show(clip);
            bool isApproved = catalog.ShotTasks.TryGetValue(clip, out var record) && record.status == "approved_final";
            ui.FilmStatus.text = "镜头 " + clip + "  ·  " + (isApproved ? "已批准影片" : "占位 / 动态分镜") + "   |   R 重播 / K 跳过画面";
            ui.Hint.text = n.ending ? "结局解锁 · 你可以回溯以探寻其他选择" : "选择会改变后来你能掌握的证据与可能的结局";
            ui.ClearChoices();
            if (n.ending)
            {
                ui.AddChoice("结局图鉴", () => Open("endings"));
                ui.AddChoice("从头开始新的轮回", RequestNewGame);
                ui.AddChoice("返回主菜单", ReturnToMenu);
            }
            else
            {
                var options = EchoNarrative.Choices(n);
                for (int i=0;i<options.Length;i++)
                {
                    var choice = options[i];
                    bool usable = EchoNarrative.Allowed(choice, state.flags);
                    string label = (i+1) + ".  " + choice.label;
                    if (!usable) label += "    [条件未满足]";
                    ui.AddChoice(label, () => Choose(n.id, choice), usable);
                }
                var cases = catalog.Cases.rules.Count(r => r.clues.All(state.flags.Contains) && !EchoNarrative.IsSolved(catalog,state,r.id));
                if (cases > 0) ui.AddChoice("打开证据推理 · 有 " + cases + " 道可解谜题", () => Open("cases"));
            }
            UpdateCaption();
            ui.ResetStoryScroll();
            StartCoroutine(ScrollStoryTopNextFrame());
        }
        void Choose(string expectedNode, StoryChoice choice)
        {
            if (!playing || paused || ui.Modal.activeSelf || expectedNode != state.node) return;
            try
            {
                var before = new HashSet<string>(state.flags);
                state = EchoNarrative.Advance(catalog, state, choice);
                Persist();
                Render();
                var clues = state.flags.Where(id => !before.Contains(id) && catalog.Evidence.ContainsKey(id))
                    .Select(catalog.EvidenceName).Take(3).ToArray();
                if (clues.Length > 0)
                {
                    ui.ShowDiscovery("新线索已收录 · " + string.Join(" / ", clues));
                    discoveryUntil = Time.unscaledTime + 3.5f;
                }
            }
            catch (Exception e)
            {
                Debug.LogError("剧情推进失败: " + e.Message);
                ui.ShowDiscovery("无法继续剧情 · " + e.Message);
                discoveryUntil = Time.unscaledTime + 6f;
            }
        }
        void Open(string page)
        {
            if (ui == null || state == null) return;
            // Investigation and save panels pause the film so users cannot miss
            // dialogue while reading long notes or editing a save slot.
            if (playing && !paused) { paused = true; video.Pause(); }
            int restoreAction = ui.Modal.activeSelf && modalFlow.Current == page && page == "settings"
                ? ui.SelectedModalActionIndex() : -1;
            modalFlow.Open(page);
            ui.SetModal(true);
            ui.ClearModal();
            switch(page)
            {
                case "pause":
                    ui.ModalTitle.text = "暂停 / PAUSE";
                    ui.AddModalText("03:17 · 回声", "世界暂停了，但你留下的选择不会消失。");
                    ui.AddModalButton("继续游戏", CloseModal);
                    ui.AddModalButton("线索档案",()=>Open("evidence"));
                    ui.AddModalButton("剧情回顾",()=>{historyPage=0;Open("history");});
                    ui.AddModalButton("存档管理",()=>Open("saves"));
                    ui.AddModalButton("设置",()=>Open("settings"));
                    ui.AddModalButton("返回主菜单",ReturnToMenu);
                    ui.ResetModalScroll();
                    return;
                case "history": ShowHistory(); break;
                case "evidence": ShowEvidence(); break;
                case "cases": ShowCases(); break;
                case "chapters": ShowChapters(); break;
                case "timeline": ShowTimeline(); break;
                case "endings": ShowEndings(); break;
                case "saves": ShowSaves(); break;
                case "settings": ShowSettings(); break;
                default: ShowEvidence();break;
            }
            if (page != "settings")
            {
                ui.AddModalText("────────────────────────────────────────");
                if (page != "history")ui.AddModalButton("剧情回顾",()=>{historyPage=0;Open("history");});
                if (page != "evidence")ui.AddModalButton("线索",()=>Open("evidence"));
                if (page != "cases")ui.AddModalButton("证据推理",()=>Open("cases"));
                if (page != "timeline")ui.AddModalButton("时间线 / 回溯",()=>Open("timeline"));
                if (page != "chapters")ui.AddModalButton("章节复盘",()=>Open("chapters"));
                if (page != "endings")ui.AddModalButton("结局图鉴",()=>Open("endings"));
                if (page != "saves")ui.AddModalButton("存档",()=>Open("saves"));
            }
            ui.ResetModalScroll();
            if (restoreAction >= 0) ui.FocusModalAction(restoreAction);
        }
        void ShowHistory()
        {
            // This read-only view follows the normalized current save path.
            // Abandoned branches and unvisited nodes are never listed.
            var page = EchoDialogueHistory.Read(catalog, state, historyPage);
            historyPage = page.Page;
            ui.ModalTitle.text = "剧情回顾 / DIALOGUE LOG";
            ui.AddModalText("本轮已经历 " + page.TotalEntries + " 个场景 · 第 " +
                (page.Page + 1) + " / " + (page.MaxPage + 1) + " 页",
                "只显示本轮已阅读的对白与实际选择。回顾不会回溯或修改游戏进度。");
            foreach (var entry in page.Entries)
            {
                string title = (entry.IsCurrent ? "● 当前场景 · " : "") +
                    "第 " + entry.Chapter.ToString("00") + " 幕 · " + entry.Clock + " · " + entry.Title;
                string speaker = string.IsNullOrWhiteSpace(entry.Speaker) ? "旁白" : entry.Speaker;
                string body = speaker + "\n" + string.Join("\n", entry.Lines);
                if (!string.IsNullOrEmpty(entry.Choice))
                    body += "\n\n本轮选择 → " + entry.Choice;
                ui.AddModalText(title, body);
            }
            if (page.HasOlder)
                ui.AddModalButton("更早的对白", () => { historyPage++; Open("history"); });
            if (page.HasNewer)
                ui.AddModalButton("更新的对白", () => { historyPage--; Open("history"); });
        }

        void ShowEvidence()
        {
            ui.ModalTitle.text = "线索档案 / CASE FILE";
            ui.AddModalText("你真正收集到的证据", "只有经过这一轮调查获得的线索才会显示。\n未发现的真相不会被提前剧透。");
            if (state.flags.Length == 0) ui.AddModalText("暂无记录。开门之前，再核实一件事。");
            foreach (var id in state.flags) ui.AddModalText("▣  " + catalog.EvidenceName(id));
        }
        void ShowCases()
        {
            ui.ModalTitle.text = "证据推理 / DEDUCTION";
            if (!string.IsNullOrEmpty(deductionFeedback))
            {
                ui.AddModalText(deductionFeedback);
                deductionFeedback = "";
            }
            ui.AddModalText("两份证据，一个判断", "推理可以选错并重新修正，但不能凭空获得尚未调查到的线索。");
            foreach (var rule in catalog.Cases.rules)
            {
                bool solved = EchoNarrative.IsSolved(catalog,state,rule.id);
                var have = rule.clues.Where(state.flags.Contains).ToArray();
                ui.AddModalText("第 " + rule.chapter + " 幕 · " + rule.title + (solved ? "  ✓" : ""),
                    string.Join("\n", rule.clues.Select(id => state.flags.Contains(id) ? "✓ " + catalog.EvidenceName(id) : "？ 未取得的证据")));
                if (solved) { ui.AddModalText(rule.explanation); continue; }
                if (have.Length < rule.clues.Length) { ui.AddModalText("请先从剧情中收集齐证据。"); continue; }
                foreach (var option in rule.options)
                {
                    var opt = option;
                    ui.AddModalButton("推断：" + opt.text,()=>TryDeduction(rule.id,opt.id));
                }
                ui.AddModalText("提示：" + rule.hint);
            }
        }
        void TryDeduction(string ruleId,string answer)
        {
            try
            {
                state = EchoNarrative.RecordDeduction(catalog,state,ruleId,answer);
                Persist();
                var rule = catalog.Rules[ruleId];
                bool success = rule.correct == answer;
                // Put the outcome at the TOP of the refreshed casebook, not
                // below all five cases where it is invisible after scroll reset.
                deductionFeedback = success ? "推理成立：" + rule.explanation : "推理尚不成立：" + rule.hint;
                Open("cases");
            }
            catch(Exception e) { ui.AddModalText("无法提交：" + e.Message); }
        }
        void ShowTimeline()
        {
            ui.ModalTitle.text = "时间线 / REWIND";
            ui.AddModalText("时间可以重来，证据不会凭空保留。", "回溯将删除回溯点之后的本轮线索和推理，但保留已解锁结局。");
            var points=EchoNarrative.Trace(catalog,state.history).Points;
            var discoveredChapters=new HashSet<int>();
            foreach(var point in points)
            {
                var n=catalog.Nodes[point.Node];
                bool chapterStart=discoveredChapters.Add(n.chapterIndex);
                bool decision=EchoNarrative.Choices(n).Length>1 && point.Index<state.history.Length;
                if(!chapterStart&&!decision)continue;
                int index=point.Index;
                string label=(chapterStart ? "第 " + n.chapterIndex + " 幕 · " : "关键选择 · ") + n.title;
                ui.AddModalButton(label + "  ·  重玩",()=>ConfirmRewindTo(index));
            }
        }
        void ConfirmRewindTo(int index)
        {
            modalFlow.Confirm("timeline");
            ui.SetModal(true);
            ui.ClearModal();
            ui.ModalTitle.text = "确定回溯这条时间线？";
            ui.AddModalText("回溯至第 " + index + " 步", "之后的选择、线索和推理会从本轮移除；已解锁结局保留。\n建议先将当前轮回保存到手动槽位。");
            ui.AddModalButton("确认 · 回溯", () => RewindTo(index));
            ui.AddModalButton("取消 · 返回时间线", () => Open("timeline"));
            ui.ResetModalScroll();
        }
        void ReplaceCurrentProgress(GameProgress loaded)
        {
            // Loading/rewinding must never let an old shot resume or leak old
            // dialogue while the replacement shot is still being prepared.
            video.Halt();
            ui.HideDiscovery();
            discoveryUntil = 0f;
            state = loaded;
            historyPage = 0;
            Persist();
            CloseModal();
            if (!playing) Begin(); else Render();
        }
        void RewindTo(int index)
        {
            try { ReplaceCurrentProgress(EchoNarrative.Rewind(catalog, state, index)); }
            catch (Exception e) { ui.AddModalText("回溯失败：" + e.Message); }
        }
        void ShowChapters()
        {
            ui.ModalTitle.text="章节复盘 / CHAPTERS";
            foreach(var r in EchoNarrative.Reports(catalog,state))
            {
                ui.AddModalText("第 "+r.chapter+" 幕 · "+r.name+(r.finished?"  ✓ 已完成":"  · 进行中"),
                    "本幕获得的证据："+(r.discovered.Length==0?"无":string.Join(" / ",r.discovered.Select(catalog.EvidenceName)))+
                    "\n推理："+(r.solved.Length==0?"尚未完成":string.Join(" / ",r.solved.Select(id=>catalog.Rules[id].title)))+
                    "\n行动风险："+(r.risks.Length==0?"无":string.Join(" / ",r.risks.Select(catalog.EvidenceName))));
            }
        }
        void ShowEndings()
        {
            ui.ModalTitle.text="结局图鉴 / ENDINGS";
            ui.AddModalText("已解锁 "+state.endings.Length+" / 4", "未知结局的标题不会提前显示。");
            foreach(var id in EndingIds)ui.AddModalText(state.endings.Contains(id) ? "✓ "+catalog.Nodes[id].endingLabel : "◇ ？？？ · 尚未解锁");
        }
        void ShowSaves()
        {
            ui.ModalTitle.text="本地存档 / SAVES";
            ui.AddModalText("三个独立存档槽", "文件保存在系统用户目录。自动存档在每次剧情选择后更新。\n数据完整性与剧情路径会在读取时重新校验。");
            for(int number=1;number<=3;number++)
            {
                int slot=number;
                ui.AddModalText("存档位 "+slot+(saves.HasSlot(slot)?"  ● 已使用":"  ○ 空"));
                ui.AddModalButton("保存到槽位 "+slot,()=>{
                    if (saves.HasSlot(slot)) ConfirmSlotAction(slot, true);
                    else SaveToSlot(slot);
                });
                if(saves.HasSlot(slot))
                {
                    ui.AddModalButton("读取槽位 "+slot,()=>ConfirmLoadSlot(slot));
                    ui.AddModalButton("删除槽位 "+slot,()=>ConfirmSlotAction(slot, false));
                }
            }
            ui.AddModalButton("导出本轮存档 JSON",()=>{try{var path=saves.Export(state);ui.AddModalText("已导出："+path);}catch(Exception e){ui.AddModalText(e.Message);}});
            ui.AddModalText("导入 Stage 8 网页版存档", "将旧游戏导出的 JSON 放在以下路径：\n"+Path.Combine(saves.Folder,"import-stage8.json"));
            ui.AddModalButton("读取旧版 JSON", ConfirmOldImport);
        }
        void ConfirmOldImport()
        {
            modalFlow.Confirm("saves");
            ui.SetModal(true);
            ui.ClearModal();
            ui.ModalTitle.text = "导入旧版存档？";
            ui.AddModalText("当前自动进度会被导入数据替换",
                "仅使用从 Stage 8 正式导出的存档 JSON。导入前建议保存当前进度到手动槽位。");
            ui.AddModalButton("确认导入", () => {
                try { ReplaceCurrentProgress(saves.ImportOldStage8Save(
                    Path.Combine(saves.Folder, "import-stage8.json"))); }
                catch (Exception e) { ui.AddModalText("导入失败：" + e.Message); }
            });
            ui.AddModalButton("取消 · 返回存档", () => Open("saves"));
            ui.ResetModalScroll();
        }
        void ConfirmLoadSlot(int slot)
        {
            modalFlow.Confirm("saves");
            ui.SetModal(true);
            ui.ClearModal();
            ui.ModalTitle.text = "确认读取存档？";
            ui.AddModalText("槽位 " + slot + " 将替换当前剧情进度",
                "当前自动存档会被新读取的剧情状态覆盖。手动槽位不会被删除。建议先将当前轮回保存到其他槽位。 ");
            ui.AddModalButton("确认读取槽位 " + slot, () => {
                try
                {
                    var loaded = saves.LoadSlot(slot); // validate before altering current state
                    ReplaceCurrentProgress(loaded);
                }
                catch (Exception e) { ui.AddModalText("读取失败：" + e.Message); }
            });
            ui.AddModalButton("取消 · 返回存档", () => Open("saves"));
            ui.ResetModalScroll();
        }
        void SaveToSlot(int slot)
        {
            try { saves.SaveSlot(slot, state); Open("saves"); }
            catch (Exception e) { ui.AddModalText("保存失败：" + e.Message); }
        }
        void ConfirmSlotAction(int slot, bool overwrite)
        {
            modalFlow.Confirm("saves");
            ui.SetModal(true);
            ui.ClearModal();
            ui.ModalTitle.text = overwrite ? "确认覆盖存档？" : "确认删除存档？";
            ui.AddModalText("槽位 " + slot + (overwrite ? " 的旧进度将被覆盖。" : " 及其备份将被永久删除。"),
                "你可以取消操作返回存档管理。不会影响其他存档槽。 ");
            ui.AddModalButton(overwrite ? "确认覆盖" : "确认删除", () => {
                try
                {
                    if (overwrite) saves.SaveSlot(slot, state);
                    else saves.DeleteSlot(slot);
                    Open("saves");
                }
                catch (Exception e) { ui.AddModalText("操作失败：" + e.Message); }
            });
            ui.AddModalButton("取消 · 返回存档", () => Open("saves"));
            ui.ResetModalScroll();
        }
        void ShowSettings()
        {
            ui.ModalTitle.text="设置 / OPTIONS";
            ui.AddModalText("播放与字幕", "这里的设置不影响存档中的剧情选择。");
            ui.AddModalButton("字幕："+(captionsEnabled?"开启":"关闭"),()=>{
                captionsEnabled=!captionsEnabled;
                ui.SetSubtitleOptions(captionsEnabled, subtitleScale);
                PersistPreferences();Open("settings");UpdateCaption();
            });
            foreach (var size in new []{1f,1.25f,1.5f})
            {
                float selectedSize = size;
                ui.AddModalButton("字幕字号 " + (int)(size*100f) + "%" +
                    (Mathf.Abs(subtitleScale-size)<.01f?"  ✓":""),()=>{
                        subtitleScale=selectedSize;
                        ui.SetSubtitleOptions(captionsEnabled, subtitleScale);
                        PersistPreferences();Open("settings");
                    });
            }
            ui.AddModalButton("声音："+(muted?"静音":"开启"),()=>{muted=!muted;video.SetMuted(muted);PersistPreferences();Open("settings");});
            foreach(var speed in new []{.75f,1f,1.25f,1.5f})
            {
                float s=speed;
                ui.AddModalButton("播放速度 " + speed.ToString("0.##") + "×"+(Mathf.Abs(video.Speed-speed)<.01f?"  ✓":""),()=>{video.SetSpeed(s);PersistPreferences();Open("settings");});
            }
            ui.AddModalButton("显示模式：" + (fullscreen ? "无边框全屏" : "窗口"), () => {
                fullscreen = !fullscreen; ApplyDisplaySettings(); PersistPreferences(); Open("settings");
            });
            ui.AddModalButton("重播当前影片镜头（R）",()=>{video.Replay();CloseModal();});
            ui.AddModalButton("跳过当前影片画面（K，不跳剧情）",()=>{video.Skip();CloseModal();});
            ui.AddModalButton("返回主菜单",ReturnToMenu);
        }
        void UpdateCaption()
        {
            if (ui==null || catalog==null || state==null) return;
            if(!captionsEnabled){ui.Subtitle.text="";return;}
            string content=catalog.Cue(video.CurrentClip,video.CurrentTime);
            if(string.IsNullOrEmpty(content)&&!video.IsPlaying)
            {
                var n=catalog.Nodes[state.node];content=n.lines.Length>0?n.lines[n.lines.Length-1]:"";
            }
            ui.Subtitle.text=content;
        }
        void Update()
        {
            if (ui==null || catalog==null || state==null)return;
            if (discoveryUntil > 0f && Time.unscaledTime >= discoveryUntil)
            {
                ui.HideDiscovery();
                discoveryUntil = 0f;
            }
            if(Time.unscaledTime-lastCaptionTick>.12f){lastCaptionTick=Time.unscaledTime;UpdateCaption();}
            if(Input.GetKeyDown(KeyCode.Escape)) { HandleBack(); return; }
            if(!playing || paused || ui.Modal.activeSelf) return;
            if(Input.GetKeyDown(KeyCode.Tab)){Open("evidence");return;}
            if(Input.GetKeyDown(KeyCode.L)){historyPage=0;Open("history");return;}
            // Replay/skip are video-only and never advance or mutate story progress.
            if(Input.GetKeyDown(KeyCode.R)){video.Replay();return;}
            if(Input.GetKeyDown(KeyCode.K)){video.Skip();UpdateCaption();return;}
            var node=catalog.Nodes[state.node];var choices=EchoNarrative.Choices(node);
            for(int i=0;i<Math.Min(9,choices.Length);i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1+i)))
                {
                    // Keyboard controls must obey the same lock rules as UI buttons.
                    if (EchoNarrative.Allowed(choices[i], state.flags)) Choose(state.node, choices[i]);
                    return;
                }
            }
            // Enter/Space/gamepad Submit are handled ONLY by the focused uGUI
            // Button. Calling Choose() here as well can advance two single-
            // choice nodes on the same keypress when EventSystem fires first.
        }
    }
}

// Standalone QA for the real Unity C# narrative core. Use `dotnet run` on
// GitHub-hosted Windows without Unity activation. Does NOT validate gameplay UI.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using Echo0317.Core;
using UnityEngine;

static class Program
{
    static void Require(bool good, string message)
    {
        if (!good) throw new Exception("Core C# QA failed: " + message);
    }
    static int Main(string[] args)
    {
        try
        {
            var root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
            Application.streamingAssetsPath = Path.Combine(root, "Assets", "StreamingAssets");
            var data = Path.Combine(Application.streamingAssetsPath, "Data");
            var catalog = GameCatalog.LoadFromStreamingAssets();
            Require(catalog.Nodes.Count == 55 && catalog.Rules.Count == 5, "narrative catalog missing");
            Require(catalog.Hotspots.Count == 23, "investigation hotspot catalog missing");
            // Stage 31: authored first-act Foley and subtitle behavior run
            // against the actual Unity-shared C# classes, not a Python mock.
            Require(EchoFirstActFoleyPlan.Valid(),
                "first act audio schedule contains invalid timings or gains");
            var firstActIds = catalog.Nodes.Values.Where(n => n.chapterIndex == 1)
                .Select(n => n.id).Distinct().ToArray();
            Require(firstActIds.Length == 10 &&
                firstActIds.All(id => EchoFirstActFoleyPlan.ForNode(id).Length > 0),
                "all ten playable first-act scenes need their own sound schedule");
            var knocks = EchoFirstActFoleyPlan.ForNode("c1_knock");
            Require(knocks.Length == 3 && knocks.All(c => c.Name == "door-knock") &&
                knocks[0].AtSeconds < knocks[1].AtSeconds &&
                knocks[1].AtSeconds < knocks[2].AtSeconds,
                "three door knocks must occur in authored temporal order");
            var firstActSoundCopy = EchoFirstActFoleyPlan.ForNode("intro");
            firstActSoundCopy[0].Gain = 999f;
            Require(EchoFirstActFoleyPlan.ForNode("intro")[0].Gain < .4f,
                "Foley schedule must be immutable from outside the catalog");
            Require(EchoFirstActFoleyPlan.ForNode("ending_merge").Length == 0,
                "completed ending scenes must not accidentally replay first-act sound");
            Require(EchoSubtitlePresentation.Select(false, false, false,
                    "should not display", new[]{"final line"}) == "" &&
                EchoSubtitlePresentation.Select(true, true, false,
                    "stale preview cue", new[]{"final line"}) == "",
                "disabled/loading captions must never reveal old or future dialogue");
            Require(EchoSubtitlePresentation.Select(true, false, true,
                    "actual timed cue", new[]{"final line"}) == "actual timed cue" &&
                EchoSubtitlePresentation.Select(true, false, true,
                    "", new[]{"final line"}) == "",
                "active or paused real movie must show timed cues, not last narrative line");
            Require(EchoSubtitlePresentation.Select(true, false, false,
                    "stale preview cue", new[]{"","scene fallback"}) == "scene fallback" &&
                EchoSubtitlePresentation.Select(true, false, false,
                    "old", Array.Empty<string>()) == "",
                "missing or finished video must show only current scene prose");

            var firstActReactions = EchoScenePresentation.Lines(catalog.Nodes["c1_hook"],
                new[]{"tested_password"}, Array.Empty<Attempt>());
            Require(firstActReactions.Any(line=>line.Contains("知道她过去怎么说")) &&
                    !firstActReactions.Any(line=>line.Contains("照片背面写着")),
                "existing first-act evidence consequences are not visible in runtime");

            // Stage 26: real Unity hotspot navigation decisions are compiled
            // and checked here on the same authored source coordinates.
            var introNavigation = EchoHotspotInvestigation.Visible(catalog, EchoNarrative.Fresh());
            Require(EchoHotspotNavigation.Next(introNavigation, "intro_phone",
                    EchoHotspotDirection.Right) == "intro_photo",
                "gamepad right must move to the adjacent opening hotspot");
            Require(EchoHotspotNavigation.Next(introNavigation, "intro_photo",
                    EchoHotspotDirection.Left) == "intro_phone",
                "gamepad left must return to the original hotspot");
            Require(EchoHotspotNavigation.Next(introNavigation, "missing",
                    EchoHotspotDirection.Right) == null &&
                    EchoHotspotNavigation.Next(Array.Empty<HotspotDefinition>(), "intro_phone",
                    EchoHotspotDirection.Right) == null,
                "unknown or empty hotspot navigation must safely fail");
            var deterministic = new [] {
                new HotspotDefinition { id="center", x=0.2f, y=0.3f, w=0.1f, h=0.1f },
                new HotspotDefinition { id="right-b", x=0.6f, y=0.3f, w=0.1f, h=0.1f },
                new HotspotDefinition { id="right-a", x=0.6f, y=0.3f, w=0.1f, h=0.1f }
            };
            Require(EchoHotspotNavigation.Next(deterministic, "center",
                    EchoHotspotDirection.Right) == "right-a" &&
                    EchoHotspotNavigation.Next(deterministic, "center",
                    EchoHotspotDirection.Left) == null,
                "directional navigation must be stable on tied or missing neighbors");

            // Play and save actual spotlight observations without bypassing
            // canonical narrative flags; no Unity license or test shims for rules.
            var hotspotFresh = EchoNarrative.Fresh();
            Require(EchoHotspotInvestigation.Visible(catalog, hotspotFresh).Length == 2,
                "opening film hotspot targets are unavailable");
            var emptyJournal = EchoInvestigationJournal.Read(catalog, hotspotFresh);
            Require(emptyJournal.TotalEntries == 0 && emptyJournal.Page == 0 &&
                emptyJournal.Entries.Length == 0, "new loop must have an empty observation notebook");
            var hotVisited = EchoHotspotInvestigation.Inspect(catalog, hotspotFresh, "intro_phone");
            Require(hotVisited.inspections.Length == 1 && hotVisited.flags.Length == 0 &&
                hotVisited.history.Length == 0, "an observation must not create story flags or steps");
            Require(EchoHotspotInvestigation.Inspect(catalog, hotVisited, "intro_phone").inspections.Length == 1,
                "repeated inspection must not duplicate records");
            var journal = EchoInvestigationJournal.Read(catalog, hotVisited);
            Require(journal.TotalEntries == 1 && journal.Entries.Length == 1 &&
                journal.Entries[0].Id == "intro_phone" && journal.Entries[0].Step == 0 &&
                journal.Entries[0].Chapter == 1 && !journal.Entries[0].EvidenceVerified,
                "observed-only hotspot should appear in read-only journal");
            Require(EchoInvestigationJournal.Read(catalog, hotVisited, -500, 999).Page == 0 &&
                EchoInvestigationJournal.Read(catalog, hotVisited, 999, 1).Page == 0,
                "journal pagination must be safely clamped");
            Require(!EchoHotspotInvestigation.IsEvidenceVerified(catalog, hotVisited, "intro_phone"),
                "a hotspot must not pretend an unearned receipt is verified");
            bool rejectedForgedHotspot = false;
            try
            {
                EchoNarrative.Normalize(catalog, new GameProgress {
                    node = "intro", inspections = new[] {
                        new HotspotVisit { id = "c2_wave_match", node = "c2_voice", at = 0 }
                    }
                });
            }
            catch (InvalidOperationException) { rejectedForgedHotspot = true; }
            Require(rejectedForgedHotspot, "forged future hotspot visit was accepted");
            var warningChoice = EchoNarrative.Choices(catalog.Nodes["intro"]).Single();
            var atWarning = EchoNarrative.Advance(catalog, hotspotFresh, warningChoice);
            var peepholeChoice = EchoNarrative.Choices(catalog.Nodes["warning"])
                .Single(c => c.to == "c1_peephole");
            var atPeephole = EchoNarrative.Advance(catalog, atWarning, peepholeChoice);
            Require(atPeephole.flags.Contains("shadow"),
                "story evidence should be granted by the actual choice");
            var inspectedPeephole = EchoHotspotInvestigation.Inspect(catalog, atPeephole, "peephole_shadow");
            Require(EchoHotspotInvestigation.IsEvidenceVerified(catalog, inspectedPeephole, "peephole_shadow"),
                "actual evidence should be recognized in the hotspot inspection");
            var verifiedJournal = EchoInvestigationJournal.Read(catalog, inspectedPeephole);
            Require(verifiedJournal.TotalEntries == 1 &&
                verifiedJournal.Entries[0].EvidenceVerified &&
                verifiedJournal.Entries[0].EvidenceId == "shadow",
                "journal must acknowledge evidence earned by the actual story choice");
            var rewoundHotspots = EchoNarrative.Rewind(catalog, inspectedPeephole, 0);
            Require(EchoInvestigationJournal.Read(catalog, rewoundHotspots).TotalEntries == 0,
                "abandoned journal observations leaked through rewind");
            Require(rewoundHotspots.inspections.Length == 0 &&
                EchoHotspotInvestigation.Visible(catalog, rewoundHotspots).Length == 2,
                "rewind kept visits or revealed an abandoned scene");
            var evidenceAfterInspect = EchoInvestigation.Archive(catalog, inspectedPeephole);
            Require(evidenceAfterInspect.Any(e => e.Id == "shadow") &&
                inspectedPeephole.flags.SequenceEqual(atPeephole.flags),
                "inspecting should not alter the existing evidence archive");

            // Stage30: the first-act detective sequence now changes the
            // dialogue the player actually sees, but not the canonical flags.
            var introExamined = EchoHotspotInvestigation.Inspect(catalog,
                EchoNarrative.Fresh(), "intro_phone");
            Require(EchoScenePresentation.Lines(catalog.Nodes["intro"],
                introExamined.flags, introExamined.attempts,
                introExamined.inspections).Any(line => line.Contains("网络投递记录")),
                "inspect intro phone must visibly alter the current film story");
            var introCompared = EchoHotspotInvestigation.Inspect(catalog,
                introExamined, "intro_photo");
            Require(EchoHotspotInvestigation.CrossCheck(introCompared,
                    catalog.Hotspots["intro_phone"]).Contains("两种记录互相担保"),
                "two real introductory observations must cross-verify");
            Require(EchoHotspotInvestigation.CrossCheck(EchoNarrative.Fresh(),
                    catalog.Hotspots["intro_phone"]) == "" &&
                EchoHotspotInvestigation.CrossCheck(introExamined,
                    catalog.Hotspots["intro_phone"]) == "",
                "unvisited comparison reference must not be fabricated");
            var warningObserved = EchoNarrative.Advance(catalog, introCompared,
                EchoNarrative.Choices(catalog.Nodes["intro"]).Single());
            var peepholeObserved = EchoNarrative.Advance(catalog, warningObserved,
                EchoNarrative.Choices(catalog.Nodes["warning"])
                    .Single(choice=>choice.to=="c1_peephole"));
            peepholeObserved = EchoHotspotInvestigation.Inspect(catalog,
                peepholeObserved, "peephole_shadow");
            var knockObserved = EchoNarrative.Advance(catalog,peepholeObserved,
                EchoNarrative.Choices(catalog.Nodes["c1_peephole"]).Single());
            var passwordObserved = EchoNarrative.Advance(catalog,knockObserved,
                EchoNarrative.Choices(catalog.Nodes["c1_knock"])
                    .Single(choice=>choice.to=="c1_password"));
            var noForcedProof = passwordObserved.flags.ToArray();
            passwordObserved = EchoHotspotInvestigation.Inspect(catalog,passwordObserved,
                "door_password");
            Require(noForcedProof.SequenceEqual(passwordObserved.flags),
                "examining a door password must not forge new narrative evidence");
            Require(EchoHotspotInvestigation.CrossCheck(passwordObserved,
                    catalog.Hotspots["door_password"]).Contains("两条线索"),
                "observed shadow and password must yield the authored contradiction");
            var hookObserved = EchoNarrative.Advance(catalog,passwordObserved,
                EchoNarrative.Choices(catalog.Nodes["c1_password"]).Single());
            var hookLines = EchoScenePresentation.Lines(catalog.Nodes["c1_hook"],
                hookObserved.flags,hookObserved.attempts,hookObserved.inspections);
            Require(hookLines.Any(line=>line.Contains("追着灯光走的影子")) &&
                hookLines.Any(line=>line.Contains("口令与停顿的差异")),
                "actual first-act inspection choices must affect follow-up dialogue");
            var noObservationLines = EchoScenePresentation.Lines(catalog.Nodes["c1_hook"],
                hookObserved.flags,hookObserved.attempts);
            Require(!noObservationLines.Any(line=>line.Contains("追着灯光走的影子")),
                "story transcript cannot imply an unseen inspection");
            var inspectedJournal = EchoDialogueHistory.Read(catalog,hookObserved);
            Require(inspectedJournal.Entries.Last().Lines.Any(line=>
                line.Contains("口令与停顿的差异")),
                "dialogue history must reproduce the observation-dependent scene");
            var rewoundFirstAct = EchoNarrative.Rewind(catalog,hookObserved,3);
            Require(EchoHotspotInvestigation.WasInspected(rewoundFirstAct,"peephole_shadow") &&
                !EchoHotspotInvestigation.WasInspected(rewoundFirstAct,"door_password") &&
                EchoHotspotInvestigation.CrossCheck(rewoundFirstAct,
                    catalog.Hotspots["door_password"]) == "",
                "rewinding must discard later first-act discoveries and comparisons");
            var tempFirstAct = Path.Combine(Path.GetTempPath(),
                "echo-stage30-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new EchoSaves(catalog,tempFirstAct);
                store.SaveSlot(1,hookObserved);
                var restored = store.LoadSlot(1);
                Require(EchoHotspotInvestigation.CrossCheck(restored,
                    catalog.Hotspots["door_password"]).Length > 0 &&
                    restored.inspections.Length == hookObserved.inspections.Length,
                    "saved observations must fully survive a real save slot roundtrip");
            }
            finally
            {
                if(Directory.Exists(tempFirstAct)) Directory.Delete(tempFirstAct,true);
            }

            var pending = new Queue<(string node, HashSet<string> flags)>();
            pending.Enqueue(("intro", new HashSet<string>()));
            var seen = new HashSet<string>();
            var reached = new HashSet<string>();
            int deadEnds = 0;
            var reachableCases = new HashSet<string>();
            while (pending.Count > 0)
            {
                var (nodeId, flags) = pending.Dequeue();
                var key = nodeId + "|" + string.Join("/", flags.OrderBy(x => x));
                if (!seen.Add(key)) continue;
                Require(seen.Count < 100001, "state explosion");
                reached.Add(nodeId);
                var node = catalog.Nodes[nodeId];
                foreach (var rule in catalog.Cases.rules)
                    if (node.chapterIndex >= rule.chapter && rule.clues.All(flags.Contains))
                        reachableCases.Add(rule.id);
                if (node.ending) continue;
                var options = EchoNarrative.Choices(node).Where(x => EchoNarrative.Allowed(x, flags)).ToArray();
                if (options.Length == 0) { deadEnds++; continue; }
                foreach (var option in options)
                {
                    var after = new HashSet<string>(flags);
                    if (!string.IsNullOrEmpty(option.flag)) after.Add(option.flag);
                    foreach (var flag in catalog.Nodes[option.to].entryFlags ?? Array.Empty<string>()) after.Add(flag);
                    pending.Enqueue((option.to, after));
                }
            }
            Require(seen.Count == 69497 && reached.Count == 55 && deadEnds == 0, "reachability parity");
            Require(reachableCases.SetEquals(catalog.Cases.rules.Select(r => r.id)),
                "not all five deduction puzzles have a reachable, acquired evidence pair");
            using var routeJson = JsonDocument.Parse(File.ReadAllText(Path.Combine(data, "verified-ending-paths.json")));
            var paths = routeJson.RootElement.GetProperty("paths").EnumerateArray().ToArray();
            Require(paths.Length == 4, "missing ending routes");
            int choices = 0;
            var endings = new HashSet<string>();
            foreach (var route in paths)
            {
                var ending = route.GetProperty("ending").GetString();
                var nodes = route.GetProperty("nodes").EnumerateArray().Select(x => x.GetString()).ToArray();
                Require(nodes.Length > 2 && nodes[0] == "intro", "invalid route");
                var tmp = Path.Combine(Path.GetTempPath(), "echo-core-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var store = new EchoSaves(catalog, tmp);
                    var state = EchoNarrative.Fresh();
                    for (int i = 1; i < nodes.Length; i++)
                    {
                        if (ending == "ending_audit" && nodes[i] == "ending_audit")
                        {
                            var auditChoice = EchoNarrative.Choices(catalog.Nodes[state.node])
                                .Single(x => x.to == "ending_audit");
                            Require(!EchoNarrative.AllowedWithDeductions(catalog, auditChoice, state),
                                "audit was unlocked without the player's independent-will deduction");
                            state = EchoNarrative.RecordDeduction(catalog, state,
                                "independent_will", catalog.Rules["independent_will"].correct);
                            Require(EchoNarrative.AllowedWithDeductions(catalog, auditChoice, state),
                                "valid deduction failed to unlock audit");
                        }
                        var options = EchoNarrative.Choices(catalog.Nodes[state.node])
                            .Where(x => x.to == nodes[i] && EchoNarrative.AllowedWithDeductions(catalog, x, state)).ToArray();
                        Require(options.Length == 1, "ambiguous choice on " + state.node);
                        state = EchoNarrative.Advance(catalog, state, options[0]);
                        choices++;
                        if (i == nodes.Length / 2)
                        {
                            store.SaveSlot(2, state);
                            var loaded = store.LoadSlot(2);
                            Require(loaded.node == state.node && loaded.history.Length == state.history.Length, "save roundtrip");
                            state = loaded;
                        }
                    }
                    // Actual Unity narrative classes also drive read-only dialogue history.
                    var newest = EchoDialogueHistory.Read(catalog, state, 0, 9);
                    Require(newest.TotalEntries == state.history.Length + 1, "transcript length mismatch");
                    Require(newest.Entries[^1].NodeId == state.node && newest.Entries[^1].IsCurrent,
                        "transcript must end at the current node");
                    var oldest = EchoDialogueHistory.Read(catalog, state, newest.MaxPage, 9);
                    Require(oldest.Entries[0].NodeId == "intro", "transcript lost first scene");
                    Require(oldest.Entries[0].Choice == state.history[0].choice, "transcript choice mismatch");
                    Require(EchoDialogueHistory.Read(catalog, state, 999, 9).Page == newest.MaxPage,
                        "transcript out-of-range page must clamp safely");
                    var rewinded = EchoNarrative.Rewind(catalog, state, 0);
                    var rewoundLog = EchoDialogueHistory.Read(catalog, rewinded);
                    Require(rewoundLog.TotalEntries == 1 && rewoundLog.Entries[0].NodeId == "intro",
                        "rewind leaks an abandoned branch into dialogue history");
                    // Investigation uses the exact same validated timeline/flags as Unity.
                    var archive = EchoInvestigation.Archive(catalog, state);
                    Require(new HashSet<string>(archive.Select(x => x.Id)).SetEquals(
                        state.flags.Where(catalog.Evidence.ContainsKey)),
                        "archive must show exactly the evidence acquired on this route");
                    Require(archive.All(e => e.AcquiredAtStep >= 0 &&
                        e.AcquiredAtStep <= state.history.Length &&
                        catalog.Nodes.Values.Any(n => n.title == e.SceneTitle)),
                        "evidence provenance must come from a visited scene");
                    var reachedCases = EchoInvestigation.VisibleCases(catalog, state);
                    int reachedChapter = catalog.Nodes[state.node].chapterIndex;
                    Require(reachedCases.All(c => c.Chapter <= reachedChapter),
                        "future case titles were revealed before their chapters");
                    foreach (var caseCard in reachedCases)
                    {
                        var rule = catalog.Rules[caseCard.Id];
                        bool ready = rule.clues.All(state.flags.Contains);
                        Require(caseCard.Ready == ready &&
                            caseCard.AcquiredCount == rule.clues.Count(state.flags.Contains),
                            "caseboard availability does not match actual evidence");
                        if (!ready) continue;
                        if (EchoNarrative.IsSolved(catalog, state, rule.id))
                        {
                            Require(!EchoInvestigation.PairMatches(catalog,state,rule.id,rule.clues),
                                "a solved case must not allow duplicate proof submissions");
                            continue;
                        }
                        Require(EchoInvestigation.PairMatches(catalog, state, rule.id, rule.clues),
                            "real available two-clue pair must unlock its case");
                        Require(!EchoInvestigation.PairMatches(catalog, state, rule.id,
                            new[] { rule.clues[0], rule.clues[0] }),
                            "duplicate pinned clue must not unlock a deduction");
                        var unrelated = archive.FirstOrDefault(a => !rule.clues.Contains(a.Id));
                        if (unrelated != null)
                            Require(!EchoInvestigation.PairMatches(catalog, state, rule.id,
                                new[] { rule.clues[0], unrelated.Id }),
                                "wrong but acquired clues must not unlock a deduction");
                        // A correct answer still goes through the existing actual
                        // deduction/save logic, never a parallel game state.
                        var deductionState = EchoNarrative.RecordDeduction(
                            catalog, state, rule.id, rule.correct);
                        Require(EchoNarrative.IsSolved(catalog, deductionState, rule.id) &&
                            !EchoInvestigation.PairMatches(catalog, deductionState, rule.id, rule.clues),
                            "already-solved case must not be answered again");
                    }
                    var discarded = EchoNarrative.Rewind(catalog, state, 0);
                    Require(EchoInvestigation.Archive(catalog, discarded).Length == 0,
                        "rewind leaked evidence from the discarded timeline");
                    Require(!EchoInvestigation.PairMatches(catalog, discarded,
                        "door_identity", new[] { "shadow", "tested_password" }),
                        "rewound evidence must not be usable for deduction");
                    if (ending == "ending_audit")
                    {
                        Require(EchoNarrative.IsSolved(catalog,state,"independent_will"),
                            "player must complete the independent-will deduction for audited ending");
                        var forgedEnding = new GameProgress {
                            node = state.node, flags = state.flags,
                            history = state.history, endings = state.endings,
                            attempts = Array.Empty<Attempt>(), inspections = state.inspections
                        };
                        bool rejected = false;
                        try { EchoNarrative.Normalize(catalog,forgedEnding); }
                        catch (InvalidOperationException) { rejected = true; }
                        Require(rejected,"format-7 audit forgery was not rejected");

                        var oldEnvelope = new SaveEnvelope {
                            game = "0317-echo", format = 6,
                            storyRevision = catalog.Story.revision, state = forgedEnding
                        };
                        var migrated = store.Parse(JsonUtility.ToJson(oldEnvelope));
                        Require(migrated.node == "ending_audit" &&
                                EchoNarrative.IsSolved(catalog,migrated,"independent_will"),
                            "legacy audit save migration did not preserve its earned ending");
                        oldEnvelope.format = 7;
                        bool newRejected = false;
                        try { store.Parse(JsonUtility.ToJson(oldEnvelope)); }
                        catch (InvalidOperationException) { newRejected = true; }
                        Require(newRejected, "new saves must not receive grandfathered proof");

                        var auditHistory = EchoDialogueHistory.Read(catalog,state,0,30);
                        var visitsToGate = auditHistory.Entries.Where(x=>x.NodeId=="c5_gate").ToArray();
                        Require(visitsToGate.Length == 2 &&
                            !visitsToGate[0].Lines.Any(x=>x.Contains("现在可以提交独立审计")) &&
                            visitsToGate[1].Lines.Any(x=>x.Contains("现在可以提交独立审计")),
                            "historical transcript leaked a later deduction into an earlier scene");
                    }
                    Require(state.node == ending && state.endings.Contains(ending), "route ending mismatch");
                    endings.Add(ending);
                    store.AutoSave(state);
                    Require(store.LoadAuto().node == ending, "auto save roundtrip");
                }
                finally { if (Directory.Exists(tmp)) Directory.Delete(tmp, true); }
            }
            Require(endings.SetEquals(new[] {"ending_merge", "ending_loop", "ending_stop", "ending_audit"}), "four endings missing");
            var forged = EchoNarrative.Fresh();
            forged.flags = new[] { "incident_proof", "cache46", "source_verified" };
            Require(EchoNarrative.Normalize(catalog, forged).flags.Length == 0, "forged evidence accepted");
            // Playback request lifecycles are the same C# class used by the
            // Unity VideoDirector. Stale completion, pending-skip and modal
            // pause behavior are all validated without Unity activation.
            var playback = new EchoPlaybackState();
            Require(!playback.Skip(), "skip must not succeed before a clip is selected");
            Require(playback.Request("V01"), "first clip request");
            int firstToken = playback.Generation;
            Require(playback.CanPresent(firstToken), "first request not presentable");
            Require(!playback.Request("V01"), "repeat request must not restart a continuous plate");
            Require(playback.Request("V02"), "second clip request");
            int pendingToken = playback.Generation;
            Require(!playback.CanPresent(firstToken), "stale request was not invalidated");
            Require(playback.Skip(), "skip must work during pending preparation");
            Require(playback.IsFinished && playback.ClipId == "V02", "skipped clip identity lost");
            Require(!playback.CanPresent(pendingToken), "skipped pending frame may become visible");
            Require(!playback.Skip(), "duplicate skip must not emit another completion");
            Require(!playback.Request("V02"), "skipped same clip must not silently autoplay");
            Require(playback.Request("V02", true), "explicit replay must restart a skipped clip");
            int replayToken = playback.Generation;
            Require(playback.CanPresent(replayToken), "replay must allow new frames");
            playback.Pause();
            Require(playback.Request("V03") && playback.IsPaused,
                "shot switch inside a modal must preserve pause");
            int thirdToken = playback.Generation;
            Require(!playback.Complete(replayToken), "old replay must not finish the latest shot");
            Require(playback.Complete(thirdToken), "current shot should finish once");
            Require(!playback.Complete(thirdToken) && !playback.Skip(),
                "finished shot must never complete twice or skip again");
            playback.Resume();
            Require(!playback.IsPaused, "resume did not unpause");
            playback.Halt();
            Require(playback.ClipId == "" && !playback.IsFinished && !playback.IsPaused,
                "leaving a scene must discard all video state");
            Require(!playback.CanPresent(thirdToken), "halted scene accepted a stale frame");
            // The same modal flow class is compiled into the Unity runtime:
            // pause -> settings -> casebook -> B -> pause -> B -> game.
            var dialogs = new EchoModalFlow();
            Require(!dialogs.IsOpen && dialogs.Back() == "", "new UI should have no modal");
            dialogs.Open("pause");
            dialogs.Open("settings");
            dialogs.Open("cases");
            Require(dialogs.Current == "cases", "modal page switching failed");
            Require(dialogs.Back() == "pause", "cancel must return to pause, not resume behind it");
            Require(dialogs.Back() == "" && !dialogs.IsOpen, "cancel from pause must close");
            dialogs.Open("saves");
            dialogs.Confirm("saves");
            Require(dialogs.Back() == "saves", "cancel save confirmation must return to saves");
            Require(dialogs.Back() == "", "direct saves page should close to underlying game");
            dialogs.Open("pause");
            dialogs.Open("saves");
            dialogs.Confirm("saves");
            Require(dialogs.Back() == "saves" && dialogs.Back() == "pause" &&
                    dialogs.Back() == "", "nested save confirmation back chain failed");
            dialogs.Confirm("");
            Require(dialogs.Back() == "" && !dialogs.IsOpen, "new-game confirmation cancel must close");
            dialogs.Open("settings");
            dialogs.Open("settings");
            Require(dialogs.Back() == "", "refreshing settings must not accumulate back steps");
            var optionRoot = Path.Combine(Path.GetTempPath(), "echo-pref-" + Guid.NewGuid().ToString("N"));
            try
            {
                var prefs = new EchoPreferences(optionRoot);
                prefs.Update(false, true, false, 1.25f, 1.5f);
                prefs = new EchoPreferences(optionRoot);
                Require(!prefs.Current.captionsEnabled && prefs.Current.muted && !prefs.Current.fullscreen &&
                        Math.Abs(prefs.Current.speed - 1.25f) < .001f &&
                        Math.Abs(prefs.Current.subtitleScale - 1.5f) < .001f, "settings persistence");
                // Old v1 options have no subtitleScale field: read them at 100%.
                var optionsFile = Path.Combine(optionRoot, "player-options.json");
                File.WriteAllText(optionsFile,
                    "{\"version\":1,\"captionsEnabled\":true,\"muted\":false,\"fullscreen\":true,\"speed\":1}");
                prefs = new EchoPreferences(optionRoot);
                Require(Math.Abs(prefs.Current.subtitleScale - 1f) < .001f,
                    "older options must default to readable 100% captions");
                prefs.Update(true, false, true, 1f, float.NaN);
                Require(Math.Abs(prefs.Current.subtitleScale - 1f) < .001f,
                    "invalid subtitle scale must normalize");
                prefs.Update(true, false, true, 1f, 999f);
                Require(Math.Abs(new EchoPreferences(optionRoot).Current.subtitleScale - 1.5f) < .001f,
                    "oversize subtitles must clamp");
            }
            finally { if (Directory.Exists(optionRoot)) Directory.Delete(optionRoot, true); }
            var report = new {
                schema = "echo-corehost-v1", result = "pass", runtime = ".NET 8 shim - NOT Unity",
                nodes = reached.Count, states = seen.Count, deadEnds,
                endings = endings.Count, choices, saveRoundtrip = true, tamperingRejected = true,
                settingsRoundtrip = true, timestampUtc = DateTime.UtcNow.ToString("O")
            };
            Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }
}

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
            var pending = new Queue<(string node, HashSet<string> flags)>();
            pending.Enqueue(("intro", new HashSet<string>()));
            var seen = new HashSet<string>();
            var reached = new HashSet<string>();
            int deadEnds = 0;
            while (pending.Count > 0)
            {
                var (nodeId, flags) = pending.Dequeue();
                var key = nodeId + "|" + string.Join("/", flags.OrderBy(x => x));
                if (!seen.Add(key)) continue;
                Require(seen.Count < 100001, "state explosion");
                reached.Add(nodeId);
                var node = catalog.Nodes[nodeId];
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
                        var options = EchoNarrative.Choices(catalog.Nodes[state.node])
                            .Where(x => x.to == nodes[i] && EchoNarrative.Allowed(x, state.flags)).ToArray();
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

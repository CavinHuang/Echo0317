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
            var optionRoot = Path.Combine(Path.GetTempPath(), "echo-pref-" + Guid.NewGuid().ToString("N"));
            try
            {
                var prefs = new EchoPreferences(optionRoot);
                prefs.Update(false, true, false, 1.25f);
                prefs = new EchoPreferences(optionRoot);
                Require(!prefs.Current.captionsEnabled && prefs.Current.muted && !prefs.Current.fullscreen &&
                        Math.Abs(prefs.Current.speed - 1.25f) < .001f, "settings persistence");
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Runtime
{
    [Serializable] public sealed class QaJourneyList { public QaJourney[] paths; }
    [Serializable] public sealed class QaJourney { public string ending; public string[] nodes; }
    [Serializable] public sealed class NativeQaReport
    {
        public string schema = "echo0317-native-qa-v2";
        public string result = "fail";
        public string unityVersion = "";
        public string storyRevision = "";
        public string utc = "";
        public string error = "";
        public int nodes;
        public int rules;
        public int graphStatesVerified;
        public int graphNodesReachable;
        public int deadEndStates;
        public int endingsVerified;
        public int choicesVerified;
        public bool saveRoundtripPassed;
        public bool tamperingRejected;
    }

    /// <summary>
    /// Run as a compiled, headless Unity Windows player:
    ///   Echo0317.exe -batchmode -nographics -echoQa -echoQaReport C:\\path\\native-game-qa.json
    /// This is deliberately *not* an editor-only script. It tests the very same
    /// serialized data and C# runtime present in the distributed player.
    /// It does not test film rendering or GUI input; these need interactive QA.
    /// </summary>
    public sealed class EchoNativeQa : MonoBehaviour
    {
        public static bool Requested => Environment.GetCommandLineArgs().Any(a => a == "-echoQa");

        static string ReportPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-echoQaReport")
                    return Path.GetFullPath(args[i + 1]);
            return Path.Combine(Application.persistentDataPath, "native-game-qa.json");
        }

        IEnumerator Start()
        {
            // Leave one frame for Unity's bootstrap to finish before running the
            // compiled-player test. The normal game's UI/video bootstrap is skipped.
            yield return null;
            var report = new NativeQaReport {
                unityVersion = Application.unityVersion,
                utc = DateTime.UtcNow.ToString("o")
            };
            int exitCode = 1;
            try
            {
                Verify(report);
                report.result = "pass";
                exitCode = 0;
            }
            catch (Exception exception)
            {
                report.result = "fail";
                report.error = exception.ToString();
                Debug.LogError("ECHO_NATIVE_QA_FAIL: " + exception);
            }
            try
            {
                var path = ReportPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonUtility.ToJson(report, true));
                Debug.Log(exitCode == 0 ? "ECHO_NATIVE_QA_PASS" : "ECHO_NATIVE_QA_FAIL");
            }
            catch (Exception writeError)
            {
                exitCode = 1;
                Debug.LogError("ECHO_NATIVE_QA_REPORT_WRITE_FAILED: " + writeError);
            }
            yield return null; // allow the logger to flush
            Application.Quit(exitCode);
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidDataException("Native QA: " + message);
        }

        /// <summary>Walk the real C# transition rules, not merely the exported JSON graph.</summary>
        static void AuditReachability(GameCatalog catalog, NativeQaReport report)
        {
            var pending = new Queue<(string node, HashSet<string> flags)>();
            pending.Enqueue(("intro", new HashSet<string>()));
            var seenStates = new HashSet<string>();
            var seenNodes = new HashSet<string>();
            while (pending.Count > 0)
            {
                var item = pending.Dequeue();
                string identity = item.node + "|" + string.Join("/", item.flags.OrderBy(id => id));
                if (!seenStates.Add(identity)) continue;
                if (seenStates.Count > 100000) throw new InvalidDataException("剧情状态增长超出安全上限");
                seenNodes.Add(item.node);
                var node = catalog.Nodes[item.node];
                if (node.ending) continue;
                var choices = EchoNarrative.Choices(node).Where(c => EchoNarrative.Allowed(c, item.flags)).ToArray();
                if (choices.Length == 0) { report.deadEndStates++; continue; }
                foreach (var choice in choices)
                {
                    var flags = new HashSet<string>(item.flags);
                    if (!string.IsNullOrEmpty(choice.flag)) flags.Add(choice.flag);
                    foreach (var flag in catalog.Nodes[choice.to].entryFlags ?? Array.Empty<string>()) flags.Add(flag);
                    pending.Enqueue((choice.to, flags));
                }
            }
            report.graphStatesVerified = seenStates.Count;
            report.graphNodesReachable = seenNodes.Count;
        }

        public static void Verify(NativeQaReport report)
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            report.storyRevision = catalog.Story.revision;
            report.nodes = catalog.Nodes.Count;
            report.rules = catalog.Rules.Count;
            Require(report.nodes == 55, "剧情节点数应为 55");
            Require(report.rules == 5, "推理题数应为 5");
            AuditReachability(catalog, report);
            Require(report.graphStatesVerified == 69497, "状态图发生了未审核的改变");
            Require(report.graphNodesReachable == 55, "存在无法到达的剧情节点");
            Require(report.deadEndStates == 0, "玩家可能在非结局节点卡住");
            var journeysFile = Path.Combine(Application.streamingAssetsPath, "Data", "verified-ending-paths.json");
            var journeys = JsonUtility.FromJson<QaJourneyList>(File.ReadAllText(journeysFile));
            Require(journeys != null && journeys.paths != null && journeys.paths.Length == 4, "四条结局路径缺失");

            var endings = new HashSet<string>();
            foreach (var route in journeys.paths)
            {
                Require(route != null && route.nodes != null && route.nodes.Length > 1, "空结局路线");
                Require(route.nodes[0] == "intro", "结局路线的入口不是 intro");
                var progress = EchoNarrative.Fresh();
                var mid = route.nodes.Length / 2;
                var temp = Path.Combine(Application.temporaryCachePath, "echo-native-qa-" + Guid.NewGuid().ToString("N"));
                try
                {
                    var storage = new EchoSaves(catalog, temp);
                    for (int i = 1; i < route.nodes.Length; i++)
                    {
                        var current = catalog.Nodes[progress.node];
                        // Simulate the player actually opening the deduction board
                        // and proving both independently signed wills. The normal
                        // runtime never auto-solves this: its UI requires two cards.
                        if (route.ending == "ending_audit" && route.nodes[i] == "ending_audit")
                        {
                            var auditChoice = EchoNarrative.Choices(current)
                                .Single(c => c.to == "ending_audit");
                            Require(!EchoNarrative.AllowedWithDeductions(catalog, auditChoice, progress),
                                "审计结局在未完成推理前就已经解锁");
                            progress = EchoNarrative.RecordDeduction(catalog, progress,
                                "independent_will", catalog.Rules["independent_will"].correct);
                            Require(EchoNarrative.AllowedWithDeductions(catalog, auditChoice, progress),
                                "核实独立意愿后，审计结局仍被错误锁定");
                        }
                        var choices = EchoNarrative.Choices(current)
                            .Where(c => c.to == route.nodes[i] &&
                                EchoNarrative.AllowedWithDeductions(catalog, c, progress)).ToArray();
                        Require(choices.Length == 1, "结局 " + route.ending + " 的分支有歧义：" + current.id + " -> " + route.nodes[i]);
                        progress = EchoNarrative.Advance(catalog, progress, choices[0]);
                        Require(progress.node == route.nodes[i], "推进目的地与预期不一致");
                        report.choicesVerified++;
                        if (i == mid)
                        {
                            storage.SaveSlot(1, progress);
                            var restored = storage.LoadSlot(1);
                            Require(restored.node == progress.node &&
                                    restored.history.Length == progress.history.Length &&
                                    new HashSet<string>(restored.flags).SetEquals(progress.flags),
                                "读档后剧情位置或证据不同");
                            progress = restored;
                            report.saveRoundtripPassed = true;
                        }
                    }
                    Require(progress.node == route.ending && catalog.Nodes[route.ending].ending,
                        "没有到达目标结局：" + route.ending);
                    Require(progress.endings.Contains(route.ending), "结局图鉴没有登记：" + route.ending);
                    endings.Add(progress.node);
                    report.endingsVerified++;
                }
                finally
                {
                    if (Directory.Exists(temp)) Directory.Delete(temp, true);
                }
            }
            Require(endings.SetEquals(new[] { "ending_merge", "ending_loop", "ending_stop", "ending_audit" }),
                "四结局集合不完整");

            // Check one particularly damaging class of save cheats: a forged
            // evidence flag must be ignored when replaying the actual history.
            var forged = EchoNarrative.Fresh();
            forged.flags = new[] { "incident_proof", "lin_willing", "source_verified" };
            var normalized = EchoNarrative.Normalize(catalog, forged);
            Require(normalized.flags.Length == 0, "可通过存档伪造证据");
            report.tamperingRejected = true;
            Require(report.saveRoundtripPassed && report.endingsVerified == 4, "游戏原生诊断未完全执行");
        }
    }
}

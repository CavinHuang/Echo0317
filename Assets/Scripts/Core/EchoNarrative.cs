using System;
using System.Collections.Generic;
using System.Linq;

namespace Echo0317.Core
{
    [Serializable] public class Step { public string from, to, choice; public int chapter; }
    [Serializable] public class Attempt { public string rule, answer; public int at; }
    [Serializable] public class GameProgress
    {
        public string node = "intro";
        public string[] flags = Array.Empty<string>();
        public Step[] history = Array.Empty<Step>();
        public string[] endings = Array.Empty<string>();
        public bool debug;
        public Attempt[] attempts = Array.Empty<Attempt>();
        // Optional additive save field; previous format-6 saves load with an empty array.
        public HotspotVisit[] inspections = Array.Empty<HotspotVisit>();
    }
    [Serializable] public class SaveEnvelope
    {
        public string game = "0317-echo";
        public int format = 6;
        public string storyRevision = "v3.0";
        public string exportedAt = "";
        public GameProgress state;
    }
    public sealed class Checkpoint
    {
        public int Index;
        public string Node;
        public string[] Flags;
        public int Chapter;
        public string Choice;
    }
    public sealed class StoryTrace
    {
        public string Node;
        public string[] Flags;
        public List<Checkpoint> Points;
    }
    public sealed class ChapterSummary
    {
        public int chapter;
        public string name;
        public bool finished;
        public string[] discovered, risks, solved;
    }
    public static class EchoNarrative
    {
        public const int MaxSteps = 4000;
        public const int MaxAttempts = 60;
        static readonly string[] Risks = { "operator_alert", "misread_identity", "coerced", "justified_coercion", "belief_future", "pressed_lock" };
        public static bool Allowed(StoryChoice c, ICollection<string> flags)
        {
            return (c.requires == null || c.requires.All(flags.Contains)) &&
                (c.needsAny == null || c.needsAny.Length == 0 || c.needsAny.Any(flags.Contains)) &&
                (c.forbids == null || c.forbids.All(f => !flags.Contains(f)));
        }
        public static StoryChoice[] Choices(StoryNode n)
        {
            if (n.choices != null && n.choices.Length > 0) return n.choices;
            return string.IsNullOrEmpty(n.@next) ? Array.Empty<StoryChoice>() :
                new[] { new StoryChoice { to = n.@next, label = n.nextLabel ?? "继续剧情" } };
        }
        public static StoryTrace Trace(GameCatalog g, Step[] steps)
        {
            if (steps == null || steps.Length > MaxSteps) throw new InvalidOperationException("剧情历史长度异常");
            string node = "intro";
            var flags = new HashSet<string>();
            var points = new List<Checkpoint> { new Checkpoint { Index = 0, Node = node, Flags = Array.Empty<string>(), Chapter = g.Nodes[node].chapterIndex, Choice = "" } };
            for (int i = 0; i < steps.Length; i++)
            {
                var step = steps[i];
                if (step == null || step.from != node || string.IsNullOrEmpty(step.to) || !g.Nodes.ContainsKey(step.to))
                    throw new InvalidOperationException("剧情路径断裂，第 " + i + " 步");
                if (g.Nodes[node].ending)
                    throw new InvalidOperationException("不能从已结束的结局继续推进，第 " + i + " 步");
                StoryChoice selected = null;
                foreach (var c in Choices(g.Nodes[node])) if (c.to == step.to && c.label == step.choice) { selected = c; break; }
                // Legacy saves may omit a choice label. A *present but incorrect* label
                // must never silently select an option with the same destination.
                if (selected == null && string.IsNullOrEmpty(step.choice))
                {
                    var candidates = Choices(g.Nodes[node]).Where(c => c.to == step.to).ToArray();
                    if (candidates.Length == 1) selected = candidates[0];
                }
                if (selected == null || !Allowed(selected, flags)) throw new InvalidOperationException("分支条件不满足，第 " + i + " 步");
                if (!string.IsNullOrEmpty(selected.flag)) flags.Add(selected.flag);
                node = selected.to;
                foreach (var f in g.Nodes[node].entryFlags ?? Array.Empty<string>()) flags.Add(f);
                points.Add(new Checkpoint { Index = i + 1, Node = node, Flags = flags.ToArray(), Chapter = g.Nodes[node].chapterIndex, Choice = step.choice ?? "" });
            }
            return new StoryTrace { Node = node, Flags = flags.ToArray(), Points = points };
        }
        public static GameProgress Fresh() => new GameProgress();
        public static GameProgress Normalize(GameCatalog g, GameProgress raw)
        {
            if (raw == null) throw new InvalidOperationException("缺少存档对象");
            var history = raw.history ?? Array.Empty<Step>();
            var result = Trace(g, history);
            if (raw.node != result.Node) throw new InvalidOperationException("存档节点与历史不一致");
            var endings = (raw.endings ?? Array.Empty<string>()).Distinct().ToArray();
            if (endings.Any(id => !g.Nodes.ContainsKey(id) || !g.Nodes[id].ending)) throw new InvalidOperationException("结局记录损坏");
            var canonical = history.Select(h => {
                var validChoices = Choices(g.Nodes[h.from]).Where(c => c.to == h.to).ToArray();
                var actualChoice = validChoices.FirstOrDefault(c => c.label == h.choice) ??
                                   (validChoices.Length == 1 ? validChoices[0] : null);
                return new Step { from = h.from, to = h.to,
                    choice = actualChoice?.label ?? "", chapter = g.Nodes[h.from].chapterIndex };
            }).ToArray();
            var attempts = raw.attempts ?? Array.Empty<Attempt>();
            if (attempts.Length > MaxAttempts) throw new InvalidOperationException("推理次数超限");
            int prior = -1;
            var solved = new HashSet<string>();
            foreach (var a in attempts)
            {
                if (a == null || a.at < prior || a.at < 0 || a.at > history.Length || !g.Rules.TryGetValue(a.rule ?? "", out var r) ||
                    r.options.All(o => o.id != a.answer)) throw new InvalidOperationException("无效推理历史");
                var historicFlags = new HashSet<string>(Trace(g, canonical.Take(a.at).ToArray()).Flags);
                if (!r.clues.All(historicFlags.Contains) || solved.Contains(a.rule)) throw new InvalidOperationException("推理时尚未取得证据或已完成");
                if (r.correct == a.answer) solved.Add(a.rule);
                prior = a.at;
            }
            var inspections = EchoHotspotInvestigation.Validate(g, canonical, raw.inspections);
            return new GameProgress { node = result.Node, flags = result.Flags, history = canonical, endings = endings,
                attempts = attempts.Select(a => new Attempt { rule = a.rule, answer = a.answer, at = a.at }).ToArray(),
                inspections = inspections, debug = raw.debug };
        }
        public static GameProgress Advance(GameCatalog g, GameProgress progress, StoryChoice choice)
        {
            var p = Normalize(g, progress);
            if (choice == null || !g.Nodes.ContainsKey(choice.to) || !Allowed(choice, new HashSet<string>(p.flags)))
                throw new InvalidOperationException("不可用的剧情选择");
            if (!Choices(g.Nodes[p.node]).Any(c => c.to == choice.to && c.label == choice.label))
                throw new InvalidOperationException("不能跨过剧情节点");
            var steps = p.history.ToList();
            steps.Add(new Step { from = p.node, to = choice.to, choice = choice.label ?? "", chapter = g.Nodes[p.node].chapterIndex });
            p.history = steps.ToArray();
            p.node = choice.to;
            if (g.Nodes[p.node].ending) p.endings = p.endings.Concat(new[] { p.node }).Distinct().ToArray();
            return Normalize(g, p);
        }
        public static GameProgress Rewind(GameCatalog g, GameProgress p, int stepIndex)
        {
            p = Normalize(g, p);
            if (stepIndex < 0 || stepIndex > p.history.Length) throw new InvalidOperationException("回溯点无效");
            p.history = p.history.Take(stepIndex).ToArray();
            p.attempts = p.attempts.Where(a => a.at <= stepIndex).ToArray();
            p.inspections = p.inspections.Where(v => v.at <= stepIndex).ToArray();
            p.node = Trace(g, p.history).Node;
            return Normalize(g, p);
        }
        public static bool IsSolved(GameCatalog g, GameProgress p, string rule)
        {
            if (!g.Rules.TryGetValue(rule, out var r)) return false;
            return (p.attempts ?? Array.Empty<Attempt>()).Any(a => a.rule == rule && a.answer == r.correct);
        }
        public static GameProgress RecordDeduction(GameCatalog g, GameProgress p, string ruleId, string answer)
        {
            p = Normalize(g, p);
            if (!g.Rules.TryGetValue(ruleId, out var r) || r.options.All(x => x.id != answer)) throw new InvalidOperationException("未知推理答案");
            if (!r.clues.All(p.flags.Contains)) throw new InvalidOperationException("证据尚未集齐");
            if (IsSolved(g, p, ruleId)) throw new InvalidOperationException("已经解开该推理");
            if (p.attempts.Length >= MaxAttempts) throw new InvalidOperationException("推理次数超限");
            p.attempts = p.attempts.Concat(new[] { new Attempt { rule = ruleId, answer = answer, at = p.history.Length } }).ToArray();
            return Normalize(g, p);
        }
        public static ChapterSummary[] Reports(GameCatalog g, GameProgress p)
        {
            p = Normalize(g, p);
            var points = Trace(g, p.history).Points;
            var reports = new List<ChapterSummary>();
            for (int chapter = 1; chapter <= 5; chapter++)
            {
                var segment = points.Where(x => x.Chapter == chapter).ToArray();
                if (segment.Length == 0) continue;
                var first = segment[0]; var last = segment[segment.Length - 1];
                var previous = first.Index > 0 ? points[first.Index - 1].Flags : Array.Empty<string>();
                var newlyFound = last.Flags.Except(previous).ToArray();
                reports.Add(new ChapterSummary {
                    chapter = chapter, name = g.ChapterName(chapter),
                    finished = points.Any(x => x.Chapter > chapter) || (chapter == 5 && g.Nodes[p.node].ending),
                    discovered = newlyFound.Where(g.Evidence.ContainsKey).ToArray(),
                    risks = newlyFound.Intersect(Risks).ToArray(),
                    solved = g.Cases.rules.Where(r => r.chapter == chapter && p.attempts.Any(a => a.rule == r.id && a.answer == r.correct && a.at <= last.Index)).Select(r => r.id).ToArray()
                });
            }
            return reports.ToArray();
        }
    }
}

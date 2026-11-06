using System;
using System.Collections.Generic;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>
    /// Build the lines actually heard/read for one visited scene, using the
    /// canonical evidence and CASE conclusions available at that point.
    /// Story JSON already authored 9 scenes of conditional reactions; older
    /// UIs silently skipped them. This is shared by the player and transcript.
    /// </summary>
    public static class EchoScenePresentation
    {
        public static string[] Lines(StoryNode scene, IEnumerable<string> flags,
            IEnumerable<Attempt> attempts, IEnumerable<HotspotVisit> observations = null)
        {
            if (scene == null) throw new ArgumentNullException(nameof(scene));
            var acquired = new HashSet<string>(flags ?? Array.Empty<string>());
            var inspected = new HashSet<string>((observations ?? Array.Empty<HotspotVisit>())
                .Where(v => v != null).Select(v => v.id), StringComparer.Ordinal);
            var entries = (scene.lines ?? Array.Empty<string>()).ToList();
            foreach (var reaction in scene.reactions ?? Array.Empty<StoryReaction>())
            {
                if (reaction == null || string.IsNullOrWhiteSpace(reaction.text)) continue;
                bool required = string.IsNullOrWhiteSpace(reaction.@if) ||
                    acquired.Contains(reaction.@if);
                bool forbidden = !string.IsNullOrWhiteSpace(reaction.unless) &&
                    acquired.Contains(reaction.unless);
                bool inspectedRequired = string.IsNullOrWhiteSpace(reaction.ifInspected) ||
                    inspected.Contains(reaction.ifInspected);
                bool inspectedForbidden = !string.IsNullOrWhiteSpace(reaction.unlessInspected) &&
                    inspected.Contains(reaction.unlessInspected);
                if (required && !forbidden && inspectedRequired && !inspectedForbidden)
                    entries.Add(reaction.text);
            }

            if (scene.id == "c5_gate" && acquired.Contains("joint_signed"))
            {
                bool proofComplete = (attempts ?? Array.Empty<Attempt>()).Any(a =>
                    a != null && a.rule == "independent_will" &&
                    a.answer == "independent");
                entries.Add(proofComplete
                    ? "独立意愿案件已在推理板完成互证：双方签名与同意不能互相替代。现在可以提交独立审计。"
                    : "双方签名已经就绪，但仅有签名不等于证明知情与自愿。先在「证据推理」核实独立意愿，才能提交审计。");
            }
            return entries.ToArray();
        }
    }
}

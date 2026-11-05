using System;
using System.Collections.Generic;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>An acquired record, sourced from the player's actual, validated timeline.</summary>
    public sealed class EchoEvidenceRecord
    {
        public string Id;
        public string Label;
        public int AcquiredAtStep;
        public int Chapter;
        public string SceneTitle;
        public string Clock;
    }

    /// <summary>The only deduction cards a player has reached in this loop.</summary>
    public sealed class EchoCaseCard
    {
        public string Id;
        public string Title;
        public int Chapter;
        public int AcquiredCount;
        public int RequiredCount;
        public bool Solved;
        public bool Ready;
    }

    /// <summary>
    /// Read-only investigation board built from actual history. Temporary pinned
    /// clues live only in the UI and do not affect save files or story flags.
    /// </summary>
    public static class EchoInvestigation
    {
        /// <summary>
        /// Show exactly the collected flags that have a matching evidence label.
        /// The source scene is the FIRST point at which the flag appeared on
        /// this timeline. After rewind, abandoned evidence disappears naturally.
        /// </summary>
        public static EchoEvidenceRecord[] Archive(GameCatalog catalog, GameProgress progress)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var normalized = EchoNarrative.Normalize(catalog, progress);
            var points = EchoNarrative.Trace(catalog, normalized.history).Points;
            var seen = new HashSet<string>();
            var found = new List<EchoEvidenceRecord>();
            foreach (var point in points)
            {
                var node = catalog.Nodes[point.Node];
                foreach (var id in point.Flags)
                {
                    if (!catalog.Evidence.ContainsKey(id) || !seen.Add(id)) continue;
                    found.Add(new EchoEvidenceRecord {
                        Id = id,
                        Label = catalog.EvidenceName(id),
                        AcquiredAtStep = point.Index,
                        Chapter = node.chapterIndex,
                        SceneTitle = node.title ?? "",
                        Clock = node.clock ?? ""
                    });
                }
            }
            return found.ToArray();
        }

        /// <summary>
        /// Unvisited future cases are not enumerated. Case availability is
        /// based on real flags, not guesses or manually checked UI labels.
        /// </summary>
        public static EchoCaseCard[] VisibleCases(GameCatalog catalog, GameProgress progress)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var state = EchoNarrative.Normalize(catalog, progress);
            var chapters = EchoNarrative.Trace(catalog, state.history).Points.Max(p => p.Chapter);
            var acquired = new HashSet<string>(state.flags);
            return catalog.Cases.rules.Where(r => r.chapter <= chapters)
                .Select(r => new EchoCaseCard {
                    Id = r.id,
                    Title = r.title,
                    Chapter = r.chapter,
                    AcquiredCount = r.clues.Count(acquired.Contains),
                    RequiredCount = r.clues.Length,
                    Solved = EchoNarrative.IsSolved(catalog, state, r.id),
                    Ready = r.clues.All(acquired.Contains)
                }).ToArray();
        }

        /// <summary>
        /// Checks the exact two evidence IDs before the actual answer is
        /// submitted through EchoNarrative.RecordDeduction. No hint in this
        /// method exposes the required pair; the UI only shows a generic
        /// mismatch message until the player discovers the correct connection.
        /// </summary>
        public static bool PairMatches(GameCatalog catalog, GameProgress progress,
            string ruleId, IEnumerable<string> pinned)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var state = EchoNarrative.Normalize(catalog, progress);
            if (!catalog.Rules.TryGetValue(ruleId ?? "", out var rule) ||
                EchoNarrative.IsSolved(catalog, state, ruleId)) return false;
            var maxChapter = EchoNarrative.Trace(catalog, state.history).Points.Max(p => p.Chapter);
            if (rule.chapter > maxChapter || rule.clues == null || rule.clues.Length != 2)
                return false;
            var ids = pinned == null ? Array.Empty<string>() : pinned.ToArray();
            if (ids.Length != 2 || ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != 2)
                return false;
            var acquired = new HashSet<string>(state.flags);
            return ids.All(acquired.Contains) &&
                new HashSet<string>(ids).SetEquals(rule.clues);
        }
    }
}

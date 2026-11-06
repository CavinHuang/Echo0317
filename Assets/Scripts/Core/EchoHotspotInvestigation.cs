using System;
using System.Collections.Generic;
using System.Linq;

namespace Echo0317.Core
{
    [Serializable] public sealed class HotspotFile
    {
        public HotspotDefinition[] hotspots = Array.Empty<HotspotDefinition>();
    }

    [Serializable] public sealed class HotspotDefinition
    {
        public string id, node, label, description, evidenceId, icon;
        // Optional chapter-local clue comparison; never mints a story flag.
        public string crossCheckId, crossCheckText;
        public float x, y, w, h;
        public bool required;
        public string[] requiresFlags;
    }

    /// <summary>Recorded investigation on a particular, validated story step.</summary>
    [Serializable] public sealed class HotspotVisit
    {
        public string id, node;
        public int at;
    }

    /// <summary>
    /// Narrative-safe, genuinely persisted hotspot investigations. A hotspot
    /// never fabricates story evidence: only the actual narrative Choice/entry
    /// flags can grant casebook evidence and unlock deductions.
    /// </summary>
    public static class EchoHotspotInvestigation
    {
        public const int MaxVisits = 1024;

        public static HotspotDefinition[] Visible(GameCatalog catalog, GameProgress progress)
        {
            var state = EchoNarrative.Normalize(catalog, progress);
            return catalog.Hotspots.Values.Where(h => h.node == state.node &&
                (h.requiresFlags == null || h.requiresFlags.All(state.flags.Contains)))
                .OrderBy(h => h.id, StringComparer.Ordinal).ToArray();
        }

        public static bool IsInspected(GameProgress progress, HotspotDefinition definition)
        {
            if (progress == null || definition == null) return false;
            int step = progress.history == null ? 0 : progress.history.Length;
            return (progress.inspections ?? Array.Empty<HotspotVisit>()).Any(v =>
                v != null && v.id == definition.id && v.at == step && v.node == progress.node);
        }

        public static HotspotVisit[] Validate(GameCatalog catalog, Step[] history, HotspotVisit[] visits)
        {
            var records = visits ?? Array.Empty<HotspotVisit>();
            if (records.Length > MaxVisits) throw new InvalidOperationException("调查记录超过安全上限");
            var points = EchoNarrative.Trace(catalog, history ?? Array.Empty<Step>()).Points;
            var unique = new HashSet<string>();
            int last = -1;
            foreach (var record in records)
            {
                if (record == null || record.at < last || record.at < 0 || record.at >= points.Count ||
                    !catalog.Hotspots.TryGetValue(record.id ?? "", out var hotspot) ||
                    record.node != hotspot.node || points[record.at].Node != hotspot.node ||
                    (hotspot.requiresFlags != null &&
                     !hotspot.requiresFlags.All(points[record.at].Flags.Contains)) ||
                    !unique.Add(record.at + ":" + record.id))
                    throw new InvalidOperationException("调查记录不属于合法剧情时间线");
                last = record.at;
            }
            return records.Select(v => new HotspotVisit { id = v.id, node = v.node, at = v.at }).ToArray();
        }

        public static GameProgress Inspect(GameCatalog catalog, GameProgress progress, string hotspotId)
        {
            var state = EchoNarrative.Normalize(catalog, progress);
            if (!catalog.Hotspots.TryGetValue(hotspotId ?? "", out var hotspot) ||
                hotspot.node != state.node ||
                (hotspot.requiresFlags != null && !hotspot.requiresFlags.All(state.flags.Contains)))
                throw new InvalidOperationException("该调查目标未在当前场景解锁");
            if (IsInspected(state, hotspot)) return state;
            if (state.inspections.Length >= MaxVisits)
                throw new InvalidOperationException("调查记录超过安全上限");
            state.inspections = state.inspections.Concat(new[] {
                new HotspotVisit { id = hotspot.id, node = state.node, at = state.history.Length }
            }).ToArray();
            return EchoNarrative.Normalize(catalog, state);
        }

        public static bool IsEvidenceVerified(GameCatalog catalog, GameProgress progress, string hotspotId)
        {
            var state = EchoNarrative.Normalize(catalog, progress);
            return catalog.Hotspots.TryGetValue(hotspotId ?? "", out var hotspot) &&
                !string.IsNullOrEmpty(hotspot.evidenceId) && state.flags.Contains(hotspot.evidenceId);
        }
    }
}

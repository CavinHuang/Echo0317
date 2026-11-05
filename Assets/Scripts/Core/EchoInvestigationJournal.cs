using System;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>One scene observation, validated against the active timeline.</summary>
    public sealed class EchoJournalEntry
    {
        public string Id, NodeId, Label, Description, SceneTitle, Clock, EvidenceId, EvidenceLabel;
        public int Step, Chapter;
        public bool EvidenceVerified;
        public string Key => Step + ":" + Id;
    }

    public sealed class EchoJournalPage
    {
        public EchoJournalEntry[] Entries;
        public int Page, MaxPage, TotalEntries;
        public bool HasOlder => Page < MaxPage;
        public bool HasNewer => Page > 0;
    }

    /// <summary>
    /// Reverse-chronological, read-only notebook for actual observed hotspots.
    /// It cannot create evidence: "verified" means a narrative choice/entry
    /// granted the associated flag on the current canonical timeline.
    /// </summary>
    public static class EchoInvestigationJournal
    {
        public const int DefaultPageSize = 8;
        public const int MaxPageSize = 20;

        public static EchoJournalPage Read(GameCatalog catalog, GameProgress progress,
            int page = 0, int pageSize = DefaultPageSize)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var state = EchoNarrative.Normalize(catalog, progress);
            pageSize = Math.Max(1, Math.Min(MaxPageSize, pageSize));
            var visits = state.inspections ?? Array.Empty<HotspotVisit>();
            int count = visits.Length;
            int lastPage = count == 0 ? 0 : (count - 1) / pageSize;
            page = Math.Max(0, Math.Min(lastPage, page));
            var entries = visits.Reverse().Skip(page * pageSize).Take(pageSize)
                .Select(visit => {
                    var hotspot = catalog.Hotspots[visit.id];
                    var scene = catalog.Nodes[visit.node];
                    bool verified = !string.IsNullOrEmpty(hotspot.evidenceId) &&
                        state.flags.Contains(hotspot.evidenceId);
                    return new EchoJournalEntry {
                        Id = visit.id, NodeId = visit.node, Label = hotspot.label,
                        Description = hotspot.description, SceneTitle = scene.title,
                        Clock = scene.clock, Chapter = scene.chapterIndex,
                        Step = visit.at, EvidenceId = hotspot.evidenceId ?? "",
                        EvidenceLabel = string.IsNullOrEmpty(hotspot.evidenceId) ? "" :
                            catalog.EvidenceName(hotspot.evidenceId),
                        EvidenceVerified = verified
                    };
                }).ToArray();
            return new EchoJournalPage {
                Entries = entries, TotalEntries = count, Page = page, MaxPage = lastPage
            };
        }
    }
}

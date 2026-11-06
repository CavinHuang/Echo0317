using System;
using System.Collections.Generic;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>A visited story node and the actual choice made there. Never contains locked alternatives.</summary>
    public sealed class DialogueHistoryEntry
    {
        public int StepIndex;
        public string NodeId;
        public int Chapter;
        public string Title;
        public string Speaker;
        public string Clock;
        public string[] Lines;
        public string Choice;
        public bool IsCurrent;
    }

    public sealed class DialogueHistoryPage
    {
        public int Page;
        public int MaxPage;
        public int TotalEntries;
        public DialogueHistoryEntry[] Entries;

        public bool HasOlder => Page < MaxPage;
        public bool HasNewer => Page > 0;
    }

    /// <summary>
    /// A save-derived read-only transcript for the current loop. No separate log
    /// is persisted, so loading a save, rewinding or starting over cannot leak
    /// abandoned paths or reveal unvisited dialogue.
    /// </summary>
    public static class EchoDialogueHistory
    {
        public const int DefaultPageSize = 12;
        public const int MaxPageSize = 30;

        public static DialogueHistoryPage Read(GameCatalog catalog, GameProgress progress,
            int page = 0, int pageSize = DefaultPageSize)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (page < 0) throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize < 1 || pageSize > MaxPageSize) throw new ArgumentOutOfRangeException(nameof(pageSize));

            // Validate the entire route before presenting its transcript. This also
            // canonicalizes labels from legacy saves with an omitted choice text.
            var state = EchoNarrative.Normalize(catalog, progress);
            var total = checked(state.history.Length + 1); // intro or current node is always visible
            var maxPage = (total - 1) / pageSize;
            var selectedPage = Math.Min(page, maxPage);
            var end = total - selectedPage * pageSize;
            var start = Math.Max(0, end - pageSize);
            var entries = new List<DialogueHistoryEntry>(end - start);
            var points = EchoNarrative.Trace(catalog, state.history).Points;
            for (int i = start; i < end; i++)
            {
                var step = i < state.history.Length ? state.history[i] : null;
                var id = step == null ? state.node : step.from;
                var node = catalog.Nodes[id];
                entries.Add(new DialogueHistoryEntry {
                    StepIndex = i,
                    NodeId = id,
                    Chapter = node.chapterIndex,
                    Title = node.title ?? "",
                    Speaker = node.speaker ?? "",
                    Clock = node.clock ?? "",
                    Lines = EchoScenePresentation.Lines(node, points[i].Flags,
                        state.attempts.Where(a => a.at <= i),
                        state.inspections.Where(v => v.at <= i)),
                    Choice = step?.choice ?? "",
                    IsCurrent = step == null
                });
            }
            return new DialogueHistoryPage {
                Page = selectedPage,
                MaxPage = maxPage,
                TotalEntries = total,
                Entries = entries.ToArray()
            };
        }
    }
}

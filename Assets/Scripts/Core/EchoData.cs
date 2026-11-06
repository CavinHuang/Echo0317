using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Echo0317.Core
{
    [Serializable] public class StoryGraph
    {
        public string title, revision;
        public int totalClips;
        public StoryNode[] nodes;
        public EvidenceDefinition[] evidence;
        public MediaDefinition[] media;
        public ChapterDefinition[] chapters;
    }
    [Serializable] public class StoryNode
    {
        public string id, clip, stage, chapter, clock, kicker, title, speaker;
        public int chapterIndex;
        public string[] lines;
        public string @next, nextLabel, endingLabel;
        public bool ending;
        public StoryChoice[] choices;
        public string[] entryFlags;
        public StoryReaction[] reactions;
    }
    [Serializable] public class StoryReaction { public string @if, unless, ifInspected, unlessInspected, text; }
    [Serializable] public class StoryChoice
    {
        public string label, to, flag, lockedLabel;
        public string[] requires, needsAny, forbids, requiresSolved;
    }
    [Serializable] public class EvidenceDefinition { public string id, label; }
    [Serializable] public class MediaDefinition { public string id, src, status, purpose; public bool ready; }
    [Serializable] public class ChapterDefinition { public int index; public string name; }
    [Serializable] public class CasesFile { public DeductionRule[] rules; }
    [Serializable] public class DeductionRule
    {
        public string id, title, correct, explanation, hint;
        public int chapter;
        public string[] clues;
        public DeductionOption[] options;
    }
    [Serializable] public class DeductionOption { public string id, text; }
    [Serializable] public class CaptionsFile { public CaptionTrack[] tracks; }
    [Serializable] public class CaptionTrack { public string id; public CaptionCue[] cues; }
    [Serializable] public class CaptionCue { public float start, end; public string text; }
    [Serializable] public class ShotsFile { public ShotDefinition[] shots; }
    [Serializable] public class ShotDefinition
    {
        public string id, status, path;
        public int chapter;
        public float duration;
        public string[] referenceIds;
    }

    public sealed class GameCatalog
    {
        public readonly StoryGraph Story;
        public readonly CasesFile Cases;
        public readonly CaptionsFile Captions;
        public readonly ShotsFile Shots;
        public readonly HotspotFile HotspotData;
        public readonly Dictionary<string, StoryNode> Nodes = new Dictionary<string, StoryNode>();
        public readonly Dictionary<string, DeductionRule> Rules = new Dictionary<string, DeductionRule>();
        public readonly Dictionary<string, string> Evidence = new Dictionary<string, string>();
        public readonly Dictionary<string, string> ChapterNames = new Dictionary<string, string>();
        public readonly Dictionary<string, MediaDefinition> Media = new Dictionary<string, MediaDefinition>();
        public readonly Dictionary<string, ShotDefinition> ShotTasks = new Dictionary<string, ShotDefinition>();
        public readonly Dictionary<string, CaptionTrack> Tracks = new Dictionary<string, CaptionTrack>();
        public readonly Dictionary<string, HotspotDefinition> Hotspots = new Dictionary<string, HotspotDefinition>();

        public GameCatalog(string storyJson, string casesJson, string captionsJson, string shotsJson, string hotspotsJson = null)
        {
            Story = JsonUtility.FromJson<StoryGraph>(storyJson);
            Cases = JsonUtility.FromJson<CasesFile>(casesJson);
            Captions = JsonUtility.FromJson<CaptionsFile>(captionsJson);
            Shots = JsonUtility.FromJson<ShotsFile>(shotsJson);
            HotspotData = string.IsNullOrWhiteSpace(hotspotsJson) ? new HotspotFile() :
                JsonUtility.FromJson<HotspotFile>(hotspotsJson);
            if (HotspotData == null || HotspotData.hotspots == null)
                throw new InvalidDataException("调查热点数据不存在");
            if (Story == null || Story.nodes == null || Story.nodes.Length == 0)
                throw new InvalidDataException("剧情数据不存在");
            foreach (var n in Story.nodes) { if (Nodes.ContainsKey(n.id)) throw new InvalidDataException("重复节点 " + n.id); Nodes.Add(n.id, n); }
            foreach (var e in Story.evidence) Evidence.Add(e.id, e.label);
            foreach (var chapter in Story.chapters) ChapterNames.Add(chapter.index.ToString(), chapter.name);
            foreach (var m in Story.media) Media[m.id] = m;
            foreach (var rule in Cases.rules) Rules.Add(rule.id, rule);
            foreach (var track in Captions.tracks) Tracks.Add(track.id, track);
            foreach (var shot in Shots.shots) ShotTasks.Add(shot.id, shot);
            foreach (var h in HotspotData.hotspots)
            {
                if (h == null || string.IsNullOrWhiteSpace(h.id) || !Nodes.ContainsKey(h.node ?? "") ||
                    (h.evidenceId != null && h.evidenceId.Length > 0 && !Evidence.ContainsKey(h.evidenceId)) ||
                    h.x < 0f || h.y < 0f || h.w <= 0f || h.h <= 0f ||
                    h.x + h.w > 1f || h.y + h.h > 1f ||
                    float.IsNaN(h.x) || float.IsNaN(h.y) || float.IsNaN(h.w) || float.IsNaN(h.h) ||
                    Hotspots.ContainsKey(h.id))
                    throw new InvalidDataException("调查热点配置无效 " + h?.id);
                Hotspots.Add(h.id, h);
            }
            // Validate every relationship only after the complete hotspot
            // catalog is loaded. An invalid or future-only reference must
            // fail on import instead of silently fabricating a comparison.
            foreach (var h in HotspotData.hotspots)
            {
                if (string.IsNullOrEmpty(h.crossCheckId)) continue;
                if (h.crossCheckId == h.id ||
                    !Hotspots.TryGetValue(h.crossCheckId, out var related) ||
                    string.IsNullOrWhiteSpace(h.crossCheckText) ||
                    Nodes[related.node].chapterIndex > Nodes[h.node].chapterIndex)
                    throw new InvalidDataException("调查交叉引用无效 " + h.id);
            }
            if (!Nodes.ContainsKey("intro")) throw new InvalidDataException("缺少 intro 起始节点");
            foreach (var n in Story.nodes)
            {
                if (n.lines == null || n.lines.Length < 2) throw new InvalidDataException("空剧情节点 " + n.id);
                if (!Media.ContainsKey(n.clip)) throw new InvalidDataException("缺少镜头 " + n.clip);
                if (n.chapterIndex < 1 || n.chapterIndex > 5) throw new InvalidDataException("章节编号越界 " + n.id);
                if (!string.IsNullOrEmpty(n.@next) && !Nodes.ContainsKey(n.@next)) throw new InvalidDataException("断开的下一节点 " + n.id);
                if (n.reactions != null) foreach (var reaction in n.reactions)
                {
                    if (reaction == null || string.IsNullOrWhiteSpace(reaction.text))
                        throw new InvalidDataException("剧情反馈文本无效 " + n.id);
                    if (HotspotData.hotspots.Length > 0 &&
                        !string.IsNullOrEmpty(reaction.ifInspected) &&
                        !Hotspots.ContainsKey(reaction.ifInspected))
                        throw new InvalidDataException("剧情反馈引用了未知调查点 " + n.id);
                    if (HotspotData.hotspots.Length > 0 &&
                        !string.IsNullOrEmpty(reaction.unlessInspected) &&
                        !Hotspots.ContainsKey(reaction.unlessInspected))
                        throw new InvalidDataException("剧情反馈排除条件引用了未知调查点 " + n.id);
                }
                if (n.choices != null) foreach (var c in n.choices)
                {
                    if (!Nodes.ContainsKey(c.to)) throw new InvalidDataException("非法分支 " + n.id + " -> " + c.to);
                    if (c.requiresSolved != null)
                        foreach (var ruleId in c.requiresSolved)
                            if (!Rules.ContainsKey(ruleId))
                                throw new InvalidDataException("分支引用了不存在的推理案件 " + n.id + " -> " + ruleId);
                }
            }
        }
        public static GameCatalog LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Data");
            return new GameCatalog(
                File.ReadAllText(Path.Combine(dir, "story.json")),
                File.ReadAllText(Path.Combine(dir, "cases.json")),
                File.ReadAllText(Path.Combine(dir, "captions.json")),
                File.ReadAllText(Path.Combine(dir, "shots.json")),
                File.Exists(Path.Combine(dir, "hotspots.json")) ?
                    File.ReadAllText(Path.Combine(dir, "hotspots.json")) : null);
        }
        public string EvidenceName(string id) => Evidence.TryGetValue(id, out var text) ? text : id;
        public string ChapterName(int index) => ChapterNames.TryGetValue(index.ToString(), out var value) ? value : "第" + index + "幕";
        public string Cue(string clip, double time)
        {
            if (Tracks.TryGetValue(clip, out var track)) foreach (var cue in track.cues)
                if (time >= cue.start && time < cue.end) return cue.text;
            return string.Empty;
        }
    }
}

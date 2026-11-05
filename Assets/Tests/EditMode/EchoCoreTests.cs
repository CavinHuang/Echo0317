#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public class EchoCoreTests
    {
        GameCatalog g;
        [SetUp] public void Prepare() { g = GameCatalog.LoadFromStreamingAssets(); }
        [Test] public void MigratedNarrativeIsComplete()
        {
            Assert.AreEqual("v3.0", g.Story.revision);
            Assert.AreEqual(55, g.Nodes.Count);
            Assert.AreEqual(5, g.Rules.Count);
            Assert.AreEqual(5, g.Story.chapters.Length);
            Assert.AreEqual(24, g.Media.Count);
            Assert.AreEqual(8, g.Tracks.Count);
            Assert.AreEqual(43, g.ShotTasks.Count); // 40 work orders + 3 non-render parent IDs
        }
        [Test] public void AllEndingPathsAndEveryNodeReachable()
        {
            var pending = new Queue<(string node,HashSet<string> flags)>();
            pending.Enqueue(("intro",new HashSet<string>()));
            var seen = new HashSet<string>();var nodes = new HashSet<string>();var endings = new HashSet<string>();
            while(pending.Count>0)
            {
                var item = pending.Dequeue();
                string key = item.node + "|" + string.Join("/",item.flags.OrderBy(x=>x));
                if(!seen.Add(key))continue;
                Assert.Less(seen.Count,100000);
                nodes.Add(item.node);
                var n=g.Nodes[item.node];
                if(n.ending) { endings.Add(item.node); continue; }
                foreach(var choice in EchoNarrative.Choices(n))
                {
                    if(!EchoNarrative.Allowed(choice,item.flags))continue;
                    var flags=new HashSet<string>(item.flags);
                    if(!string.IsNullOrEmpty(choice.flag))flags.Add(choice.flag);
                    foreach(var f in g.Nodes[choice.to].entryFlags ?? Array.Empty<string>())flags.Add(f);
                    if(choice.to=="c5_sign")
                    {
                        foreach(var must in new[]{"incident_proof","cache46","source_verified","protocol_read","lin_willing"})Assert.IsTrue(flags.Contains(must));
                        Assert.IsTrue(flags.Contains("shadow")||flags.Contains("render_log"));
                        Assert.IsTrue(!flags.Contains("coerced")||flags.Contains("amends"));
                    }
                    pending.Enqueue((choice.to,flags));
                }
            }
            Assert.AreEqual(55,nodes.Count);
            CollectionAssert.AreEquivalent(new[]{"ending_merge","ending_loop","ending_stop","ending_audit"},endings);
            Assert.AreEqual(69497,seen.Count);
        }
        [Test] public void SaveFormatAndTamperingValidation()
        {
            var p=EchoNarrative.Fresh();
            p=EchoNarrative.Advance(g,p,EchoNarrative.Choices(g.Nodes["intro"])[0]);
            var envelope=new SaveEnvelope{game="0317-echo",format=6,storyRevision=g.Story.revision,exportedAt="test",state=p};
            var dir=Path.Combine(Application.temporaryCachePath,"EchoTestSaves");
            var repo=new EchoSaves(g,dir);
            Assert.AreEqual(p.node,repo.Parse(JsonUtility.ToJson(envelope)).node);
            var illegal=JsonUtility.FromJson<SaveEnvelope>(JsonUtility.ToJson(envelope));
            illegal.state.node="ending_audit";
            Assert.Throws<InvalidOperationException>(()=>repo.Parse(JsonUtility.ToJson(illegal)));
        }
        [Test] public void RewindKeepsCollectedEndingsButRemovesFutureFlags()
        {
            var p=EchoNarrative.Fresh(); p.endings=new[]{"ending_merge"};
            p=EchoNarrative.Advance(g,p,EchoNarrative.Choices(g.Nodes["intro"])[0]);
            p=EchoNarrative.Advance(g,p,EchoNarrative.Choices(g.Nodes["warning"])[0]);
            Assert.IsTrue(p.flags.Contains("shadow"));
            p=EchoNarrative.Rewind(g,p,1);
            Assert.IsFalse(p.flags.Contains("shadow"));
            CollectionAssert.Contains(p.endings,"ending_merge");
        }
        [Test] public void RealLegacySaveJsonIsImportable()
        {
            var file=Path.Combine(Application.streamingAssetsPath,"Data","stage8-sample-save.json");
            Assert.IsTrue(File.Exists(file));
            var repo=new EchoSaves(g,Path.Combine(Application.temporaryCachePath,"EchoMigrationTests"));
            var state=repo.Parse(File.ReadAllText(file));
            Assert.AreEqual("c1_peephole",state.node);
            Assert.IsTrue(state.flags.Contains("shadow"));
        }
        [Test] public void ReplayingAValidDestinationWithAForgedChoiceLabelIsRejected()
        {
            var valid = EchoNarrative.Advance(g, EchoNarrative.Fresh(), EchoNarrative.Choices(g.Nodes["intro"])[0]);
            var forged = new GameProgress {
                node = valid.node,
                history = valid.history.Select(h => new Step {
                    from = h.from, to = h.to, chapter = h.chapter, choice = "假造的选项文字"
                }).ToArray()
            };
            Assert.Throws<InvalidOperationException>(() => EchoNarrative.Normalize(g, forged));
            // Older exports with an omitted label may be migrated *only* when
            // exactly one destination choice exists.
            forged.history[0].choice = "";
            Assert.AreEqual(valid.node, EchoNarrative.Normalize(g, forged).node);
        }
        [Test] public void AutoSaveAndManualSlotRecoverFromThePreviousGeneration()
        {
            var directory = Path.Combine(Application.temporaryCachePath, "Echo-Backup-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new EchoSaves(g, directory);
                var initial = EchoNarrative.Fresh();
                var after = EchoNarrative.Advance(g, initial, EchoNarrative.Choices(g.Nodes["intro"])[0]);
                store.AutoSave(initial);
                store.AutoSave(after);
                store.SaveSlot(1, initial);
                store.SaveSlot(1, after);
                File.WriteAllText(Path.Combine(directory, "autosave.json"), "{broken json");
                File.WriteAllText(Path.Combine(directory, "slot-1.json"), "{broken json");
                Assert.IsTrue(store.HasAuto);
                Assert.AreEqual("intro", store.LoadAuto().node);
                Assert.AreEqual("intro", store.LoadSlot(1).node);
                store.DeleteSlot(1);
                Assert.IsFalse(store.HasSlot(1));
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }
    }
}
#endif

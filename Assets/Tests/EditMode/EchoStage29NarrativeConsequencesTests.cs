#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Tests
{
    /// <summary>Unity Editor coverage for canonical deduction gates and visible story consequences.</summary>
    public sealed class EchoStage29NarrativeConsequencesTests
    {
        GameCatalog catalog;

        [SetUp] public void Setup() { catalog = GameCatalog.LoadFromStreamingAssets(); }

        GameProgress AuditAtGate()
        {
            var data = JsonUtility.FromJson<EndingJourneyCollection>(File.ReadAllText(
                Path.Combine(Application.streamingAssetsPath,"Data","verified-ending-paths.json")));
            var route = data.paths.Single(p => p.ending == "ending_audit");
            var state = EchoNarrative.Fresh();
            foreach (var id in route.nodes.Skip(1).Take(route.nodes.Length - 2))
            {
                var choice = EchoNarrative.Choices(catalog.Nodes[state.node]).Single(c =>
                    c.to == id && EchoNarrative.AllowedWithDeductions(catalog,c,state));
                state = EchoNarrative.Advance(catalog,state,choice);
            }
            Assert.AreEqual("c5_gate",state.node);
            Assert.Contains("joint_signed",state.flags);
            return state;
        }

        [Test] public void AuditEndingCannotBypassIndependentWillDeduction()
        {
            var before = AuditAtGate();
            var audit = EchoNarrative.Choices(catalog.Nodes[before.node])
                .Single(c => c.to == "ending_audit");
            Assert.IsFalse(EchoNarrative.AllowedWithDeductions(catalog,audit,before));
            Assert.Throws<InvalidOperationException>(() =>
                EchoNarrative.Advance(catalog,before,audit));

            var proven = EchoNarrative.RecordDeduction(catalog,before,
                "independent_will",catalog.Rules["independent_will"].correct);
            Assert.IsTrue(EchoNarrative.AllowedWithDeductions(catalog,audit,proven));
            var ending = EchoNarrative.Advance(catalog,proven,audit);
            Assert.AreEqual("ending_audit",ending.node);
            Assert.IsTrue(EchoNarrative.IsSolved(catalog,ending,"independent_will"));

            ending.attempts = Array.Empty<Attempt>();
            Assert.Throws<InvalidOperationException>(() =>
                EchoNarrative.Normalize(catalog,ending),
                "Forging an old finished route without deduction cannot bypass the canonical gate");
        }

        [Test] public void OldAuditSaveIsMigratedButNewFormatCannotForgeProof()
        {
            var before = AuditAtGate();
            var legacy = new GameProgress {
                node = "ending_audit",
                history = before.history.Concat(new[] {
                    new Step {from="c5_gate",to="ending_audit",
                        choice=EchoNarrative.Choices(catalog.Nodes["c5_gate"])
                        .Single(c=>c.to=="ending_audit").label,chapter=5}
                }).ToArray(),
                endings=new[]{"ending_audit"}
            };
            var root = Path.Combine(Application.temporaryCachePath,
                "echo-stage29-"+Guid.NewGuid().ToString("N"));
            try
            {
                var saves = new EchoSaves(catalog,root);
                var oldFile = JsonUtility.ToJson(new SaveEnvelope {
                    format=6,storyRevision=catalog.Story.revision,state=legacy
                });
                var loaded = saves.Parse(oldFile);
                Assert.IsTrue(EchoNarrative.IsSolved(catalog,loaded,"independent_will"));
                Assert.AreEqual("ending_audit",loaded.node);
                var newFile = JsonUtility.ToJson(new SaveEnvelope {
                    format=7,storyRevision=catalog.Story.revision,state=legacy
                });
                Assert.Throws<InvalidOperationException>(()=>saves.Parse(newFile));
            }
            finally { if (Directory.Exists(root)) Directory.Delete(root,true); }
        }

        [Test] public void AuthoredFlagsActuallyChangeLiveStoryAndHistoricalTranscript()
        {
            var scene=catalog.Nodes["c1_hook"];
            var withProof=EchoScenePresentation.Lines(scene,
                new[]{"tested_password"},Array.Empty<Attempt>());
            var withoutProof=EchoScenePresentation.Lines(scene,
                new[]{"photo"},Array.Empty<Attempt>());
            Assert.IsTrue(withProof.Any(l=>l.Contains("知道她过去怎么说")));
            Assert.IsFalse(withoutProof.Any(l=>l.Contains("知道她过去怎么说")));
            Assert.IsTrue(withoutProof.Any(l=>l.Contains("照片背面写着")));

            var gate=catalog.Nodes["c5_gate"];
            var before=EchoScenePresentation.Lines(gate,new[]{"joint_signed"},Array.Empty<Attempt>());
            var after=EchoScenePresentation.Lines(gate,new[]{"joint_signed"},
                new[]{new Attempt {rule="independent_will",answer="independent"}});
            Assert.IsTrue(before.Any(l=>l.Contains("先在「证据推理」核实")));
            Assert.IsTrue(after.Any(l=>l.Contains("现在可以提交独立审计")));
        }
    }
}
#endif

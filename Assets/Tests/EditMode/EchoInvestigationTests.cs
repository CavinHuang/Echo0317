#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoInvestigationTests
    {
        [Serializable] sealed class RouteCollection { public EndingRoute[] paths; }
        [Serializable] sealed class EndingRoute { public string ending; public string[] nodes; }

        [Test]
        public void NewLoopShowsNoEvidenceOrFutureCaseTitles()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var fresh = EchoNarrative.Fresh();
            Assert.IsEmpty(EchoInvestigation.Archive(catalog, fresh));
            var cards = EchoInvestigation.VisibleCases(catalog, fresh);
            Assert.AreEqual(1, cards.Length, "Only first-chapter cases may be revealed");
            Assert.AreEqual("door_identity", cards[0].Id);
            Assert.IsFalse(cards[0].Ready);
            Assert.IsFalse(EchoInvestigation.PairMatches(catalog, fresh,
                "door_identity", new[] { "shadow", "tested_password" }));
            Assert.IsFalse(EchoInvestigation.PairMatches(catalog, fresh,
                "independent_will", new[] { "protocol_read", "lin_willing" }));
        }

        [Test]
        public void EvidenceArchiveAndPinnedPairsFollowRealEndingsAndRewind()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var routes = JsonUtility.FromJson<RouteCollection>(File.ReadAllText(
                Path.Combine(Application.streamingAssetsPath, "Data", "verified-ending-paths.json")));
            Assert.AreEqual(4, routes.paths.Length);
            int matchedCases = 0;
            foreach (var route in routes.paths)
            {
                var state = EchoNarrative.Fresh();
                foreach (var id in route.nodes.Skip(1))
                {
                    var option = EchoNarrative.Choices(catalog.Nodes[state.node])
                        .Single(c => c.to == id && EchoNarrative.Allowed(c, state.flags));
                    state = EchoNarrative.Advance(catalog, state, option);
                }
                var archive = EchoInvestigation.Archive(catalog, state);
                CollectionAssert.AreEquivalent(state.flags.Where(catalog.Evidence.ContainsKey),
                    archive.Select(x => x.Id));
                Assert.IsTrue(archive.All(x => x.AcquiredAtStep <= state.history.Length));
                var titles = EchoInvestigation.VisibleCases(catalog, state);
                Assert.IsTrue(titles.All(c => c.Chapter <= catalog.Nodes[state.node].chapterIndex));
                foreach (var card in titles)
                {
                    var rule = catalog.Rules[card.Id];
                    if (!card.Ready) continue;
                    matchedCases++;
                    Assert.IsTrue(EchoInvestigation.PairMatches(catalog, state, rule.id, rule.clues));
                    Assert.IsFalse(EchoInvestigation.PairMatches(catalog, state, rule.id,
                        new[] { rule.clues[0], rule.clues[0] }));
                    var unrelated = archive.FirstOrDefault(c => !rule.clues.Contains(c.Id));
                    if (unrelated != null)
                        Assert.IsFalse(EchoInvestigation.PairMatches(catalog, state, rule.id,
                            new[] { rule.clues[0], unrelated.Id }));
                }
                var rewound = EchoNarrative.Rewind(catalog, state, 0);
                Assert.IsEmpty(EchoInvestigation.Archive(catalog, rewound));
            }
            Assert.Greater(matchedCases, 0, "Ending fixtures should cover at least one solved pair");
        }

        [Test]
        public void ForgedOrDuplicateEvidenceCannotUnlockDeduction()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var fake = EchoNarrative.Fresh();
            fake.flags = new[] { "shadow", "tested_password" };
            Assert.IsFalse(EchoInvestigation.PairMatches(catalog, fake,
                "door_identity", fake.flags));
            Assert.IsFalse(EchoInvestigation.PairMatches(catalog, fake,
                "door_identity", null));
            Assert.IsFalse(EchoInvestigation.PairMatches(catalog, fake,
                "missing_rule", new[] { "shadow", "tested_password" }));
            Assert.IsEmpty(EchoInvestigation.Archive(catalog, fake));
        }
    }
}
#endif

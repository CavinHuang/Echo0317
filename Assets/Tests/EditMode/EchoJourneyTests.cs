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
    [Serializable] public class EndingJourneyCollection { public EndingJourney[] paths; }
    [Serializable] public class EndingJourney { public string ending; public string[] nodes; }

    /// <summary>
    /// Native C# replay of the four Stage 8 verified routes. This deliberately exercises
    /// the Unity narrative engine rather than just checking the exported JSON graph.
    /// </summary>
    public sealed class EchoJourneyTests
    {
        GameCatalog catalog;
        [SetUp] public void Prepare() { catalog = GameCatalog.LoadFromStreamingAssets(); }

        [Test] public void EveryVerifiedEndingIsActuallyPlayableInCSharp()
        {
            var path = Path.Combine(Application.streamingAssetsPath, "Data", "verified-ending-paths.json");
            var journeys = JsonUtility.FromJson<EndingJourneyCollection>(File.ReadAllText(path));
            Assert.NotNull(journeys);
            Assert.AreEqual(4, journeys.paths.Length);
            var unlocked = new HashSet<string>();
            foreach (var route in journeys.paths)
            {
                Assert.AreEqual("intro", route.nodes[0], route.ending);
                var state = EchoNarrative.Fresh();
                for (var i = 1; i < route.nodes.Length; i++)
                {
                    var destination = route.nodes[i];
                    var legal = EchoNarrative.Choices(catalog.Nodes[state.node])
                        .Where(c => c.to == destination && EchoNarrative.Allowed(c, state.flags)).ToArray();
                    Assert.AreEqual(1, legal.Length, route.ending + " at " + state.node + " -> " + destination);
                    state = EchoNarrative.Advance(catalog, state, legal[0]);
                    Assert.AreEqual(destination, state.node);
                }
                Assert.AreEqual(route.ending, state.node);
                Assert.IsTrue(catalog.Nodes[state.node].ending);
                CollectionAssert.Contains(state.endings, route.ending);
                unlocked.Add(route.ending);
            }
            CollectionAssert.AreEquivalent(new [] { "ending_merge", "ending_loop", "ending_stop", "ending_audit" }, unlocked);
        }

        [Test] public void HeadlessPlayerQaUsesTheSameCompiledNarrativeEngine()
        {
            // In the Editor this checks the same code executed by -echoQa in a
            // built Windows Player. It does NOT claim that the Player was built.
            var report = new Echo0317.Runtime.NativeQaReport();
            Echo0317.Runtime.EchoNativeQa.Verify(report);
            Assert.AreEqual(55, report.nodes);
            Assert.AreEqual(5, report.rules);
            Assert.AreEqual(69497, report.graphStatesVerified);
            Assert.AreEqual(55, report.graphNodesReachable);
            Assert.AreEqual(0, report.deadEndStates);
            Assert.AreEqual(4, report.endingsVerified);
            Assert.AreEqual(123, report.choicesVerified);
            Assert.IsTrue(report.saveRoundtripPassed);
            Assert.IsTrue(report.tamperingRejected);
        }

        [Test] public void SameNarrativeCanBeSavedAndContinuedAfterReload()
        {
            var route = JsonUtility.FromJson<EndingJourneyCollection>(File.ReadAllText(
                Path.Combine(Application.streamingAssetsPath, "Data", "verified-ending-paths.json"))).paths[3];
            var state = EchoNarrative.Fresh();
            var directory = Path.Combine(Application.temporaryCachePath, "Echo0317-Journey-" + Guid.NewGuid().ToString("N"));
            try
            {
                var storage = new EchoSaves(catalog, directory);
                for (var i = 1; i < route.nodes.Length; i++)
                {
                    var choice = EchoNarrative.Choices(catalog.Nodes[state.node]).Single(c =>
                        c.to == route.nodes[i] && EchoNarrative.Allowed(c, state.flags));
                    state = EchoNarrative.Advance(catalog, state, choice);
                    if (i == 11)
                    {
                        storage.SaveSlot(1, state);
                        state = storage.LoadSlot(1);
                        Assert.AreEqual(route.nodes[i], state.node);
                    }
                }
                Assert.AreEqual("ending_audit", state.node);
                var afterEnding = EchoNarrative.Rewind(catalog, state, 4);
                Assert.AreEqual(route.nodes[4], afterEnding.node);
                CollectionAssert.Contains(afterEnding.endings, "ending_audit");
            }
            finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
        }

        [Test] public void LockedChoiceAndForgedFlagCannotAdvance()
        {
            var initial = EchoNarrative.Fresh();
            var falsified = EchoNarrative.Fresh();
            falsified.flags = new [] { "incident_proof", "lin_willing", "source_verified" };
            Assert.IsEmpty(EchoNarrative.Normalize(catalog, falsified).flags);
            var forbidden = catalog.Nodes["c5_gate"].choices.FirstOrDefault(c =>
                c.to == "c5_sign");
            Assert.NotNull(forbidden);
            Assert.IsFalse(EchoNarrative.Allowed(forbidden, initial.flags));
            Assert.Throws<InvalidOperationException>(() => EchoNarrative.Advance(catalog, initial, forbidden));
        }
    }
}
#endif

#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoStage24HotspotTests
    {
        [Test]
        public void HotspotSchemaHasValidScenesAndEvidence()
        {
            var catalog=GameCatalog.LoadFromStreamingAssets();
            Assert.AreEqual(23,catalog.Hotspots.Count);
            foreach(var spot in catalog.Hotspots.Values)
            {
                Assert.IsTrue(catalog.Nodes.ContainsKey(spot.node));
                Assert.IsTrue(spot.x>=0 && spot.y>=0 &&
                    spot.w>0 && spot.h>0 && spot.x+spot.w<=1 && spot.y+spot.h<=1);
                if(!string.IsNullOrEmpty(spot.evidenceId))
                    Assert.IsTrue(catalog.Evidence.ContainsKey(spot.evidenceId));
            }
        }

        [Test]
        public void InspectionsPersistWithoutForgingEvidenceAndRejectWrongScene()
        {
            var catalog=GameCatalog.LoadFromStreamingAssets();
            var fresh=EchoNarrative.Fresh();
            Assert.AreEqual(2,EchoHotspotInvestigation.Visible(catalog,fresh).Length);
            var inspected=EchoHotspotInvestigation.Inspect(catalog,fresh,"intro_phone");
            Assert.AreEqual(1,inspected.inspections.Length);
            Assert.IsEmpty(inspected.flags);
            Assert.IsFalse(EchoHotspotInvestigation.IsEvidenceVerified(catalog,inspected,"intro_phone"));
            Assert.AreEqual(1,EchoHotspotInvestigation.Inspect(catalog,inspected,"intro_phone").inspections.Length);
            Assert.Throws<InvalidOperationException>(()=>
                EchoHotspotInvestigation.Inspect(catalog,inspected,"c2_wave_match"));
            var forged=EchoNarrative.Fresh();
            forged.inspections=new[]{new HotspotVisit{id="c2_wave_match",node="c2_voice",at=0}};
            Assert.Throws<InvalidOperationException>(()=>EchoNarrative.Normalize(catalog,forged));
        }

        [Test]
        public void RewindingDiscardsAbandonedHotspotsButNotEarnedNarrativeFlags()
        {
            var catalog=GameCatalog.LoadFromStreamingAssets();
            var warning=EchoNarrative.Advance(catalog,EchoNarrative.Fresh(),
                EchoNarrative.Choices(catalog.Nodes["intro"]).Single());
            var peephole=EchoNarrative.Advance(catalog,warning,
                EchoNarrative.Choices(catalog.Nodes["warning"])
                    .Single(c=>c.to=="c1_peephole"));
            var visited=EchoHotspotInvestigation.Inspect(catalog,peephole,"peephole_shadow");
            Assert.IsTrue(EchoHotspotInvestigation.IsEvidenceVerified(catalog,visited,"peephole_shadow"));
            CollectionAssert.AreEquivalent(peephole.flags,visited.flags);
            var rewind=EchoNarrative.Rewind(catalog,visited,0);
            Assert.IsEmpty(rewind.inspections);
            Assert.IsEmpty(rewind.flags);
        }
    }
}
#endif

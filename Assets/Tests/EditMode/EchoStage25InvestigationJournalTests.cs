#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoStage25InvestigationJournalTests
    {
        [Test]
        public void ObservationHasRealSourceButCannotGrantNarrativeEvidence()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var intro = EchoNarrative.Fresh();
            var empty = EchoInvestigationJournal.Read(catalog, intro);
            Assert.AreEqual(0, empty.TotalEntries);
            var visited = EchoHotspotInvestigation.Inspect(catalog, intro, "intro_phone");
            var page = EchoInvestigationJournal.Read(catalog, visited);
            Assert.AreEqual(1, page.TotalEntries);
            Assert.AreEqual("intro_phone", page.Entries[0].Id);
            Assert.AreEqual(0, page.Entries[0].Step);
            Assert.AreEqual("intro", page.Entries[0].NodeId);
            Assert.AreEqual(1, page.Entries[0].Chapter);
            Assert.IsFalse(page.Entries[0].EvidenceVerified);
            Assert.IsEmpty(visited.flags);
        }

        [Test]
        public void RealStoryChoiceVerifiesClueAndRewindRemovesAbandonedJournal()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var warning = EchoNarrative.Advance(catalog, EchoNarrative.Fresh(),
                EchoNarrative.Choices(catalog.Nodes["intro"]).Single());
            var peephole = EchoNarrative.Advance(catalog, warning,
                EchoNarrative.Choices(catalog.Nodes["warning"])
                    .Single(c => c.to == "c1_peephole"));
            var inspected = EchoHotspotInvestigation.Inspect(catalog, peephole, "peephole_shadow");
            var page = EchoInvestigationJournal.Read(catalog, inspected);
            Assert.AreEqual(1, page.TotalEntries);
            Assert.IsTrue(page.Entries[0].EvidenceVerified);
            Assert.AreEqual("shadow", page.Entries[0].EvidenceId);
            Assert.AreEqual("c1_peephole", page.Entries[0].NodeId);
            Assert.AreEqual(0, EchoInvestigationJournal.Read(catalog,
                EchoNarrative.Rewind(catalog, inspected, 0)).TotalEntries);
        }

        [Test]
        public void JournalPagesClampAndNewestEntriesComeFirst()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var original = EchoNarrative.Fresh();
            var one = EchoHotspotInvestigation.Inspect(catalog, original, "intro_photo");
            var both = EchoHotspotInvestigation.Inspect(catalog, one, "intro_phone");
            Assert.AreEqual("intro_phone",
                EchoInvestigationJournal.Read(catalog, both, 0, 1).Entries[0].Id);
            var older = EchoInvestigationJournal.Read(catalog, both, 1, 1);
            Assert.AreEqual("intro_photo", older.Entries[0].Id);
            Assert.IsTrue(older.HasNewer);
            Assert.AreEqual(1, EchoInvestigationJournal.Read(catalog, both, 999, 1).Page);
            Assert.AreEqual(0, EchoInvestigationJournal.Read(catalog, both, -999, 1).Page);
        }
    }
}
#endif

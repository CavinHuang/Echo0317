#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoDialogueHistoryTests
    {
        [Test]
        public void NewGameShowsOnlyCurrentIntro()
        {
            var game = GameCatalog.LoadFromStreamingAssets();
            var state = EchoNarrative.Fresh();
            var page = EchoDialogueHistory.Read(game, state);
            Assert.AreEqual(1, page.TotalEntries);
            Assert.AreEqual(0, page.MaxPage);
            Assert.IsFalse(page.HasOlder);
            Assert.IsFalse(page.HasNewer);
            Assert.AreEqual("intro", page.Entries[0].NodeId);
            Assert.IsTrue(page.Entries[0].IsCurrent);
            Assert.IsEmpty(page.Entries[0].Choice);
        }

        [Test]
        public void TranscriptRecordsActualChoicesAndPaginatesWithoutShowingFuture()
        {
            var game = GameCatalog.LoadFromStreamingAssets();
            var choice = EchoNarrative.Choices(game.Nodes["intro"])[0];
            var state = EchoNarrative.Advance(game, EchoNarrative.Fresh(), choice);
            var latest = EchoDialogueHistory.Read(game, state, 0, 1);
            Assert.AreEqual(2, latest.TotalEntries);
            Assert.AreEqual(1, latest.MaxPage);
            Assert.AreEqual(state.node, latest.Entries[0].NodeId);
            Assert.IsTrue(latest.Entries[0].IsCurrent);
            Assert.IsTrue(latest.HasOlder);
            var oldest = EchoDialogueHistory.Read(game, state, 1, 1);
            Assert.AreEqual("intro", oldest.Entries[0].NodeId);
            Assert.AreEqual(choice.label, oldest.Entries[0].Choice);
            Assert.IsFalse(oldest.Entries[0].IsCurrent);
            Assert.IsTrue(oldest.HasNewer);
            Assert.AreEqual(1, EchoDialogueHistory.Read(game, state, 500, 1).Page);
            var rewound = EchoDialogueHistory.Read(game, EchoNarrative.Rewind(game, state, 0));
            Assert.AreEqual(1, rewound.TotalEntries);
            Assert.AreEqual("intro", rewound.Entries[0].NodeId);
        }

        [Test]
        public void InvalidSavesAndOversizedPagesCannotExposeDialogue()
        {
            var game = GameCatalog.LoadFromStreamingAssets();
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoDialogueHistory.Read(game, EchoNarrative.Fresh(), -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => EchoDialogueHistory.Read(game, EchoNarrative.Fresh(), 0, 31));
            var choice = EchoNarrative.Choices(game.Nodes["intro"])[0];
            var state = EchoNarrative.Advance(game, EchoNarrative.Fresh(), choice);
            state.history[0].choice = "伪造分支";
            Assert.Throws<InvalidOperationException>(() => EchoDialogueHistory.Read(game, state));
        }
    }
}
#endif

#if UNITY_EDITOR
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoPlaybackStateTests
    {
        [Test]
        public void SkippingDuringPrepareInvalidatesThatRequestAndIsIdempotent()
        {
            var playback = new EchoPlaybackState();
            Assert.IsTrue(playback.Request("V01"));
            int first = playback.Generation;
            Assert.IsTrue(playback.Request("V02"));
            int pending = playback.Generation;
            Assert.IsFalse(playback.CanPresent(first));
            Assert.IsTrue(playback.Skip());
            Assert.IsTrue(playback.IsFinished);
            Assert.IsFalse(playback.CanPresent(pending));
            Assert.IsFalse(playback.Skip(), "Repeated skip should not duplicate completion events");
            Assert.IsFalse(playback.Request("V02"), "Same skipped clip must remain finished");
            Assert.IsTrue(playback.Request("V02", true), "Manual replay must be possible after skip");
            Assert.IsTrue(playback.CanPresent(playback.Generation));
        }

        [Test]
        public void PauseIsRetainedAcrossSwitchesAndResetOnMenuExit()
        {
            var playback = new EchoPlaybackState();
            playback.Pause();
            Assert.IsTrue(playback.Request("V23"));
            Assert.IsTrue(playback.IsPaused);
            int token = playback.Generation;
            playback.Halt();
            Assert.IsFalse(playback.IsPaused);
            Assert.IsFalse(playback.CanPresent(token));
            Assert.AreEqual("", playback.ClipId);
        }

        [Test]
        public void CurrentClipCanCompleteOnlyOnce()
        {
            var playback = new EchoPlaybackState();
            Assert.IsFalse(playback.Skip());
            Assert.IsTrue(playback.Request("V24"));
            int token = playback.Generation;
            Assert.IsTrue(playback.Complete(token));
            Assert.IsFalse(playback.Complete(token));
            Assert.IsFalse(playback.Skip());
            Assert.IsTrue(playback.IsFinished);
        }
    }
}
#endif

#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Echo0317.Core;
using Echo0317.Runtime;
using Echo0317.UI;

namespace Echo0317.Tests
{
    /// <summary>Real VideoPlayer integration; requires activated Unity PlayMode.</summary>
    public sealed class EchoStage20PlaybackTests
    {
        [UnityTest]
        public IEnumerator SkipDuringDecodeIsImmediateAndDoesNotRestartTheSkippedClip()
        {
            var ui = new EchoGameUI();
            var owner = new GameObject("Stage20-Playback");
            try
            {
                var director = owner.AddComponent<EchoVideoDirector>();
                director.Initialize(GameCatalog.LoadFromStreamingAssets(), ui.VideoLayer);
                int ended = 0;
                director.OnEnded = () => ended++;
                director.Show("V01");
                Assert.IsTrue(director.IsPreparing);
                director.Show("V02");
                Assert.AreEqual("V02", director.CurrentClip);
                Assert.IsTrue(director.IsPreparing);
                director.Skip();
                Assert.AreEqual(1, ended, "Pending clip should be skipped, not the old plate");
                Assert.IsFalse(director.IsPreparing);
                Assert.IsFalse(director.HasActiveVideo);
                Assert.AreEqual("V02", director.CurrentClip);
                director.Skip();
                Assert.AreEqual(1, ended, "Second skip must be idempotent");
                yield return null; // cancelled Prepare/first-frame must never surface later
                Assert.IsFalse(director.HasActiveVideo);
                Assert.IsFalse(director.IsPreparing);

                director.Replay();
                Assert.AreEqual("V02", director.CurrentClip);
                Assert.IsTrue(director.IsPreparing, "Explicit replay should request a fresh decoder");
                director.Pause();
                Assert.IsTrue(director.IsPaused);
                director.Resume();
                Assert.IsFalse(director.IsPaused);
                director.Halt();
                Assert.IsFalse(director.IsPreparing);
                Assert.AreEqual("", director.CurrentClip);
            }
            finally
            {
                Object.Destroy(owner);
                if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExitingSceneCancelsDecoderWhilePaused()
        {
            var ui = new EchoGameUI();
            var owner = new GameObject("Stage20-Exit");
            try
            {
                var director = owner.AddComponent<EchoVideoDirector>();
                director.Initialize(GameCatalog.LoadFromStreamingAssets(), ui.VideoLayer);
                director.Show("V24B");
                director.Pause();
                Assert.IsTrue(director.IsPaused);
                director.Halt();
                director.Resume();
                yield return null;
                Assert.IsFalse(director.HasActiveVideo);
                Assert.IsFalse(director.IsPreparing);
                Assert.AreEqual("", director.CurrentClip);
            }
            finally
            {
                Object.Destroy(owner);
                if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }
    }
}
#endif

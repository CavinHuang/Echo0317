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
    public sealed class EchoStage31CinematicFallbackTests
    {
        [UnityTest]
        public IEnumerator UnmadeSceneUsesSafeStaticArtAndRetainsReplayIdentity()
        {
            var ui = new EchoGameUI();
            var host = new GameObject("Stage31-FallbackHost");
            try
            {
                var director = host.AddComponent<EchoVideoDirector>();
                director.Initialize(GameCatalog.LoadFromStreamingAssets(),ui.VideoLayer);
                director.SetSceneArt("S01");
                Assert.NotNull(director.StaticImage.texture);
                director.SetSceneArt("nonexistent-stage31-scene");
                Assert.AreEqual("nonexistent-stage31-scene",director.SceneArtId);
                Assert.IsNull(director.StaticImage.texture);
                Assert.AreEqual(0f,director.StaticImage.color.a,.0001f,
                    "An absent image cannot reveal a stale frame or an opaque white quad.");
                director.SetSceneArt("S02");
                Assert.NotNull(director.StaticImage.texture);
                Assert.AreEqual(1f,director.StaticImage.color.a,.0001f);
                director.Show("V06"); // Missing from Previs; deliberately unfinished
                Assert.AreEqual("V06",director.CurrentClip);
                Assert.IsTrue(director.IsFallback);
                Assert.IsFalse(director.IsPreparing);
                Assert.IsFalse(director.HasActiveVideo);
                StringAssert.Contains("V06",director.LastFallbackReason);
                director.Replay();
                Assert.AreEqual("V06",director.CurrentClip);
                Assert.IsTrue(director.IsFallback);
                director.Halt();
                Assert.AreEqual("",director.CurrentClip);
                Assert.IsFalse(director.IsFallback);
                yield return null;
            }
            finally
            {
                Object.Destroy(host);
                if(ui.Canvas!=null)Object.Destroy(ui.Canvas.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator FoleyKeepsItsScheduleAcrossPauseMuteAndDuplicateRender()
        {
            var obj = new GameObject("Stage31-OriginalFoley");
            try
            {
                var foley = obj.AddComponent<EchoFirstActFoley>();
                foley.SetMuted(true);
                foley.SetScene("c1_knock@3","c1_knock",false);
                Assert.AreEqual(3,foley.PendingCues);
                foley.SetPaused(true);
                yield return new WaitForSecondsRealtime(.35f);
                Assert.AreEqual(3,foley.PendingCues,
                    "Paused cinematics must not advance the scheduled door knocks");
                foley.SetPaused(false);
                yield return new WaitForSecondsRealtime(.85f);
                Assert.Less(foley.PendingCues,3,
                    "First knock should play once after unpausing");
                int count = foley.PendingCues;
                foley.SetScene("c1_knock@3","c1_knock",false);
                Assert.AreEqual(count,foley.PendingCues,
                    "Re-render must not re-trigger a completed knock");
                foley.SetScene("c1_knock@4","c1_knock",true);
                Assert.AreEqual(0,foley.PendingCues,
                    "Approved movie soundtrack replaces procedural preview cues");
                foley.StopScene();
                Assert.AreEqual("",foley.CurrentVisit);
                Assert.AreEqual(0,foley.PendingCues);
            }
            finally { Object.Destroy(obj); }
            yield return null;
        }
    }
}
#endif

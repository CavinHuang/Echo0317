#if UNITY_INCLUDE_TESTS
using System;
using System.Linq;
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoStage31CinematicPolicyTests
    {
        [Test]
        public void AllTenFirstActSceneVisitsHaveOrderedOriginalFoley()
        {
            var catalog = GameCatalog.LoadFromStreamingAssets();
            var scenes = catalog.Nodes.Values.Where(n => n.chapterIndex == 1).ToArray();
            Assert.AreEqual(10, scenes.Length);
            Assert.IsTrue(EchoFirstActFoleyPlan.Valid());
            foreach (var scene in scenes)
            {
                var cues = EchoFirstActFoleyPlan.ForNode(scene.id);
                Assert.Greater(cues.Length, 0, scene.id);
                Assert.IsTrue(cues.All(c => c.Gain > 0 && c.Gain <= .4f));
            }
            var knocks = EchoFirstActFoleyPlan.ForNode("c1_knock");
            Assert.AreEqual(3, knocks.Length);
            CollectionAssert.AreEqual(new [] { "door-knock", "door-knock", "door-knock" },
                knocks.Select(c => c.Name).ToArray());
            Assert.Less(knocks[1].AtSeconds, knocks[2].AtSeconds);
            Assert.IsEmpty(EchoFirstActFoleyPlan.ForNode("ending_stop"));
        }

        [Test]
        public void LoadingAndPausedMovieNeverExposeLastStoryLineAsSubtitle()
        {
            var text = new [] { "第一句", "最后一句" };
            Assert.AreEqual("", EchoSubtitlePresentation.Select(true,true,false,
                "旧片字幕",text));
            Assert.AreEqual("当前镜头字幕", EchoSubtitlePresentation.Select(true,false,true,
                "当前镜头字幕",text));
            Assert.AreEqual("", EchoSubtitlePresentation.Select(true,false,true,"",text),
                "Paused movie within a subtitle gap must remain blank.");
            Assert.AreEqual("最后一句", EchoSubtitlePresentation.Select(true,false,false,
                "过期字幕",text));
            Assert.AreEqual("", EchoSubtitlePresentation.Select(false,false,false,
                "过期字幕",text));
        }
    }
}
#endif

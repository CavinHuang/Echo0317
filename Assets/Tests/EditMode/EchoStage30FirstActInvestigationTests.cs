#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoStage30FirstActInvestigationTests
    {
        GameCatalog catalog;

        [SetUp] public void Prepare(){catalog=GameCatalog.LoadFromStreamingAssets();}

        [Test] public void IntroObservationsUnlockOnlyRealCrosscheckedClues()
        {
            var blank=EchoNarrative.Fresh();
            var phone=EchoHotspotInvestigation.Inspect(catalog,blank,"intro_phone");
            Assert.AreEqual("",EchoHotspotInvestigation.CrossCheck(phone,catalog.Hotspots["intro_phone"]));
            var photo=EchoHotspotInvestigation.Inspect(catalog,phone,"intro_photo");
            StringAssert.Contains("两种记录互相担保",
                EchoHotspotInvestigation.CrossCheck(photo,catalog.Hotspots["intro_phone"]));
            Assert.IsEmpty(photo.flags);
            var page=EchoDialogueHistory.Read(catalog,photo);
            Assert.IsTrue(page.Entries[0].Lines.Any(line=>line.Contains("网络投递记录")));
            var same=EchoHotspotInvestigation.Inspect(catalog,photo,"intro_phone");
            Assert.AreEqual(2,same.inspections.Length);
        }

        [Test] public void DoorPasswordComparedToPeepholeChangesActualFirstActStory()
        {
            var fresh=EchoNarrative.Fresh();
            var warning=EchoNarrative.Advance(catalog,fresh,
                EchoNarrative.Choices(catalog.Nodes["intro"]).Single());
            var peephole=EchoNarrative.Advance(catalog,warning,
                EchoNarrative.Choices(catalog.Nodes["warning"])
                    .Single(c=>c.to=="c1_peephole"));
            peephole=EchoHotspotInvestigation.Inspect(catalog,peephole,"peephole_shadow");
            var knock=EchoNarrative.Advance(catalog,peephole,
                EchoNarrative.Choices(catalog.Nodes["c1_peephole"]).Single());
            var password=EchoNarrative.Advance(catalog,knock,
                EchoNarrative.Choices(catalog.Nodes["c1_knock"])
                    .Single(c=>c.to=="c1_password"));
            password=EchoHotspotInvestigation.Inspect(catalog,password,"door_password");
            StringAssert.Contains("两条线索",EchoHotspotInvestigation.CrossCheck(
                password,catalog.Hotspots["door_password"]));
            var hook=EchoNarrative.Advance(catalog,password,
                EchoNarrative.Choices(catalog.Nodes["c1_password"]).Single());
            var lines=EchoScenePresentation.Lines(catalog.Nodes["c1_hook"],
                hook.flags,hook.attempts,hook.inspections);
            Assert.IsTrue(lines.Any(x=>x.Contains("口令与停顿的差异")));
            Assert.IsTrue(EchoDialogueHistory.Read(catalog,hook).Entries.Last().Lines
                .Any(x=>x.Contains("口令与停顿的差异")));
            Assert.IsFalse(EchoScenePresentation.Lines(catalog.Nodes["c1_hook"],
                hook.flags,hook.attempts).Any(x=>x.Contains("口令与停顿的差异")));
            var rewind=EchoNarrative.Rewind(catalog,hook,3);
            Assert.IsFalse(EchoHotspotInvestigation.WasInspected(rewind,"door_password"));
            Assert.AreEqual("",EchoHotspotInvestigation.CrossCheck(
                rewind,catalog.Hotspots["door_password"]));
        }

        [Test] public void InvalidCrossReferencesFailDuringDataImport()
        {
            var dir=Path.Combine(Application.streamingAssetsPath,"Data");
            string story=File.ReadAllText(Path.Combine(dir,"story.json"));
            string cases=File.ReadAllText(Path.Combine(dir,"cases.json"));
            string captions=File.ReadAllText(Path.Combine(dir,"captions.json"));
            string shots=File.ReadAllText(Path.Combine(dir,"shots.json"));
            string hotspots=File.ReadAllText(Path.Combine(dir,"hotspots.json"));
            Assert.IsTrue(hotspots.Contains("\"crossCheckId\": \"intro_photo\""));
            string forged=hotspots.Replace("\"crossCheckId\": \"intro_photo\"",
                "\"crossCheckId\": \"not_a_real_hotspot\"");
            Assert.Throws<InvalidDataException>(()=>new GameCatalog(
                story,cases,captions,shots,forged));
        }
    }
}
#endif

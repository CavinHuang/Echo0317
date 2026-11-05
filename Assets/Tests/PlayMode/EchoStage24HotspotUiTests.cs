#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Echo0317.Core;
using Echo0317.UI;

namespace Echo0317.Tests
{
    public sealed class EchoStage24HotspotUiTests
    {
        [UnityTest]
        public IEnumerator InteractiveHotspotsReceiveClicksAndRestoreNarrativeFocus()
        {
            var ui=new EchoGameUI();
            try
            {
                var catalog=GameCatalog.LoadFromStreamingAssets();
                var state=EchoNarrative.Fresh();
                var overlay=new EchoInvestigationOverlay(ui);
                ui.SetGameVisible(true);
                ui.AddChoice("下一段剧情",()=>{});
                var previous=EventSystem.current.currentSelectedGameObject;
                ui.SetInvestigationInputLocked(true);
                var targets=EchoHotspotInvestigation.Visible(catalog,state);
                int clicks=0;
                overlay.Show(targets,state,null,false,false,id=>{
                    state=EchoHotspotInvestigation.Inspect(catalog,state,id);
                    clicks++;
                    overlay.Show(targets,state,id,true,false,unused=>{},
                        ()=>overlay.Hide());
                },()=>overlay.Hide());
                Assert.IsTrue(overlay.Root.activeSelf);
                Assert.AreEqual(2,overlay.VisibleHotspots);
                Assert.IsFalse(ui.Game.GetComponent<CanvasGroup>().interactable);
                var hotspot=overlay.HotspotArea.Find("Hotspot-intro_phone");
                Assert.NotNull(hotspot);
                Assert.NotNull(hotspot.GetComponent<EchoHotspotVisual>());
                Assert.NotNull(hotspot.GetComponent<EchoCancelAction>());
                Assert.NotNull(hotspot.GetComponent<Outline>());
                hotspot.GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1,clicks);
                Assert.AreEqual(1,state.inspections.Length);
                Assert.IsEmpty(state.flags);
                Assert.AreEqual("intro_phone",
                    EventSystem.current.currentSelectedGameObject
                        .GetComponent<EchoHotspotVisual>().HotspotId);
                ui.SetInvestigationInputLocked(false);
                overlay.Hide();
                Assert.IsFalse(overlay.Root.activeSelf);
                Assert.IsTrue(ui.Game.GetComponent<CanvasGroup>().interactable);
                Assert.AreSame(previous,EventSystem.current.currentSelectedGameObject);
                yield return null;
            }
            finally { if(ui.Canvas!=null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ArtIsTransparentAndFallsBackWithoutExternalDownloads()
        {
            var art=new EchoInvestigationArt();
            var idle=art.Get("hotspot-idle");
            var selected=art.Get("hotspot-focus");
            var done=art.Get("hotspot-complete");
            Assert.NotNull(idle);
            Assert.NotNull(selected);
            Assert.NotNull(done);
            Assert.AreSame(idle,art.Get("hotspot-idle"));
            Assert.NotNull(art.Get("inspect-result"));
            Assert.NotNull(art.Get("notice-new"));
            yield return null;
        }
    }
}
#endif

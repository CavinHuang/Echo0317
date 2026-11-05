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
    public sealed class EchoStage26FilmViewportTests
    {
        [UnityTest]
        public IEnumerator MovieAndHotspotsUseTheSameLetterboxedRectThenRestoreCleanly()
        {
            var ui=new EchoGameUI();
            var overlay=new EchoInvestigationOverlay(ui);
            var movie=ui.VideoLayer;
            var originalParent=movie.parent;
            var originalIndex=movie.GetSiblingIndex();
            var originalAnchors=movie.anchorMin;
            var originalOffsets=movie.offsetMin;
            try
            {
                ui.SetGameVisible(true);
                ui.AddChoice("继续剧情",()=>{});
                var previous=EventSystem.current.currentSelectedGameObject;
                ui.SetInvestigationInputLocked(true);
                overlay.MountFilm(movie);
                var catalog=GameCatalog.LoadFromStreamingAssets();
                var state=EchoNarrative.Fresh();
                overlay.Show(EchoHotspotInvestigation.Visible(catalog,state),state,
                    null,false,false,id=>{},()=>overlay.Hide());
                Assert.IsTrue(overlay.HasMountedFilm);
                Assert.AreSame(overlay.FilmStage,movie.parent);
                Assert.AreSame(movie,overlay.HotspotArea.parent);
                Assert.AreEqual(0,movie.GetSiblingIndex(),
                    "The actual movie must render beneath the interactive hotspots");
                Assert.AreSame(overlay.HotspotArea,
                    overlay.HotspotArea.Find("Hotspot-intro_phone").parent);
                var fitter=movie.GetComponent<AspectRatioFitter>();
                Assert.NotNull(fitter);
                Assert.AreEqual(AspectRatioFitter.AspectMode.FitInParent,fitter.aspectMode);
                Assert.AreEqual(16f/9f,fitter.aspectRatio,.0001f);
                yield return null;
                Canvas.ForceUpdateCanvases();
                if(movie.rect.height>1f)
                    Assert.AreEqual(16f/9f,movie.rect.width/movie.rect.height,.03f,
                        "Non-16:9 windows must letterbox the film and its hotspots together");
                ui.SetInvestigationInputLocked(false);
                overlay.Hide();
                Assert.IsFalse(overlay.HasMountedFilm);
                Assert.AreSame(originalParent,movie.parent);
                Assert.AreEqual(originalIndex,movie.GetSiblingIndex());
                Assert.AreEqual(originalAnchors,movie.anchorMin);
                Assert.AreEqual(originalOffsets,movie.offsetMin);
                Assert.AreSame(overlay.FilmStage,overlay.HotspotArea.parent);
                Assert.AreSame(previous,EventSystem.current.currentSelectedGameObject);
                yield return null;
            }
            finally
            {
                overlay.Hide();
                if(ui.Canvas!=null) Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitGamepadDirectionsAndCancelReturnOnlyOnce()
        {
            var ui=new EchoGameUI();
            var overlay=new EchoInvestigationOverlay(ui);
            try
            {
                var catalog=GameCatalog.LoadFromStreamingAssets();
                var state=EchoNarrative.Fresh();
                var targets=EchoHotspotInvestigation.Visible(catalog,state);
                int exits=0;
                ui.SetGameVisible(true);
                overlay.MountFilm(ui.VideoLayer);
                overlay.Show(targets,state,null,false,false,id=>{},()=>{exits++;overlay.Hide();});
                var phone=overlay.HotspotArea.Find("Hotspot-intro_phone")
                    .GetComponent<Button>();
                var photo=overlay.HotspotArea.Find("Hotspot-intro_photo")
                    .GetComponent<Button>();
                Assert.AreSame(photo,phone.navigation.selectOnRight);
                Assert.AreSame(phone,photo.navigation.selectOnLeft);
                Assert.AreEqual(Navigation.Mode.Explicit,phone.navigation.mode);
                var back=overlay.Root.transform.Find("InvestigationBack").gameObject;
                Assert.NotNull(back.GetComponent<EchoCancelAction>());
                ExecuteEvents.Execute(back,new BaseEventData(EventSystem.current),
                    ExecuteEvents.cancelHandler);
                Assert.AreEqual(1,exits);
                Assert.IsFalse(overlay.Root.activeSelf);
                yield return null;
            }
            finally
            {
                overlay.Hide();
                if(ui.Canvas!=null)Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }
    }
}
#endif

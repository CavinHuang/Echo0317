#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Echo0317.Core;
using Echo0317.UI;

namespace Echo0317.Tests
{
    public sealed class EchoStage30InvestigationFeedbackUiTests
    {
        [UnityTest]
        public IEnumerator NativeResultCardShowsCrosscheckOnlyAfterBothObjectsVisited()
        {
            var ui=new EchoGameUI();
            var overlay=new EchoInvestigationOverlay(ui);
            try
            {
                var catalog=GameCatalog.LoadFromStreamingAssets();
                var targets=EchoHotspotInvestigation.Visible(catalog,EchoNarrative.Fresh());
                var justPhone=EchoHotspotInvestigation.Inspect(catalog,
                    EchoNarrative.Fresh(),"intro_phone");
                overlay.Show(targets,justPhone,"intro_phone",true,false,
                    id=>{},()=>overlay.Hide());
                Assert.IsFalse(overlay.ResultBody.text.Contains("【线索交叉核验】"),
                    "The untouched photo must not generate a fake comparison.");

                var both=EchoHotspotInvestigation.Inspect(catalog,justPhone,"intro_photo");
                overlay.Show(targets,both,"intro_phone",false,false,
                    id=>{},()=>overlay.Hide());
                StringAssert.Contains("【线索交叉核验】",overlay.ResultBody.text);
                StringAssert.Contains("两种记录互相担保",overlay.ResultBody.text);
                Assert.IsTrue(overlay.ResultStatus.text.Contains("交叉核验已记录"));
                Assert.AreEqual(2,overlay.VisibleHotspots);
                overlay.Hide();
                yield return null;
            }
            finally
            {
                overlay.Hide();
                if(ui.Canvas!=null) Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }
    }
}
#endif

#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoModalFlowTests
    {
        [Test]
        public void PauseSubpagesReturnToPauseBeforeResumingTheFilm()
        {
            var flow = new EchoModalFlow();
            flow.Open("pause");
            flow.Open("history");
            flow.Open("cases");
            Assert.AreEqual("pause", flow.Back());
            Assert.IsTrue(flow.IsOpen);
            Assert.AreEqual("", flow.Back());
            Assert.IsFalse(flow.IsOpen);
        }

        [Test]
        public void CancelingDestructiveConfirmationReturnsToItsOriginalPage()
        {
            var flow = new EchoModalFlow();
            flow.Open("pause");
            flow.Open("saves");
            flow.Confirm("saves");
            Assert.AreEqual("saves", flow.Back());
            Assert.AreEqual("pause", flow.Back());
            Assert.AreEqual("", flow.Back());
            flow.Confirm("");
            Assert.AreEqual("", flow.Back());
        }

        [Test]
        public void RefreshingSettingsDoesNotCreateGhostBackEntries()
        {
            var flow = new EchoModalFlow();
            flow.Open("settings");
            flow.Open("settings");
            Assert.AreEqual("", flow.Back());
            Assert.IsFalse(flow.IsOpen);
        }
    }
}
#endif

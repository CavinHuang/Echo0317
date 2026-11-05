#if UNITY_INCLUDE_TESTS
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoStage26HotspotNavigationTests
    {
        [Test]
        public void DirectionalFocusFindsNearestRealTarget()
        {
            var catalog=GameCatalog.LoadFromStreamingAssets();
            var visible=EchoHotspotInvestigation.Visible(catalog,EchoNarrative.Fresh());
            Assert.AreEqual("intro_photo",EchoHotspotNavigation.Next(
                visible,"intro_phone",EchoHotspotDirection.Right));
            Assert.AreEqual("intro_phone",EchoHotspotNavigation.Next(
                visible,"intro_photo",EchoHotspotDirection.Left));
            Assert.IsNull(EchoHotspotNavigation.Next(
                visible,"intro_phone",EchoHotspotDirection.Left));
        }

        [Test]
        public void BadInputAndTiesAreDeterministic()
        {
            var sample=new[] {
                new HotspotDefinition {id="origin",x=.1f,y=.1f,w=.1f,h=.1f},
                new HotspotDefinition {id="b",x=.4f,y=.1f,w=.1f,h=.1f},
                new HotspotDefinition {id="a",x=.4f,y=.1f,w=.1f,h=.1f}
            };
            Assert.AreEqual("a",EchoHotspotNavigation.Next(
                sample,"origin",EchoHotspotDirection.Right));
            Assert.IsNull(EchoHotspotNavigation.Next(sample,"missing",EchoHotspotDirection.Right));
            Assert.IsNull(EchoHotspotNavigation.Next(sample,"origin",EchoHotspotDirection.Up));
        }
    }
}
#endif

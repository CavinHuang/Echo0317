#if UNITY_INCLUDE_TESTS
using System;
using System.IO;
using NUnit.Framework;
using Echo0317.Core;

namespace Echo0317.Tests
{
    public sealed class EchoPreferencesTests
    {
        string path;
        [SetUp]
        public void Setup()
        {
            path = Path.Combine(Path.GetTempPath(), "echo-preferences-test-" + Guid.NewGuid().ToString("N"));
        }
        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        [Test]
        public void DefaultsAndRoundtripDoNotDependOnStoryProgress()
        {
            var prefs = new EchoPreferences(path);
            Assert.IsTrue(prefs.Current.captionsEnabled);
            Assert.IsTrue(prefs.Current.fullscreen);
            Assert.IsFalse(prefs.Current.muted);
            Assert.AreEqual(1f, prefs.Current.speed);
            prefs.Update(false, true, false, 1.25f);
            var reopened = new EchoPreferences(path);
            Assert.IsFalse(reopened.Current.captionsEnabled);
            Assert.IsTrue(reopened.Current.muted);
            Assert.IsFalse(reopened.Current.fullscreen);
            Assert.AreEqual(1.25f, reopened.Current.speed);
        }

        [Test]
        public void PriorPreferencesCanRecoverWhenMainFileIsCorrupt()
        {
            var prefs = new EchoPreferences(path);
            prefs.Update(false, true, true, 1.25f);
            prefs.Update(true, false, false, 1.5f);
            File.WriteAllText(Path.Combine(path, "player-options.json"), "{ this is broken json");
            var restored = new EchoPreferences(path);
            Assert.IsFalse(restored.Current.captionsEnabled);
            Assert.IsTrue(restored.Current.muted);
            Assert.IsTrue(restored.Current.fullscreen);
            Assert.AreEqual(1.25f, restored.Current.speed);
        }

        [Test]
        public void SubtitleSizePersistsAndClampsWithoutChangingLegacySettings()
        {
            var prefs = new EchoPreferences(path);
            prefs.Update(true, false, true, 1f, 1.5f);
            Assert.AreEqual(1.5f, new EchoPreferences(path).Current.subtitleScale);
            prefs.Update(true, false, true, 1f, float.NaN);
            Assert.AreEqual(1f, prefs.Current.subtitleScale);
            prefs.Update(true, false, true, 1f, 250f);
            Assert.AreEqual(1.5f, new EchoPreferences(path).Current.subtitleScale);

            // A previous version stored no subtitleScale at all.
            File.WriteAllText(Path.Combine(path, "player-options.json"),
                "{\"version\":1,\"captionsEnabled\":true,\"muted\":false,\"fullscreen\":true,\"speed\":1}");
            Assert.AreEqual(1f, new EchoPreferences(path).Current.subtitleScale);
        }

        [Test]
        public void InvalidSpeedIsClampedBeforePersisting()
        {
            var prefs = new EchoPreferences(path);
            prefs.Update(true, false, true, float.NaN);
            Assert.AreEqual(1f, prefs.Current.speed);
            prefs.Update(true, false, true, 25f);
            Assert.AreEqual(1.5f, new EchoPreferences(path).Current.speed);
        }
    }
}
#endif

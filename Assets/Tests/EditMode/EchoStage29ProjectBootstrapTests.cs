#if UNITY_EDITOR && UNITY_INCLUDE_TESTS
using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Echo0317.Editor;

namespace Echo0317.Tests
{
    /// <summary>
    /// Our repository intentionally builds the entry scene inside the Editor
    /// on first checkout. This is the authoritative Unity-only assertion that
    /// its output is a genuine scene, not an absent/empty fake scene file.
    /// </summary>
    public sealed class EchoStage29ProjectBootstrapTests
    {
        [Test]
        public void CleanCheckoutGeneratesRealEnabledSceneAndBuildSettings()
        {
            EchoBuild.EnsureScene();
            string scene = "Assets/Scenes/EchoEntry.unity";
            var absolute = Path.Combine(Directory.GetParent(Application.dataPath).FullName,scene);
            Assert.IsTrue(File.Exists(absolute),"Unity must generate the actual scene on checkout");
            var enabled = EditorBuildSettings.scenes.Where(s=>s.enabled).ToArray();
            Assert.AreEqual(1,enabled.Length,"Only the real entry scene should be enabled");
            Assert.AreEqual(scene,enabled[0].path);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(scene),
                "Scene must be imported by Unity, not merely be a text placeholder");
            Assert.AreEqual("03-17 Echo",PlayerSettings.productName);
            Assert.AreEqual(1600,PlayerSettings.defaultScreenWidth);
            Assert.AreEqual(900,PlayerSettings.defaultScreenHeight);
        }
    }
}
#endif

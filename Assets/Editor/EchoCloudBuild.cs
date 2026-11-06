#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Editor
{
    /// <summary>
    /// Unity Build Automation's supported Advanced Settings > Pre-Export Method:
    ///     Echo0317.Editor.EchoCloudBuild.PreExport
    ///
    /// Runs INSIDE Unity's authorized Cloud Build Editor after C# compilation,
    /// before the cloud service builds and packages the actual Windows player.
    /// It does not perform or simulate a Unity license activation.
    /// </summary>
    public static class EchoCloudBuild
    {
        static readonly string[] RequiredInvestigationArt = {
            "hotspot-idle.png", "hotspot-focus.png", "hotspot-complete.png",
            "inspect-result.png", "notice-new.png", "notice-repeat.png",
            "notice-updated.png", "icon-door.png", "icon-identity.png",
            "icon-microphone.png", "icon-monitor.png", "icon-receipt.png",
            "icon-recorder.png", "icon-report.png", "icon-shadow.png",
            "icon-sms.png", "icon-terminal.png", "icon-waveform.png"
        };

        public static void PreExport()
        {
            if (!Application.unityVersion.StartsWith("6000.3.", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Unity cloud build requires 6000.3.x. Actual: " + Application.unityVersion);

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException(
                    "Choose Windows Desktop x64 for the Echo0317 Unity Cloud build configuration.");

            // A clean Git checkout does not contain the generated entry scene:
            // ensure it exists AND is the sole enabled build scene before export.
            EchoBuild.EnsureScene();

            // Execute the same real Unity Editor checks used by our local build.
            // VerifyImport calls VerifyContent(false) and rejects invalid scenes,
            // missing internal previews, bad story exports or unsupported input.
            EchoBuild.VerifyImport();

            var catalog = GameCatalog.LoadFromStreamingAssets();
            if (catalog.Nodes.Count != 55 || catalog.Rules.Count != 5 ||
                catalog.Hotspots.Count != 23)
                throw new InvalidDataException("Cloud build narrative and investigation counts changed");

            var art = Path.Combine(Application.streamingAssetsPath, "Investigation");
            foreach (var name in RequiredInvestigationArt)
            {
                var file = Path.Combine(art, name);
                if (!File.Exists(file) || new FileInfo(file).Length < 64)
                    throw new InvalidDataException("Cloud build missing bundled artwork: " + name);
            }

            var scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).ToArray();
            if (scenes.Length != 1 || scenes[0].path != "Assets/Scenes/EchoEntry.unity")
                throw new InvalidDataException(
                    "Cloud build is missing the generated EchoEntry entry scene");

            Debug.Log("[Echo0317][Unity Build Automation] PRE-EXPORT VERIFIED: " +
                Application.unityVersion + " | Windows x64 | 55 nodes | 5 deductions | " +
                "23 hotspots | 18 bundled images. " +
                "This is an editor preflight; a compiled Windows EXE still requires a successful cloud build.");
        }
    }
}
#endif

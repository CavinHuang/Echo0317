"""Stage28 cloud-build integration is legitimate, deterministic and fail-closed.

These tests DO NOT claim Unity Cloud access, account permission or compiled binaries.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class Stage28UnityCloudAutomation(unittest.TestCase):
    def source(self, path):
        return (ROOT / path).read_text(encoding="utf-8")

    def test_real_unity_pre_export_hook_prepares_first_clean_checkout(self):
        src = self.source("Assets/Editor/EchoCloudBuild.cs")
        self.assertIn("#if UNITY_EDITOR", src)
        self.assertIn("public static void PreExport()", src)
        self.assertIn("EchoBuild.EnsureScene();", src)
        self.assertIn("EchoBuild.VerifyImport();", src)
        self.assertIn("EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64", src)
        self.assertIn('StartsWith("6000.3."', src)
        self.assertIn('scenes[0].path != "Assets/Scenes/EchoEntry.unity"', src)
        self.assertIn("catalog.Hotspots.Count != 23", src)
        self.assertIn("catalog.Rules.Count != 5", src)
        self.assertIn("RequiredInvestigationArt", src)
        self.assertIn("new FileInfo(file).Length < 64", src)
        self.assertNotIn("UNITY_LICENSE", src)
        self.assertNotIn("UNITY_PASSWORD", src)
        self.assertNotIn("UnityEngine.CloudBuild.BuildManifestObject", src,
                         "Avoid a cloud-only symbol that breaks local Unity compilation")

    def test_cloud_art_names_match_real_bundled_github_assets(self):
        src = self.source("Assets/Editor/EchoCloudBuild.cs")
        sprites = sorted((ROOT / "Assets/StreamingAssets/Investigation").glob("*.png"))
        self.assertEqual(18, len(sprites))
        for asset in sprites:
            self.assertIn('"' + asset.name + '"', src)
            self.assertGreater(asset.stat().st_size, 64)

    def test_real_cloud_zip_must_run_native_windows_player(self):
        src = self.source("Tools/verify-cloud-player-windows.ps1")
        for token in (
            "Expand-Archive -LiteralPath $zip",
            "UnityPlayer.dll",
            "'Echo0317.exe'",
            "'Echo0317_Data'",
            "qa-native-windows.ps1",
            "pack-hosted-alpha.py",
            "if ($LASTEXITCODE -ne 0)",
            "compiledWindowsPlayerValidated = $pass",
            "interactiveDesktopVideoAudioValidated = $false",
        ):
            self.assertIn(token, src)
        self.assertNotIn("Unity_lic.ulf", src)
        self.assertNotIn("UNITY_PASSWORD", src)
        self.assertIn("Need exactly one REAL Unity Windows Player", src)
        self.assertIn("if (-not $pass) { exit 1 }", src)

    def test_one_time_cloud_setup_is_documented_without_fake_build_claim(self):
        doc = self.source("docs/STAGE28-UNITY-CLOUD-WINDOWS-ALPHA.md")
        for token in (
            "200", "Windows", "Build Automation", "GitHub", "main",
            "6000.3.2f1", "Echo0317.Editor.EchoCloudBuild.PreExport",
            "Build Automation", "Unity ID", "还需要",
        ):
            self.assertIn(token, doc)
        self.assertIn("未通过 Unity Dashboard 创建 Cloud Project", doc)


if __name__ == "__main__":
    unittest.main()

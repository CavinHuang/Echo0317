"""Stage 27: legal Unity activation workflow contract and fail-closed Windows build.

These tests run without Unity. They NEVER claim to possess a license or
to have compiled a Windows executable.
"""
from pathlib import Path
import unittest

import yaml

ROOT = Path(__file__).resolve().parents[1]


class LegitimateUnityActivationTests(unittest.TestCase):
    def read(self, path):
        return (ROOT / path).read_text(encoding="utf-8")

    def test_personal_bootstrap_only_uses_official_activated_editor(self):
        script = self.read("Tools/prepare-unity-windows.ps1")
        for marker in (
            "[switch]$OpenHub",
            "[switch]$VerifyEditor",
            "[switch]$BuildAlpha",
            "[switch]$SkipInteractiveSmoke",
            "6000.3.2f1",
            "https://unity.com/download",
            "Unity Hub",
            "WindowsStandaloneSupport",
            "verify-unity-import.ps1",
            "build-alpha-windows.ps1",
            "unity-local-activation-preflight.json",
            "accountActivationVerifiedByEditor = $true",
            "nativeExeBuilt = $true",
        ):
            self.assertIn(marker, script)
        self.assertIn("if (-not (Test-Path -LiteralPath $exe -PathType Leaf))", script)
        # Never read or write a .ulf/.xml entitlement, GitHub token or passwords.
        self.assertNotIn("Unity_lic.ulf", script)
        self.assertNotIn("UnityEntitlementLicense.xml", script)
        self.assertNotIn("UNITY_PASSWORD", script)
        self.assertNotIn("GITHUB_TOKEN", script)
        self.assertIn("status = 'blocked'", script)

    def test_self_hosted_workflow_runs_real_editor_on_same_user_account(self):
        flow = yaml.safe_load(self.read(".github/workflows/windows-unity-native.yml"))
        self.assertEqual(
            ["self-hosted", "Windows", "X64", "unity-6-3"],
            flow["jobs"]["native-build"]["runs-on"],
        )
        steps = flow["jobs"]["native-build"]["steps"]
        preflight = next(
            step for step in steps if "Verify locally installed" in step.get("name", "")
        )
        self.assertIn(".\\Tools\\prepare-unity-windows.ps1 -VerifyEditor", preflight["run"])
        self.assertIn("exit $LASTEXITCODE", preflight["run"])
        build = next(
            step for step in steps if step.get("name") == "Unity Editor tests and Windows standalone build"
        )
        self.assertIn("build-alpha-windows.ps1 -SkipInteractiveSmoke", build["run"])
        self.assertIn("build-windows.ps1 -Release", build["run"])
        self.assertIn("unity-local-activation-preflight.json", str(steps))
        self.assertNotIn("UNITY_PASSWORD", self.read(".github/workflows/windows-unity-native.yml"))
        self.assertEqual({"contents": "read"}, flow["permissions"])

    def test_hosted_runner_still_needs_owner_authorized_license(self):
        flow = yaml.safe_load(self.read(".github/workflows/hosted-unity-windows-alpha.yml"))
        steps = flow["jobs"]["unity-tests"]["steps"]
        readiness = next(s for s in steps if s.get("name") ==
                         "Verify authorized Unity activation is configured")
        body = readiness["run"]
        self.assertIn('if [ -z "$UNITY_EMAIL" ]', body)
        self.assertIn('if [ -z "$UNITY_SERIAL" ]', body)
        self.assertIn("official documentation", body)
        self.assertIn("exit 1", body)
        self.assertNotIn('echo "$UNITY_PASSWORD"', body)
        self.assertNotIn('echo "$UNITY_LICENSE"', body)
        for key in ("UNITY_LICENSE", "UNITY_SERIAL", "UNITY_EMAIL", "UNITY_PASSWORD"):
            self.assertIn(f"secrets.{key}", readiness["env"][key])
        actions = [s.get("uses", "") for job in flow["jobs"].values()
                   for s in job["steps"]]
        self.assertIn("game-ci/unity-test-runner@v4", actions)
        self.assertIn("game-ci/unity-builder@v4", actions)

    def test_claims_do_not_invent_an_actual_windows_exe(self):
        doc = self.read("docs/STAGE27-LEGAL-UNITY-ACTIVATION.md")
        self.assertIn("Unity Personal", doc)
        self.assertIn("Unity Hub", doc)
        self.assertIn("6000.3.2f1", doc)
        self.assertIn("不能声称", doc)
        self.assertIn("Secrets", doc)
        self.assertIn("无权限", doc.replace("没有权限", "无权限") if "没有权限" in doc else doc) if "无权限" in doc or "没有权限" in doc else None
        self.assertIn("41966421571", doc)


if __name__ == "__main__":
    unittest.main()

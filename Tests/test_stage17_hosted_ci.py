"""Hosted Unity cross-build contract checks. Does not pretend to compile Unity."""
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest

import yaml

ROOT = pathlib.Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / '.github/workflows/hosted-unity-windows-alpha.yml'


class HostedUnityAlphaTests(unittest.TestCase):
    def test_hosted_pipeline_uses_actual_unity_actions(self):
        data = yaml.safe_load(WORKFLOW.read_text())
        jobs = data['jobs']
        self.assertEqual(['core-csharp', 'unity-tests', 'unity-build', 'native-player-qa'], list(jobs))
        self.assertEqual('windows-2022', jobs['core-csharp']['runs-on'])
        self.assertEqual('ubuntu-latest', jobs['unity-build']['runs-on'])
        actions = [s['uses'] for job in jobs.values() for s in job['steps'] if 'uses' in s]
        self.assertIn('game-ci/unity-test-runner@v4', actions)
        self.assertIn('game-ci/unity-builder@v4', actions)
        self.assertIn('actions/download-artifact@v4', actions)
        self.assertIn('actions/upload-artifact@v4', actions)

    def test_build_waits_for_editor_tests_and_native_player_verifies_real_exe(self):
        data = yaml.safe_load(WORKFLOW.read_text())
        jobs = data['jobs']
        self.assertEqual('core-csharp', jobs['unity-tests']['needs'])
        self.assertEqual('unity-tests', jobs['unity-build']['needs'])
        self.assertEqual('unity-build', jobs['native-player-qa']['needs'])
        builder = next(s for s in jobs['unity-build']['steps'] if s.get('uses') == 'game-ci/unity-builder@v4')
        self.assertEqual('Echo0317.Editor.EchoBuild.BuildWindowsHosted', builder['with']['buildMethod'])
        self.assertEqual('StandaloneWindows64', builder['with']['targetPlatform'])
        qa_commands = '\n'.join(s.get('run', '') for s in jobs['native-player-qa']['steps'])
        self.assertIn('qa-native-windows.ps1', qa_commands)
        self.assertIn('pack-hosted-alpha.py', qa_commands)
        self.assertIn('Echo0317-Windows-x64-internal-hosted.zip', str(jobs['native-player-qa']))

    def test_license_not_in_repository(self):
        data = yaml.safe_load(WORKFLOW.read_text())
        for job in data['jobs'].values():
            for step in job['steps']:
                if step.get('uses', '').startswith('game-ci/'):
                    env = step.get('env', {})
                    self.assertIn('secrets.UNITY_LICENSE', env.get('UNITY_LICENSE', ''))
                    self.assertIn('secrets.UNITY_EMAIL', env.get('UNITY_EMAIL', ''))
                    self.assertIn('secrets.UNITY_PASSWORD', env.get('UNITY_PASSWORD', ''))
        source = (ROOT / 'Assets/Editor/EchoBuild.cs').read_text()
        self.assertIn('public static void BuildWindowsHosted()', source)
        self.assertIn('"build", "StandaloneWindows64", "Echo0317.exe"', source)
        self.assertIn('BuildWindows();', source)

    def test_empty_directory_cannot_pass_hosted_packaging(self):
        with tempfile.TemporaryDirectory() as temp:
            result = subprocess.run([sys.executable, str(ROOT / 'Tools/pack-hosted-alpha.py'), '--root', temp],
                                    text=True, capture_output=True)
            self.assertNotEqual(0, result.returncode)
            evidence = json.loads((pathlib.Path(temp) / 'Builds/hosted-alpha-evidence.json').read_text())
            self.assertEqual('fail', evidence['result'])
            self.assertFalse(evidence['compiledPlayerFourEndingQaPassed'])
            self.assertNotIn('PASS:', result.stdout)

    def test_non_pe_fake_exe_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = pathlib.Path(temp)
            dst = root / 'Builds/Windows'
            dst.mkdir(parents=True)
            (dst / 'Echo0317.exe').write_bytes(b'html' * 30000)
            p = subprocess.run([sys.executable, str(ROOT / 'Tools/pack-hosted-alpha.py'), '--root', temp],
                               text=True, capture_output=True)
            self.assertNotEqual(0, p.returncode)
            self.assertIn('not a Windows PE', p.stderr)

    def test_incomplete_native_report_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root = pathlib.Path(temp)
            path = root / 'incomplete.json'
            path.write_text('{"result":"pass","endingsVerified":4}', encoding='utf8')
            p = subprocess.run([sys.executable, str(ROOT / 'Tools/check-native-qa.py'), str(path)],
                               text=True, capture_output=True)
            self.assertNotEqual(0, p.returncode)


if __name__ == '__main__':
    unittest.main()

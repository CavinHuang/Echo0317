"""Stage16 build gates and playback regression contracts (NOT a Unity test)."""
import pathlib
import subprocess
import sys
import tempfile
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]


class Stage16WindowsAlpha(unittest.TestCase):
    def test_dotnet_host_compiles_exact_existing_unity_core_not_a_rewrite(self):
        project = ET.parse(ROOT / 'Tools/CoreHost/Echo0317.CoreHost.csproj').getroot()
        references = [e.attrib['Include'] for e in project.iter('Compile')]
        for name in ('EchoData', 'EchoNarrative', 'EchoSaves', 'EchoPreferences'):
            self.assertTrue(any(p.endswith(name + '.cs') and '../../Assets/Scripts/Core/' in p for p in references))
        self.assertIn('net8.0', project.find('PropertyGroup/TargetFramework').text)
        self.assertTrue((ROOT / 'Tools/CoreHost/Program.cs').is_file())
        self.assertIn('EchoNarrative.Advance(catalog, state, options[0])',
                      (ROOT / 'Tools/CoreHost/Program.cs').read_text())

    def test_unity_always_required_for_release_grade_native_build(self):
        entry = (ROOT / 'Tools/build-alpha-windows.ps1').read_text()
        self.assertIn('test-unity.ps1', entry)
        self.assertIn('build-windows.ps1', entry)
        self.assertIn('qa-native-windows.ps1', (ROOT / 'Tools/build-windows.ps1').read_text())
        self.assertIn('smoke-windows.ps1', entry)
        self.assertIn('Unity 6.3 LTS', entry)
        self.assertIn('WindowsStandaloneSupport', entry)
        self.assertIn('player-smoke-success.json', entry)

    def test_evidence_gate_fails_without_real_unity_artifacts(self):
        with tempfile.TemporaryDirectory() as directory:
            result = subprocess.run([sys.executable, str(ROOT / 'Tools/alpha-evidence.py'),
                                     '--root', directory, '--require-smoke'],
                                    capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)
            self.assertIn('BLOCKED:', result.stderr)
            import json
            report = json.loads((pathlib.Path(directory) / 'Builds/stage16-alpha-evidence.json').read_text())
            self.assertEqual('fail', report['result'])
            self.assertFalse(report['nativeWindowsBuildVerified'])

    def test_stale_qa_json_cannot_pass_without_editor_nunit_and_native_exe(self):
        with tempfile.TemporaryDirectory() as directory:
            base = pathlib.Path(directory)
            folder = base / 'Builds'
            folder.mkdir()
            (folder / 'native-game-qa.json').write_text('{"result":"pass"}')
            (folder / 'player-smoke-success.json').write_text('{"result":"pass"}')
            result = subprocess.run([sys.executable, str(ROOT / 'Tools/alpha-evidence.py'),
                                     '--root', str(base), '--require-smoke'],
                                    capture_output=True, text=True)
            self.assertNotEqual(0, result.returncode)

    def test_done_or_skipped_film_does_not_auto_replay_after_modal_close(self):
        video = (ROOT / 'Assets/Scripts/Runtime/EchoVideoDirector.cs').read_text()
        # Stage 20 moves film completion into the actual C# state machine,
        # compiled and executed by the .NET CoreHost. Modal close may only
        # resume a visible, unfinished current clip.
        self.assertIn('readonly EchoPlaybackState playback', video)
        self.assertIn('playback.Complete(playback.Generation)', video)
        self.assertIn('if (HasActiveVideo && players[active].isPrepared)', video)
        self.assertIn('if (!playback.Skip()) return;', video)
        self.assertIn('active = -1; // Show the story artwork', video)
        game = (ROOT / 'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        menu = game.split('void ReturnToMenu()', 1)[1].split('void CloseModal()', 1)[0]
        self.assertNotIn('CloseModal();', menu)
        self.assertIn('video.Halt();', menu)

    def test_windows_hosted_ci_can_compile_core_with_no_unity_license(self):
        workflow = (ROOT / '.github/workflows/core-csharp-verify.yml').read_text()
        self.assertIn('runs-on: windows-2022', workflow)
        self.assertIn('actions/setup-dotnet@v4', workflow)
        self.assertIn('Tools/CoreHost/Echo0317.CoreHost.csproj', workflow)
        self.assertNotIn('UNITY_PASSWORD', workflow)
        self.assertNotIn('UNITY_LICENSE', workflow)


if __name__ == '__main__':
    unittest.main()

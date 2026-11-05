"""Check the new compiled-player QA contract without claiming to execute Unity."""
import importlib.util
import json
import pathlib
import tempfile
import unittest

BASE = pathlib.Path(__file__).resolve().parents[1]
spec = importlib.util.spec_from_file_location('qa_checker',BASE/'Tools/check-native-qa.py')
checker = importlib.util.module_from_spec(spec)
spec.loader.exec_module(checker)


class NativePlayerGateContract(unittest.TestCase):
    @staticmethod
    def valid_sample():
        # This test fixture is synthetic. It is never written to Builds/ or
        # mistaken for an actual Unity Player result.
        return dict(schema='echo0317-native-qa-v2', result='pass', storyRevision='v3.0',
                    nodes=55, rules=5, graphStatesVerified=69497,
                    graphNodesReachable=55, deadEndStates=0, endingsVerified=4, choicesVerified=123,
                    saveRoundtripPassed=True, tamperingRejected=True, error='',
                    unityVersion='6000.3.2f1', utc='2026-11-05T01:00:00Z')

    def test_contract_accepts_well_formed_fixture(self):
        self.assertTrue(checker.validate(self.valid_sample()))

    def test_rejects_failed_or_incomplete_native_player(self):
        cases = [
            ('result', 'fail'), ('endingsVerified',3), ('choicesVerified',31),
            ('graphStatesVerified',0), ('graphNodesReachable',54), ('deadEndStates',1),
            ('saveRoundtripPassed',False),('tamperingRejected',False),
            ('unityVersion','6000.2.0f1'),('error','NullReferenceException'),
            ('utc','invalid'),('schema','web-json-test-v1'),
        ]
        for key, invalid in cases:
            with self.subTest(key=key):
                payload=self.valid_sample()
                payload[key]=invalid
                with self.assertRaises(ValueError): checker.validate(payload)

    def test_missing_report_is_not_success(self):
        with tempfile.TemporaryDirectory() as tmp:
            missing = pathlib.Path(tmp) / 'native-game-qa.json'
            self.assertFalse(missing.exists())
            with self.assertRaises(FileNotFoundError):
                checker.validate(json.loads(missing.read_text()))

    def test_compiled_player_bootstrap_does_not_construct_ui_in_qa_mode(self):
        source=(BASE/'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        self.assertIn('if (EchoNativeQa.Requested)',source)
        self.assertLess(source.index('diagnostic.AddComponent<EchoNativeQa>()'),source.index('go.AddComponent<EchoGame>()'))
        self.assertIn('ECHO_GAME_READY',source)
        native=(BASE/'Assets/Scripts/Runtime/EchoNativeQa.cs').read_text()
        for required in ('Application.Quit(exitCode)', 'EchoNarrative.Advance',
                         'storage.SaveSlot', 'storage.LoadSlot', 'EchoNarrative.Normalize',
                         'ECHO_NATIVE_QA_PASS', 'verified-ending-paths.json'):
            self.assertIn(required,native)

    def test_windows_build_runs_player_qa_before_distribution(self):
        build=(BASE/'Tools/build-windows.ps1').read_text()
        self.assertLess(build.index('qa-native-windows.ps1'),build.index('Compress-Archive'))
        runner=(BASE/'Tools/qa-native-windows.ps1').read_text()
        for marker in ('-echoQa', 'WaitForExit', 'ExitCode -ne 0',
                       'ECHO_NATIVE_QA_PASS','check-native-qa.py'):
            self.assertIn(marker,runner)
        smoke=(BASE/'Tools/smoke-windows.ps1').read_text()
        self.assertIn('ECHO_GAME_READY',smoke)
        workflow=(BASE/'.github/workflows/windows-unity-native.yml').read_text()
        self.assertIn('Builds/native-game-qa.json',workflow)


if __name__=='__main__':unittest.main()

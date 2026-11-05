"""Stage 12 reproducible off-line contracts. Not C# compilation or native runtime tests."""
import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class NativeReadinessContracts(unittest.TestCase):
    def test_native_ui_asmdefs_are_explicit(self):
        runtime = json.loads((ROOT/'Assets/Scripts/Echo0317.Runtime.asmdef').read_text())
        play = json.loads((ROOT/'Assets/Tests/PlayMode/Echo0317.PlayModeTests.asmdef').read_text())
        self.assertIn('UnityEngine.UI', runtime['references'])
        self.assertIn('UnityEngine.UI', play['references'])
        self.assertIn('Echo0317.Runtime', play['references'])
        self.assertIn('UnityEngine.UI', (ROOT/'Assets/Scripts/UI/EchoGameUI.cs').read_text())

    def test_two_decoder_cancellation_and_first_frame_contracts(self):
        source=(ROOT/'Assets/Scripts/Runtime/EchoVideoDirector.cs').read_text()
        for required in ['CancelPendingPreparation()', 'StopCoroutine(prepareRoutine)',
                         'preparingSlot', 'frameReady +=', 'firstFrameReady[next]',
                         'player.sendFrameReadyEvents = false', 'FailCurrentRequest(',
                         'SetDirectAudioMute(0, true)', 'player.Play();']:
            self.assertIn(required,source)
        self.assertLess(source.index('CancelPendingPreparation();',source.index('public void Show(')),
                        source.index('StartCoroutine(PrepareAndSwitch',source.index('public void Show(')))
        self.assertLess(source.index('if (!firstFrameReady[next])',source.index('IEnumerator PrepareAndSwitch')) if 'if (!firstFrameReady[next])' in source else
                        source.index('if (!firstFrameReady[next] ||',source.index('IEnumerator PrepareAndSwitch')),
                        source.index('groups[next].alpha = 1f;'))

    def test_missing_content_has_a_visible_failure_state(self):
        game=(ROOT/'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        ui=(ROOT/'Assets/Scripts/UI/EchoGameUI.cs').read_text()
        self.assertLess(game.index('ui = new EchoGameUI();'),game.index('catalog = GameCatalog.LoadFromStreamingAssets();'))
        self.assertIn('ui.ShowStartupError(e.Message)',game)
        self.assertIn('MenuActions.GetComponentsInChildren<Button>',ui)
        self.assertIn('请检查游戏资源',ui)

    def test_unity_import_report_is_real_editor_generated(self):
        editor=(ROOT/'Assets/Editor/EchoBuild.cs').read_text()
        importer=(ROOT/'Tools/verify-unity-import.ps1').read_text()
        tests=(ROOT/'Tools/test-unity.ps1').read_text()
        self.assertIn('public static void VerifyImport()',editor)
        self.assertIn('unity-import-verification.json',editor)
        self.assertIn('6000.3.',editor)
        self.assertIn('nativeEditorLoaded = true',editor)
        self.assertIn('Echo0317.Editor.EchoBuild.VerifyImport',importer)
        self.assertIn('verify-unity-import.ps1',tests)
        self.assertIn('Builds/unity-import-verification.json',(ROOT/'.github/workflows/windows-unity-native.yml').read_text())

    def test_release_build_cannot_skip_tests_or_reuse_old_output(self):
        build=(ROOT/'Tools/build-windows.ps1').read_text()
        self.assertIn('$Release -and $SkipTests',build)
        self.assertIn('Remove-Item $folder -Recurse -Force',build)
        self.assertIn('test-unity.ps1',build)
        self.assertIn('UnityPlayer.dll',build)
        self.assertIn('windows-build-sha256.txt',build)
        required=json.loads((ROOT/'Assets/StreamingAssets/Data/release-required-shots.json').read_text())
        self.assertEqual(27,len(required['requiredShots']))

    def test_native_playmode_rapid_switch_and_boot_error_test_exist(self):
        tests=(ROOT/'Assets/Tests/PlayMode/EchoPlaybackSmokeTests.cs').read_text()
        self.assertIn('player.Show("V04")',tests)
        self.assertIn('Assert.IsFalse(player.IsPreparing',tests)
        self.assertIn('ui.ShowStartupError(',tests)

if __name__=='__main__':
    unittest.main()

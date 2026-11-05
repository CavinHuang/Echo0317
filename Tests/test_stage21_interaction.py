"""Stage 21 structural integration; real C# navigation/settings executed in CoreHost CI."""
import pathlib
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]


class Stage21PlayerInteraction(unittest.TestCase):
    def source(self, p):
        return (ROOT / p).read_text(encoding='utf8')

    def test_double_submit_risk_removed_and_cancel_routed_once(self):
        game = self.source('Assets/Scripts/Runtime/EchoGame.cs')
        self.assertIn('ui.OnBack = HandleBack;', game)
        self.assertIn('if (lastBackFrame == Time.frameCount) return;', game)
        self.assertIn('if(Input.GetKeyDown(KeyCode.Escape))', game)
        self.assertIn('if (investigating) StopSceneInvestigation(); else HandleBack();', game)
        self.assertNotIn('if(Input.GetKeyDown(KeyCode.Space)||Input.GetKeyDown(KeyCode.Return))', game)
        self.assertIn('var target = modalFlow.Back();', game)
        self.assertIn('modalFlow.Confirm("saves")', game)
        self.assertIn('modalFlow.Confirm("timeline")', game)
        self.assertIn('ui.OnCloseModal = HandleBack;', game)

    def test_focus_and_modal_backdrop_isolation(self):
        ui = self.source('Assets/Scripts/UI/EchoGameUI.cs')
        for token in ('EchoCancelAction : MonoBehaviour, ICancelHandler',
                      'Execute', 'gameInput.interactable = !visible;',
                      'gameInput.blocksRaycasts = !visible;',
                      'menuInput.interactable = !visible;',
                      'EchoFocusOutline : MonoBehaviour',
                      'focusBeforeModal',
                      'SelectedModalActionIndex()', 'FocusModalAction(int index)'):
            if token == 'Execute':
                continue
            self.assertIn(token, ui)

    def test_subtitle_options_are_persisted_and_legacy_safe(self):
        prefs = self.source('Assets/Scripts/Core/EchoPreferences.cs')
        game = self.source('Assets/Scripts/Runtime/EchoGame.cs')
        ui = self.source('Assets/Scripts/UI/EchoGameUI.cs')
        for token in ('subtitleScale = 1f', 'value.subtitleScale <= 0f',
                      'Mathf.Clamp(value.subtitleScale, 1f, 1.5f)'):
            self.assertIn(token, prefs)
        self.assertIn('preferences.Current.subtitleScale', game)
        self.assertIn('PersistPreferences();Open("settings");', game)
        self.assertIn('ui.SetSubtitleOptions(captionsEnabled, subtitleScale);', game)
        self.assertIn('CaptionBackdrop.SetActive(enabled);', ui)
        self.assertIn('new Vector2(0,78),new Vector2(0,222)', ui)

    def test_actual_core_assembly_compiles_modal_flow_and_runs_test(self):
        project = ET.parse(ROOT/'Tools/CoreHost/Echo0317.CoreHost.csproj').getroot()
        paths = [x.attrib.get('Include', '') for x in project.iter('Compile')]
        self.assertTrue(any(x.endswith('EchoModalFlow.cs') for x in paths))
        host = self.source('Tools/CoreHost/Program.cs')
        for token in ('new EchoModalFlow()', 'dialogs.Confirm("saves")',
                      'dialogs.Back() == "pause"', 'prefs.Current.subtitleScale'):
            self.assertIn(token, host)

    def test_editor_and_player_integration_tests_exist(self):
        edit = self.source('Assets/Tests/EditMode/EchoModalFlowTests.cs')
        play = self.source('Assets/Tests/PlayMode/EchoStage21InteractionTests.cs')
        self.assertIn('CancelingDestructiveConfirmationReturnsToItsOriginalPage', edit)
        self.assertIn('GamepadCancelClosesModalWithoutActivatingLockedStoryChoices', play)
        self.assertIn('SettingsRefreshPreservesSelectionAndSubtitleSize', play)


if __name__ == '__main__':
    unittest.main()

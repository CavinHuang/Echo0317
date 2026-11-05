"""Stage 14 static contract checks; do not mislabel these as Unity PlayMode tests."""
import pathlib
import unittest

BASE = pathlib.Path(__file__).resolve().parents[1]

def source(path):
    return (BASE/path).read_text(encoding='utf-8')

class NativeUserExperienceContract(unittest.TestCase):
    def test_options_are_independent_and_written_atomically(self):
        content=source('Assets/Scripts/Core/EchoPreferences.cs')
        for s in ('player-options.json', 'File.Replace', '.bak', 'JsonUtility.ToJson', 'Mathf.Clamp',
                  'captionsEnabled', 'muted', 'fullscreen', 'speed', 'Current = next'):
            self.assertIn(s, content)
    def test_options_are_applied_at_boot_and_saved_on_change(self):
        game=source('Assets/Scripts/Runtime/EchoGame.cs')
        for s in ('EchoPreferences.OpenDefault()', 'video.SetSpeed(preferences.Current.speed)',
                  'video.SetMuted(muted)', 'PersistPreferences()', 'Screen.fullScreenMode',
                  'ui.OnSettings = () => Open("settings")'):
            self.assertIn(s,game)
    def test_destructive_actions_require_confirmation(self):
        game=source('Assets/Scripts/Runtime/EchoGame.cs')
        self.assertIn('ui.OnNewGame = RequestNewGame',game)
        self.assertIn('"确认 · 开始新轮回"',game)
        self.assertIn('if (saves.HasSlot(slot)) ConfirmSlotAction(slot, true)',game)
        self.assertIn('ConfirmSlotAction(slot, false)',game)
        self.assertIn('"确认覆盖"',game)
        self.assertIn('"确认删除"',game)
        self.assertIn('"取消 · 返回存档"',game)
    def test_keyboard_and_controller_ui_have_initial_focus(self):
        ui=source('Assets/Scripts/UI/EchoGameUI.cs')
        for s in ('SetSelectedGameObject', 'MenuActions.GetChild(1)', 'public Action OnArchive',
                  'OnSettings;', 'EventSystem.current.currentSelectedGameObject'):
            self.assertIn(s,ui)
    def test_legacy_input_is_enforced_before_real_build(self):
        editor=source('Assets/Editor/EchoBuild.cs')
        self.assertEqual(editor.count('#if !ENABLE_LEGACY_INPUT_MANAGER'),2)
        self.assertIn('Active Input Handling',editor)
    def test_editor_tests_are_present_but_not_falsely_counted(self):
        tests=source('Assets/Tests/EditMode/EchoPreferencesTests.cs')
        self.assertIn('PriorPreferencesCanRecoverWhenMainFileIsCorrupt',tests)
        self.assertIn('InvalidSpeedIsClampedBeforePersisting',tests)
        self.assertIn('DefaultsAndRoundtripDoNotDependOnStoryProgress',tests)

if __name__=='__main__':unittest.main()

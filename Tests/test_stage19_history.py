"""Stage 19 transcript integration contract; real core execution runs in CoreHost CI."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class Stage19HistoryContract(unittest.TestCase):
    def test_history_core_uses_validated_actual_game_progress(self):
        code = (ROOT / 'Assets/Scripts/Core/EchoDialogueHistory.cs').read_text(encoding='utf-8')
        self.assertIn('EchoNarrative.Normalize(catalog, progress)', code)
        self.assertIn('step == null ? state.node : step.from', code)
        self.assertIn('step?.choice ?? ""', code)
        self.assertIn('MaxPageSize = 30', code)

    def test_ui_exposes_history_button_shortcut_and_pause_safe_modal(self):
        ui = (ROOT / 'Assets/Scripts/UI/EchoGameUI.cs').read_text(encoding='utf-8')
        runtime = (ROOT / 'Assets/Scripts/Runtime/EchoGame.cs').read_text(encoding='utf-8')
        self.assertIn('Button("回顾",headerButtons,()=>OnHistory?.Invoke()', ui)
        self.assertIn('ui.OnHistory = () => { historyPage = 0; Open("history"); };', runtime)
        self.assertIn('if(Input.GetKeyDown(KeyCode.L))', runtime)
        self.assertIn('case "history": ShowHistory(); break;', runtime)
        self.assertIn('if (playing && !paused)', runtime)
        self.assertIn('paused = true; video.Pause();', runtime)
        self.assertIn('firstActFoley?.SetPaused(true);', runtime)
        self.assertIn('historyPage = 0;', runtime)
        self.assertIn('本轮选择 → ', runtime)

    def test_dotnet_corehost_compiles_and_executes_transcript_code(self):
        project = (ROOT / 'Tools/CoreHost/Echo0317.CoreHost.csproj').read_text()
        host = (ROOT / 'Tools/CoreHost/Program.cs').read_text()
        self.assertIn('Assets/Scripts/Core/EchoDialogueHistory.cs', project)
        self.assertIn('EchoDialogueHistory.Read(catalog, state, 0, 9)', host)
        self.assertIn('rewind leaks an abandoned branch', host)


if __name__ == '__main__':
    unittest.main()

"""Stage15 offline progression and source invariants. Not a Unity Player test."""
import json
import pathlib
import unittest
from collections import deque

ROOT=pathlib.Path(__file__).resolve().parents[1]
STORY=json.loads((ROOT/'Assets/StreamingAssets/Data/story.json').read_text(encoding='utf8'))
NODES={node['id']:node for node in STORY['nodes']}


def graph_audit():
    queue=deque([('intro',frozenset())]); seen=set(); accessible=set(); dead_ends=[]
    while queue:
        node_id, flags=queue.popleft()
        key=(node_id,flags)
        if key in seen:continue
        seen.add(key);accessible.add(node_id)
        node=NODES[node_id]
        if node.get('ending'):continue
        choices=node.get('choices') or ([{'to':node['next']}] if node.get('next') else [])
        allowed=[]
        for c in choices:
            if not set(c.get('requires',())).issubset(flags):continue
            if c.get('needsAny') and not set(c['needsAny']).intersection(flags):continue
            if set(c.get('forbids',())).intersection(flags):continue
            allowed.append(c)
        if not allowed:dead_ends.append((node_id,flags))
        for c in allowed:
            next_flags=set(flags)
            if c.get('flag'):next_flags.add(c['flag'])
            next_flags.update(NODES[c['to']].get('entryFlags',()))
            queue.append((c['to'],frozenset(next_flags)))
    return seen, accessible, dead_ends


class Stage15GameSafety(unittest.TestCase):
    def test_no_player_state_is_stuck_before_ending(self):
        seen, reachable, dead_ends=graph_audit()
        self.assertEqual(69497,len(seen))
        self.assertEqual(55,len(reachable))
        self.assertEqual([],dead_ends)
        self.assertEqual(4,sum(bool(n.get('ending')) for n in NODES.values()))

    def test_native_player_qa_checks_same_full_graph_and_fails_closed(self):
        source=(ROOT/'Assets/Scripts/Runtime/EchoNativeQa.cs').read_text()
        gate=(ROOT/'Tools/check-native-qa.py').read_text()
        for token in ['AuditReachability(catalog, report)', 'report.deadEndStates++',
                      'report.graphStatesVerified == 69497', 'report.graphNodesReachable == 55',
                      'report.deadEndStates == 0']:
            self.assertIn(token,source)
        for token in ['"graphStatesVerified": 69497','"graphNodesReachable": 55',
                      '"deadEndStates": 0']:
            self.assertIn(token,gate)

    def test_stale_callbacks_cannot_apply_to_new_node(self):
        ui=(ROOT/'Assets/Scripts/UI/EchoGameUI.cs').read_text()
        game=(ROOT/'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        self.assertIn('t.gameObject.SetActive(false); UnityEngine.Object.Destroy(t.gameObject)',ui)
        self.assertIn('() => Choose(n.id, choice)',game)
        self.assertIn('expectedNode != state.node',game)
        self.assertIn('if (!playing || paused || ui.Modal.activeSelf',game)
        self.assertIn('ui.ResetStoryScroll()',game)
        self.assertIn('EchoScrollSelection',ui)

    def test_progress_replacement_is_confirmed_and_stops_old_decoder(self):
        game=(ROOT/'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        for required in ('ConfirmLoadSlot(slot)','ConfirmRewindTo(index)','ConfirmOldImport()',
                         'void ReplaceCurrentProgress(GameProgress loaded)',
                         'video.Halt();','ui.ResetModalScroll();',
                         'ui.SetSaveWarning(true);','ui.SetSaveWarning(false);'):
            self.assertIn(required,game)
        self.assertIn('var loaded = saves.LoadSlot(slot)',game)
        self.assertLess(game.index('var loaded = saves.LoadSlot(slot)'),game.index('ReplaceCurrentProgress(loaded)'))

    def test_windows_build_runs_stage15_safety_before_editor(self):
        powershell=(ROOT/'Tools/build-windows.ps1').read_text()
        self.assertIn("'test_stage15_*.py'",powershell)
        self.assertLess(powershell.index("'test_stage15_*.py'"),
                        powershell.index("test-unity.ps1"))

    def test_deduction_result_is_not_hidden_below_case_list(self):
        game=(ROOT/'Assets/Scripts/Runtime/EchoGame.cs').read_text()
        self.assertIn('deductionFeedback = success ?',game)
        self.assertIn('ui.AddModalText(deductionFeedback);',game)
        # Stage 22 uses a single-case interactive board, not the old
        # five-rule list. Display the answer feedback before clue pinning and
        # before the three hypothesis buttons so it is visible at the top.
        self.assertLess(game.index('ui.AddModalText(deductionFeedback);'),
                        game.index('ui.AddModalText("线索槽 ①'))

    def test_stage15_unity_playmode_covers_actual_buttons(self):
        test=(ROOT/'Assets/Tests/PlayMode/EchoStage15InteractionTests.cs').read_text()
        for name in ('ChoiceRebuildDisablesOldButtonsBeforeUnityDestroysThem',
                     'LongModalRebuildRetiresOldActionsAndRewindsScroll',
                     'ClueFeedbackAndSaveFailureAreVisibleToThePlayer'):
            self.assertIn(name,test)


if __name__=='__main__':unittest.main()

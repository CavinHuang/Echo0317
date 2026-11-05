"""Stage22 contract tests, not a substitute for Unity Editor or native Player."""
import pathlib
import unittest
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[1]


class Stage22InvestigationContracts(unittest.TestCase):
    def read(self, path):
        return (ROOT / path).read_text(encoding='utf8')

    def test_core_enforces_validated_path_and_two_acquired_distinct_clues(self):
        src = self.read('Assets/Scripts/Core/EchoInvestigation.cs')
        for marker in ('EchoNarrative.Normalize(catalog, progress)',
                       'EchoNarrative.Trace(catalog, normalized.history)',
                       'catalog.Evidence.ContainsKey(id)', 'seen.Add(id)',
                       'r.chapter <= chapters', 'ids.Distinct().Count() != 2',
                       'ids.All(acquired.Contains)', 'SetEquals(rule.clues)',
                       'EchoNarrative.IsSolved(catalog, state, ruleId)'):
            self.assertIn(marker, src)

    def test_game_requires_selection_before_hypotheses_and_resets_on_load(self):
        src = self.read('Assets/Scripts/Runtime/EchoGame.cs')
        self.assertIn('void ToggleEvidencePin(string clueId)', src)
        self.assertIn('void InspectCase(string id)', src)
        self.assertIn('void ShowEvidence()', src)
        self.assertIn('EchoInvestigation.Archive(catalog, state)', src)
        self.assertIn('EchoInvestigation.VisibleCases(catalog, state)', src)
        self.assertIn('EchoInvestigation.PairMatches(catalog, state, rule.id, pinnedEvidence)', src)
        self.assertIn('EchoNarrative.RecordDeduction(catalog, state, ruleId, answer)', src)
        self.assertIn('ResetInvestigation();', src)
        self.assertIn('var target = modalFlow.Back();', src)
        self.assertIn('线索槽 ①', src)

    def test_corehost_executes_real_caseboard_and_five_existing_cases_are_unchanged(self):
        project = ET.parse(ROOT / 'Tools/CoreHost/Echo0317.CoreHost.csproj').getroot()
        included = [x.get('Include', '') for x in project.iter('Compile')]
        self.assertTrue(any(x.endswith('/EchoInvestigation.cs') for x in included))
        host = self.read('Tools/CoreHost/Program.cs')
        for marker in ('EchoInvestigation.Archive(catalog, state)',
                       'EchoInvestigation.VisibleCases(catalog, state)',
                       'EchoInvestigation.PairMatches(catalog, state, rule.id, rule.clues)',
                       'EchoNarrative.RecordDeduction(', 'discarded = EchoNarrative.Rewind('):
            self.assertIn(marker, host)
        import json
        rules = json.loads(self.read('Assets/StreamingAssets/Data/cases.json'))['rules']
        self.assertEqual(5, len(rules))
        self.assertTrue(all(len(r['clues']) == 2 and len(r['options']) == 3 for r in rules))

    def test_unity_tests_present_not_claimed_executed(self):
        edit = self.read('Assets/Tests/EditMode/EchoInvestigationTests.cs')
        play = self.read('Assets/Tests/PlayMode/EchoStage22InvestigationUiTests.cs')
        self.assertIn('EvidenceArchiveAndPinnedPairsFollowRealEndingsAndRewind', edit)
        self.assertIn('DisabledAndRebuiltClueButtonsCannotTriggerOldEvidenceActions', play)


if __name__ == '__main__':
    unittest.main()

"""Stage29 scenario/canonical audit gates, not a substitute for Unity PlayMode."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]


class Stage29Consequences(unittest.TestCase):
    def content(self, path):
        return (ROOT/path).read_text(encoding="utf-8")

    def test_audit_ending_requires_case_result_not_just_evidence_flags(self):
        story=json.loads(self.content("Assets/StreamingAssets/Data/story.json"))
        nodes={x["id"]:x for x in story["nodes"]}
        gates=[c for c in nodes["c5_gate"]["choices"] if c["to"]=="ending_audit"]
        self.assertEqual(1,len(gates))
        self.assertEqual(["independent_will"],gates[0]["requiresSolved"])
        self.assertIn("推理",gates[0]["lockedLabel"])
        rules=json.loads(self.content("Assets/StreamingAssets/Data/cases.json"))["rules"]
        self.assertEqual(1,len([r for r in rules if r["id"]=="independent_will"]))
        self.assertEqual(["protocol_read","lin_willing"],
            next(r for r in rules if r["id"]=="independent_will")["clues"])

    def test_authorized_transition_rejects_historical_audit_bypass(self):
        src=self.content("Assets/Scripts/Core/EchoNarrative.cs")
        ui=self.content("Assets/Scripts/Runtime/EchoGame.cs")
        self.assertIn('public static bool AllowedWithDeductions(',src)
        self.assertIn('!AllowedWithDeductions(g, choice, p)',src)
        self.assertIn('a.answer == required.correct && a.at <= i',src)
        self.assertIn('剧情曾绕过未完成的推理',src)
        self.assertIn('AllowedWithDeductions(catalog, choice, state)',ui)
        self.assertIn('if (success) Render();',ui)
        self.assertIn('choice.lockedLabel',ui)
        self.assertIn('if (c.requiresSolved != null)',self.content("Assets/Scripts/Core/EchoData.cs"))

    def test_existing_authored_conditionals_reach_scene_and_recap(self):
        story=json.loads(self.content("Assets/StreamingAssets/Data/story.json"))
        self.assertGreaterEqual(sum(bool(n.get("reactions")) for n in story["nodes"]),8)
        renderer=self.content("Assets/Scripts/Core/EchoScenePresentation.cs")
        self.assertIn('acquired.Contains(reaction.@if)',renderer)
        self.assertIn('acquired.Contains(reaction.unless)',renderer)
        ui=self.content("Assets/Scripts/Runtime/EchoGame.cs")
        recap=self.content("Assets/Scripts/Core/EchoDialogueHistory.cs")
        self.assertIn('EchoScenePresentation.Lines(n, state.flags, state.attempts)',ui)
        self.assertIn('EchoScenePresentation.Lines(node, points[i].Flags,',recap)
        self.assertIn('state.attempts.Where(a => a.at <= i)',recap)
        self.assertNotIn('Lines = (string[])(node.lines?.Clone()',recap)

    def test_legacy_old_saves_only_get_proof_when_historically_earned(self):
        savings=self.content("Assets/Scripts/Core/EchoSaves.cs")
        narrative=self.content("Assets/Scripts/Core/EchoNarrative.cs")
        self.assertIn('game = "0317-echo", format = 7',savings)
        self.assertIn('env.format <= 6',savings)
        self.assertIn('EchoNarrative.Trace(catalog,',savings)
        self.assertIn('!rule.clues.All(flags.Contains)',savings)
        self.assertIn('env.format != 7',savings)
        self.assertIn('public int format = 7;',narrative)

    def test_actual_corehost_and_unity_tests_replay_four_endings_with_proof(self):
        host=self.content("Tools/CoreHost/Program.cs")
        native=self.content("Assets/Scripts/Runtime/EchoNativeQa.cs")
        editor=self.content("Assets/Tests/EditMode/EchoJourneyTests.cs")
        for src in (host,native,editor):
            self.assertIn('EchoNarrative.RecordDeduction(',src)
            self.assertIn('"independent_will"',src)
            self.assertIn('AllowedWithDeductions(',src)
        project=ET.parse(ROOT/"Tools/CoreHost/Echo0317.CoreHost.csproj")
        includes=[n.attrib.get("Include","") for n in project.iter("Compile")]
        self.assertTrue(any(s.endswith("/EchoScenePresentation.cs") for s in includes))
        tests=self.content("Assets/Tests/EditMode/EchoStage29NarrativeConsequencesTests.cs")
        self.assertIn('AuditEndingCannotBypassIndependentWillDeduction',tests)
        self.assertIn('OldAuditSaveIsMigratedButNewFormatCannotForgeProof',tests)
        bootstrap=self.content("Assets/Tests/EditMode/EchoStage29ProjectBootstrapTests.cs")
        self.assertIn('EchoBuild.EnsureScene();',bootstrap)
        self.assertIn('AssetDatabase.LoadAssetAtPath<SceneAsset>(scene)',bootstrap)
        self.assertIn('EditorBuildSettings.scenes.Where(s=>s.enabled)',bootstrap)


if __name__=="__main__":
    unittest.main()

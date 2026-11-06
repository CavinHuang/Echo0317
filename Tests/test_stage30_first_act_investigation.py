"""Stage30 first-act investigations: real reactions, crosschecks and save safety."""
import json
from pathlib import Path
import unittest

ROOT=Path(__file__).resolve().parents[1]


class Stage30FirstActInvestigations(unittest.TestCase):
    def src(self,p):
        return (ROOT/p).read_text(encoding="utf-8")

    def test_all_authored_first_act_crosschecks_link_to_existing_hotspots(self):
        data=json.loads(self.src("Assets/StreamingAssets/Data/hotspots.json"))
        hotspots={h["id"]:h for h in data["hotspots"]}
        self.assertEqual(23,len(hotspots))
        checks=[h for h in hotspots.values() if h.get("crossCheckId")]
        self.assertEqual(7,len(checks))
        for h in checks:
            self.assertIn(h["crossCheckId"],hotspots)
            self.assertNotEqual(h["id"],h["crossCheckId"])
            self.assertGreater(len(h["crossCheckText"]),15)
            self.assertIn(h["node"], {
                "intro","c1_peephole","c1_photo","c1_receipt",
                "c1_password","c1_playvoice"
            })
        self.assertEqual("peephole_shadow",hotspots["door_password"]["crossCheckId"])
        self.assertEqual("intro_phone",hotspots["receipt_queue"]["crossCheckId"])

    def test_real_story_reactions_reference_authored_hotspots_only(self):
        story=json.loads(self.src("Assets/StreamingAssets/Data/story.json"))
        hs=json.loads(self.src("Assets/StreamingAssets/Data/hotspots.json"))
        hotspot_ids={h["id"] for h in hs["hotspots"]}
        reactions=[(n["id"],r) for n in story["nodes"]
                   for r in n.get("reactions",[]) if r.get("ifInspected")]
        self.assertEqual(15,len(reactions))
        self.assertGreaterEqual(len({n for n,_ in reactions}),8)
        for node,r in reactions:
            self.assertIn(r["ifInspected"],hotspot_ids)
            self.assertGreater(len(r["text"]),15)
        self.assertEqual(5,sum(n=="c1_hook" for n,_ in reactions))
        self.assertEqual(3,sum(n=="c2_call" for n,_ in reactions))

    def test_runtime_and_historical_story_rely_on_observed_timeline(self):
        story=self.src("Assets/Scripts/Core/EchoScenePresentation.cs")
        history=self.src("Assets/Scripts/Core/EchoDialogueHistory.cs")
        ui=self.src("Assets/Scripts/Runtime/EchoGame.cs")
        for marker in (
            "IEnumerable<HotspotVisit> observations = null",
            "inspected.Contains(reaction.ifInspected)",
            "inspected.Contains(reaction.unlessInspected)"
        ):
            self.assertIn(marker,story)
        self.assertIn("state.inspections.Where(v => v.at <= i)",history)
        self.assertIn("state.attempts.Where(a => a.at <= i)",history)
        self.assertIn("state.flags, state.attempts, state.inspections",ui)
        self.assertIn("ui.Body.text = string.Join(",ui)

    def test_crosscheck_is_not_equivalent_to_an_earned_case_flag(self):
        investigator=self.src("Assets/Scripts/Core/EchoHotspotInvestigation.cs")
        overlay=self.src("Assets/Scripts/UI/EchoInvestigationOverlay.cs")
        data=self.src("Assets/Scripts/Core/EchoData.cs")
        self.assertIn("public static string CrossCheck(",investigator)
        self.assertIn("WasInspected(progress, hotspot.id)",investigator)
        self.assertIn("WasInspected(progress, hotspot.crossCheckId)",investigator)
        self.assertIn("EchoHotspotInvestigation.CrossCheck(progress,selected)",overlay)
        self.assertIn("【线索交叉核验】",overlay)
        self.assertIn('!Hotspots.TryGetValue(h.crossCheckId, out var related)',data)
        self.assertIn('!Hotspots.ContainsKey(reaction.ifInspected)',data)
        self.assertNotIn("state.flags =",investigator)

    def test_corehost_and_unity_native_checks_cover_live_gameplay(self):
        host=self.src("Tools/CoreHost/Program.cs")
        for marker in (""intro_phone"", ""intro_photo"", ""peephole_shadow"",
                       ""door_password"", "EchoScenePresentation.Lines(",
                       "EchoDialogueHistory.Read(", "EchoNarrative.Rewind(",
                       "EchoHotspotInvestigation.CrossCheck(",
                       "store.SaveSlot(1,hookObserved)","store.LoadSlot(1)"):
            self.assertIn(marker,host)
        edit=self.src("Assets/Tests/EditMode/EchoStage30FirstActInvestigationTests.cs")
        play=self.src("Assets/Tests/PlayMode/EchoStage30InvestigationFeedbackUiTests.cs")
        self.assertIn("DoorPasswordComparedToPeepholeChangesActualFirstActStory",edit)
        self.assertIn("InvalidCrossReferencesFailDuringDataImport",edit)
        self.assertIn("NativeResultCardShowsCrosscheckOnlyAfterBothObjectsVisited",play)


if __name__=="__main__":
    unittest.main()

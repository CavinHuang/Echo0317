"""Stage 24 gameplay and safety contracts. Unity rendering requires Editor PlayMode."""
import json
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]


class HotspotRuntimeContract(unittest.TestCase):
    def source(self,path):
        return (ROOT/path).read_text(encoding="utf8")

    def test_real_catalog_contains_narrative_aligned_targets(self):
        graph=json.loads(self.source("Assets/StreamingAssets/Data/story.json"))
        data=json.loads(self.source("Assets/StreamingAssets/Data/hotspots.json"))
        evidence={x["id"] for x in graph["evidence"]}
        nodes={n["id"] for n in graph["nodes"]}
        items=data["hotspots"]
        self.assertEqual(23,len(items))
        self.assertEqual(len(items),len({x["id"] for x in items}))
        self.assertTrue({"intro","c1_peephole","c2_archive","c3_receiver"}.issubset(
            {x["node"] for x in items}))
        for x in items:
            self.assertIn(x["node"],nodes)
            self.assertGreater(x["w"],0)
            self.assertGreater(x["h"],0)
            self.assertGreaterEqual(x["x"],0)
            self.assertGreaterEqual(x["y"],0)
            self.assertLessEqual(x["x"]+x["w"],1)
            self.assertLessEqual(x["y"]+x["h"],1)
            if x["evidenceId"]:
                self.assertIn(x["evidenceId"],evidence)

    def test_all_inspections_are_audited_and_never_forge_story_flags(self):
        core=self.source("Assets/Scripts/Core/EchoHotspotInvestigation.cs")
        narrative=self.source("Assets/Scripts/Core/EchoNarrative.cs")
        for token in ('EchoNarrative.Trace(catalog, history',
                      'points[record.at].Node != hotspot.node',
                      'unique.Add(record.at + ":" + record.id)',
                      'state.inspections = state.inspections.Concat',
                      'EchoNarrative.Normalize(catalog, state)',
                      'state.flags.Contains(hotspot.evidenceId)'):
            self.assertIn(token,core)
        self.assertIn('EchoHotspotInvestigation.Validate(g, canonical, raw.inspections)',narrative)
        self.assertIn('p.inspections = p.inspections.Where(v => v.at <= stepIndex)',narrative)
        self.assertNotIn('state.flags = ',core)
        project=ET.parse(ROOT/"Tools/CoreHost/Echo0317.CoreHost.csproj")
        sources=[n.attrib.get("Include","") for n in project.iter("Compile")]
        self.assertTrue(any(p.endswith("/EchoHotspotInvestigation.cs") for p in sources))

    def test_scene_mode_is_wired_to_normal_game_and_hotkey(self):
        game=self.source("Assets/Scripts/Runtime/EchoGame.cs")
        ui=self.source("Assets/Scripts/UI/EchoGameUI.cs")
        for token in ('BeginSceneInvestigation()', 'InspectSceneHotspot(string id)',
                      'StopSceneInvestigation(bool resumeFilm = true)',
                      'video.Pause();', 'ui.SetInvestigationInputLocked(true)',
                      'EchoHotspotInvestigation.Inspect(catalog, state, id)',
                      'if (!wasInspected) Persist();',
                      'if(Input.GetKeyDown(KeyCode.H)){BeginSceneInvestigation();return;}',
                      'if (investigating) StopSceneInvestigation(); else HandleBack();'):
            self.assertIn(token,game)
        self.assertIn('public void SetInvestigationInputLocked(bool locked)',ui)

    def test_generated_art_pack_asset_names_are_loaded_and_safely_degraded(self):
        art=self.source("Assets/Scripts/UI/EchoInvestigationArt.cs")
        visual=self.source("Assets/Scripts/UI/EchoInvestigationOverlay.cs")
        for s in ('"Investigation", name + ".png"',
                  'texture.LoadImage(File.ReadAllBytes(path), true)',
                  'Sprite.Create(texture','DrawFrame(name)', 'hotspot-idle',
                  'hotspot-focus','hotspot-complete','icon-','notice-new',
                  'notice-repeat','notice-updated','inspect-result'):
            self.assertIn(s,art+visual)
        self.assertIn('class EchoHotspotVisual',visual)
        # Hotspots need the actual frozen film frame visible beneath them.
        self.assertIn('new Color(.015f,.035f,.052f,.27f)', visual)
        self.assertIn('new Color(.055f,.077f,.10f,.06f)', visual)
        self.assertIn('new Color(.09f,.135f,.17f,.97f)', visual)
        self.assertIn('ICancelHandler',self.source("Assets/Scripts/UI/EchoGameUI.cs"))

    def test_native_editor_tests_exist_but_cannot_pass_without_a_license(self):
        editor=self.source("Assets/Tests/EditMode/EchoStage24HotspotTests.cs")
        play=self.source("Assets/Tests/PlayMode/EchoStage24HotspotUiTests.cs")
        self.assertIn('RewindingDiscardsAbandonedHotspotsButNotEarnedNarrativeFlags',editor)
        self.assertIn('InteractiveHotspotsReceiveClicksAndRestoreNarrativeFocus',play)


if __name__=="__main__":
    unittest.main()

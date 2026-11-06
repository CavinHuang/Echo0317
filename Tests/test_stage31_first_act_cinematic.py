"""Stage31 native-first cinematic continuity and accessibility contracts."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]


class FirstActCinematics(unittest.TestCase):
    def source(self, path):
        return (ROOT/path).read_text(encoding="utf-8")

    def test_first_act_foley_uses_original_synthesis_and_scene_visit_keys(self):
        plan=self.source("Assets/Scripts/Core/EchoFirstActFoleyPlan.cs")
        engine=self.source("Assets/Scripts/Runtime/EchoFirstActFoley.cs")
        runtime=self.source("Assets/Scripts/Runtime/EchoGame.cs")
        for name in ("intro","warning","c1_peephole","c1_photo",
                     "c1_receipt","c1_knock","c1_password","c1_playvoice",
                     "c1_trydoor","c1_hook"):
            self.assertIn('"' + name + '"',plan)
        for sound in ("phone-buzz", "door-knock", "paper-rustle", "tape-hiss",
                      "keypad-tone", "latch-warning"):
            self.assertIn(sound,plan)
            self.assertIn(sound,engine)
        self.assertIn("AudioClip.Create",engine)
        self.assertIn("Time.unscaledDeltaTime",engine)
        self.assertIn("if (newVisitKey == visitKey) return;",engine)
        self.assertIn("firstActFoley?.SetScene(",runtime)
        self.assertIn("firstActFoley?.StopScene();",runtime)
        self.assertIn("firstActFoley?.SetMuted(muted);",runtime)
        self.assertIn("firstActFoley?.SetPaused(true);",runtime)
        self.assertIn("firstActFoley?.SetPaused(false);",runtime)
        self.assertNotIn("UnityWebRequest",engine)

    def test_previous_movie_cannot_leak_when_asset_is_unavailable(self):
        director=self.source("Assets/Scripts/Runtime/EchoVideoDirector.cs")
        game=self.source("Assets/Scripts/Runtime/EchoGame.cs")
        for token in ("StaticImage.texture = null;",
                      "StaticImage.color = Color.clear;",
                      "RetireToFallback(", "playback.Skip();",
                      "public bool IsFallback =>", "LastFallbackReason",
                      "HasPlayableAsset", "HasApprovedFinal"):
            self.assertIn(token,director)
        self.assertNotIn("if (file == null) { Halt(); return; }",director)
        self.assertIn("video.IsFallback",game)
        self.assertIn("静态场景 · 镜头未制作",game)

    def test_subtitles_use_current_timed_cue_only_for_prepared_movie(self):
        policy=self.source("Assets/Scripts/Core/EchoSubtitlePresentation.cs")
        game=self.source("Assets/Scripts/Runtime/EchoGame.cs")
        self.assertIn("if (preparing) return",policy)
        self.assertIn("if (hasActiveMovie)",policy)
        self.assertIn('return activeTimedCue ?? "";',policy)
        self.assertIn("EchoSubtitlePresentation.Select(",game)
        self.assertIn("video.IsPreparing, video.HasActiveVideo,",game)
        self.assertIn("catalog.Cue(video.CurrentClip, video.CurrentTime)",game)

    def test_corehost_compiles_sound_plan_and_real_subtitle_policy(self):
        root=ET.parse(ROOT/"Tools/CoreHost/Echo0317.CoreHost.csproj").getroot()
        paths=[n.attrib.get("Include","") for n in root.iter("Compile")]
        for name in ("EchoFirstActFoleyPlan.cs","EchoSubtitlePresentation.cs"):
            self.assertTrue(any(p.endswith(name) for p in paths))
        host=self.source("Tools/CoreHost/Program.cs")
        self.assertIn("EchoFirstActFoleyPlan.Valid()",host)
        self.assertIn("EchoSubtitlePresentation.Select(",host)

    def test_native_edit_and_playmode_tests_are_authored_not_fake_runs(self):
        edit=self.source("Assets/Tests/EditMode/EchoStage31CinematicPolicyTests.cs")
        play=self.source("Assets/Tests/PlayMode/EchoStage31CinematicFallbackTests.cs")
        self.assertIn("AllTenFirstActSceneVisitsHaveOrderedOriginalFoley",edit)
        self.assertIn("LoadingAndPausedMovieNeverExposeLastStoryLineAsSubtitle",edit)
        self.assertIn("UnmadeSceneUsesSafeStaticArtAndRetainsReplayIdentity",play)
        self.assertIn("FoleyKeepsItsScheduleAcrossPauseMuteAndDuplicateRender",play)


if __name__=="__main__":
    unittest.main()

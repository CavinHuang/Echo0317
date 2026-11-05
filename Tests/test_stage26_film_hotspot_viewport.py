"""Stage 26 integration guards; actual Unity PlayMode is still a separate step."""
from pathlib import Path
import unittest
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[1]


class FilmHotspotIntegration(unittest.TestCase):
    def read(self,name):
        return (ROOT/name).read_text(encoding="utf-8")

    def test_real_movie_layer_mounted_not_faked(self):
        ui=self.read("Assets/Scripts/UI/EchoInvestigationOverlay.cs")
        runtime=self.read("Assets/Scripts/Runtime/EchoGame.cs")
        for marker in (
            'public void MountFilm(RectTransform videoLayer)',
            'HotspotArea.SetParent(mountedFilm,false);',
            'mountedFilm.SetParent(FilmStage,false);',
            'AspectRatioFitter.AspectMode.FitInParent',
            'fitter.aspectRatio=16f/9f;',
            'mountedFilm.SetAsFirstSibling();',
            'HotspotArea.SetAsLastSibling();',
            'void UnmountFilm()',
            'mountedFilm.SetParent(originalFilmParent,false);',
            'HotspotArea.SetParent(FilmStage,false);',
            'UnmountFilm();',
        ):
            self.assertIn(marker,ui)
        self.assertIn('investigationOverlay.MountFilm(ui.VideoLayer);',runtime)
        self.assertIn('investigationOverlay.Hide();',runtime)
        self.assertIn('video.Pause();',runtime)
        self.assertIn('video.Resume();',runtime)
        self.assertNotIn('new VideoPlayer()',ui)

    def test_deterministic_gamepad_navigation_compiled_in_actual_host(self):
        ui=self.read("Assets/Scripts/UI/EchoInvestigationOverlay.cs")
        navigator=self.read("Assets/Scripts/Core/EchoHotspotNavigation.cs")
        host=self.read("Tools/CoreHost/Program.cs")
        tree=ET.parse(ROOT/"Tools/CoreHost/Echo0317.CoreHost.csproj").getroot()
        files=[x.get('Include','') for x in tree.iter('Compile')]
        self.assertTrue(any(x.endswith("/EchoHotspotNavigation.cs") for x in files))
        self.assertIn('ConfigureNavigation(hotspots);',ui)
        self.assertIn('EchoHotspotNavigation.Next(hotspots,current,direction)',ui)
        self.assertIn('navigation.mode=Navigation.Mode.Explicit',ui)
        self.assertIn('EchoHotspotDirection.Right',navigator)
        self.assertIn('EchoHotspotNavigation.Next(introNavigation, "intro_phone"',host)

    def test_native_visual_playmode_tests_exist(self):
        source=self.read("Assets/Tests/PlayMode/EchoStage26FilmViewportTests.cs")
        editor=self.read("Assets/Tests/EditMode/EchoStage26HotspotNavigationTests.cs")
        self.assertIn('MovieAndHotspotsUseTheSameLetterboxedRectThenRestoreCleanly',source)
        self.assertIn('ExplicitGamepadDirectionsAndCancelReturnOnlyOnce',source)
        self.assertIn('DirectionalFocusFindsNearestRealTarget',editor)


if __name__=="__main__":
    unittest.main()

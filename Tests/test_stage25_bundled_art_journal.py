"""Stage25 production-asset and evidence-provenance regression checks."""
import pathlib
import struct
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/StreamingAssets/Investigation"
REQUIRED = {
    "hotspot-idle.png", "hotspot-focus.png", "hotspot-complete.png",
    "inspect-result.png", "notice-new.png", "notice-repeat.png", "notice-updated.png",
    "icon-door.png", "icon-shadow.png", "icon-terminal.png", "icon-microphone.png",
    "icon-recorder.png", "icon-waveform.png", "icon-receipt.png", "icon-sms.png",
    "icon-report.png", "icon-identity.png", "icon-monitor.png"
}

class Stage25BundledArtwork(unittest.TestCase):
    def test_all_generated_pngs_are_committed_and_transparent(self):
        self.assertEqual(REQUIRED, {p.name for p in ART.glob("*.png")})
        total = 0
        for name in sorted(REQUIRED):
            data = (ART / name).read_bytes()
            total += len(data)
            self.assertTrue(data.startswith(b"\x89PNG\r\n\x1a\n"), name)
            self.assertEqual(b"IHDR", data[12:16], name)
            width, height = struct.unpack(">II", data[16:24])
            self.assertGreater(width, 100, name)
            self.assertGreater(height, 100, name)
            # The optimized UI sprites are palette PNGs with transparent pixels.
            self.assertEqual(3, data[25], name)
            self.assertIn(b"tRNS", data, name)
        self.assertLess(total, 500_000, "Stage25 sprites should stay lightweight")

    def test_optional_loading_uses_actual_tracked_pngs(self):
        source = (ROOT / "Assets/Scripts/UI/EchoInvestigationArt.cs").read_text("utf8")
        self.assertIn('"Investigation", name + ".png"', source)
        self.assertIn("File.Exists(path)", source)
        self.assertIn("texture.LoadImage(File.ReadAllBytes(path), true)", source)
        self.assertIn("DrawFrame(name)", source)

    def test_journal_is_read_only_normalized_and_paginated(self):
        journal = (ROOT / "Assets/Scripts/Core/EchoInvestigationJournal.cs").read_text("utf8")
        for token in ("EchoNarrative.Normalize(catalog, progress)",
                      "visits.Reverse().Skip(page * pageSize).Take(pageSize)",
                      "state.flags.Contains(hotspot.evidenceId)",
                      "public const int MaxPageSize = 20",
                      "SceneTitle = scene.title"):
            self.assertIn(token, journal)
        host = (ROOT / "Tools/CoreHost/Program.cs").read_text("utf8")
        self.assertIn("EchoInvestigationJournal.Read(catalog, hotVisited)", host)
        self.assertIn("EchoInvestigationJournal.Read(catalog, rewoundHotspots)", host)

    def test_pause_archive_and_journal_details_are_connected(self):
        game = (ROOT / "Assets/Scripts/Runtime/EchoGame.cs").read_text("utf8")
        self.assertIn('case "journal": ShowJournal(); break;', game)
        self.assertIn('void ShowJournal()', game)
        self.assertIn('ui.AddModalButton("查看现场调查手记"', game)
        self.assertIn('journalReturnPage="pause"', game)
        self.assertIn('focusedJournalKey = captured.Key;', game)
        self.assertIn('record.EvidenceVerified', game)
        self.assertIn('if (modalFlow.Current == "journal")', game)
        edit = (ROOT / "Assets/Tests/EditMode/EchoStage25InvestigationJournalTests.cs").read_text()
        self.assertIn('JournalPagesClampAndNewestEntriesComeFirst', edit)

if __name__ == "__main__":
    unittest.main()

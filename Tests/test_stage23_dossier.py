"""Stage23 source integration checks; Unity PlayMode artwork/audio needs a real Editor."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class Stage23DossierContract(unittest.TestCase):
    def src(self, path):
        return (ROOT / path).read_text(encoding="utf8")

    def test_procedural_art_is_original_bundled_and_cached(self):
        art = self.src("Assets/Scripts/UI/EchoDossierArtwork.cs")
        self.assertIn("new Texture2D(width, height", art)
        self.assertIn("Sprite.Create(tex", art)
        self.assertIn("public static Sprite Card", art)
        self.assertIn("public static Sprite Slot", art)
        self.assertIn("width = 320, height = 128", art)
        self.assertIn("tex.SetPixels32(pixels)", art)

    def test_real_native_card_gallery_and_two_removable_slots(self):
        ui = self.src("Assets/Scripts/UI/EchoGameUI.cs")
        for token in ("AddDossierCard(", "AddEvidenceSlots(", "AddEvidenceSlot(",
                      "AddJudgementCard(", "AddBoardFeedback(",
                      "SelectedDossierCardId()", "FocusDossierCard(",
                      'rt.GetComponent<Image>().sprite=EchoDossierArtwork.Slot',
                      "image.sprite = EchoDossierArtwork.Card",
                      "EchoDossierReveal", "EchoDossierCardKey"):
            self.assertIn(token, ui)
        self.assertIn("rt.gameObject.AddComponent<EchoCancelAction>()", ui)
        self.assertIn("rt.gameObject.AddComponent<EchoScrollSelection>()", ui)

    def test_caseboard_uses_native_cards_keeps_focus_and_never_bypasses_rules(self):
        game = self.src("Assets/Scripts/Runtime/EchoGame.cs")
        self.assertIn('ui.AddDossierCard(', game)
        self.assertIn('ui.AddEvidenceSlots(', game)
        self.assertIn('ui.AddJudgementCard(', game)
        self.assertIn('ui.FocusDossierCard(previousCard);', game)
        self.assertIn('ui.ArchiveScroll.verticalNormalizedPosition = previousScroll;', game)
        self.assertIn('EchoInvestigation.PairMatches(catalog, state, rule.id, pinnedEvidence)', game)
        self.assertIn('EchoNarrative.RecordDeduction(catalog, state, ruleId, answer)', game)

    def test_feedback_is_animated_and_original_sound_respects_mute(self):
        sound = self.src("Assets/Scripts/UI/EchoDossierSound.cs")
        animation = self.src("Assets/Scripts/UI/EchoDossierBehaviours.cs")
        game = self.src("Assets/Scripts/Runtime/EchoGame.cs")
        self.assertIn('AudioClip.Create(name, count, 1, frequency, false)', sound)
        self.assertIn('if (muted || output == null || !isActiveAndEnabled) return;', sound)
        self.assertIn('Time.unscaledDeltaTime', animation)
        self.assertIn('EchoDossierCue.Pin, muted', game)
        self.assertIn('EchoDossierCue.Reject, muted', game)
        self.assertIn('EchoDossierCue.Solve : EchoDossierCue.Reject', game)

    def test_real_unity_playmode_cover_generated_art_focus_and_mute(self):
        tests = self.src("Assets/Tests/PlayMode/EchoStage23DossierUiTests.cs")
        for name in ("ProceduralArtworkAndEvidenceCardsAreClickableAndNavigable",
                     "RerenderedCardsRestoreSemanticFocusAndDoNotAcceptStaleClicks",
                     "MuteDisablesGeneratedEvidenceFeedbackSound"):
            self.assertIn(name, tests)


if __name__ == "__main__":
    unittest.main()

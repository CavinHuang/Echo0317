#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Echo0317.UI;

namespace Echo0317.Tests
{
    public sealed class EchoStage23DossierUiTests
    {
        [UnityTest]
        public IEnumerator ProceduralArtworkAndEvidenceCardsAreClickableAndNavigable()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetModal(true);
                var cardSprite = EchoDossierArtwork.Card;
                var slotSprite = EchoDossierArtwork.Slot;
                Assert.NotNull(cardSprite);
                Assert.NotNull(slotSprite);
                Assert.AreEqual(320f, cardSprite.rect.width);
                Assert.AreEqual(128f, slotSprite.rect.height);
                Assert.AreSame(cardSprite, EchoDossierArtwork.Card,
                    "The generated art is cached rather than rebuilt on each click");

                int pins = 0;
                var card = ui.AddDossierCard("shadow", "EVIDENCE / CH 01",
                    "反向影子", "走廊 · 03:17", false, 0, () => pins++);
                Assert.AreSame(cardSprite, card.GetComponent<Image>().sprite);
                Assert.NotNull(card.GetComponent<EchoScrollSelection>());
                Assert.NotNull(card.GetComponent<EchoFocusOutline>());
                Assert.NotNull(card.GetComponent<EchoCancelAction>());
                card.onClick.Invoke();
                Assert.AreEqual(1, pins);

                EventSystem.current.SetSelectedGameObject(card.gameObject);
                Assert.AreEqual("shadow", ui.SelectedDossierCardId());
                Assert.IsTrue(card.GetComponent<Outline>().enabled,
                    "The current selected dossier card must show keyboard focus");

                int removed = 0;
                ui.AddEvidenceSlots("反向影子", "", "shadow", "",
                    () => removed++, () => Assert.Fail("Empty slot should not invoke"));
                var slots = ui.ModalList.Find("EvidenceSlotsRow");
                Assert.NotNull(slots);
                var first = slots.Find("EvidenceSlot-1").GetComponent<Button>();
                var second = slots.Find("EvidenceSlot-2").GetComponent<Button>();
                Assert.IsTrue(first.interactable);
                Assert.IsFalse(second.interactable);
                Assert.AreSame(slotSprite, first.GetComponent<Image>().sprite);
                first.onClick.Invoke();
                Assert.AreEqual(1, removed);

                ui.AddBoardFeedback("线索关联成功", "现在作出判断。", true);
                var feedback = ui.ModalList.Find("DossierFeedback");
                Assert.NotNull(feedback.GetComponent<EchoDossierReveal>());
                Assert.IsFalse(feedback.GetComponent<Image>().raycastTarget);
                ui.AddJudgementCard(1, "声纹并不能证明身份", () => pins += 10);
                var judgement = ui.ModalList.Find("Judgement-1").GetComponent<Button>();
                judgement.onClick.Invoke();
                Assert.AreEqual(11, pins);
                yield return null;
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator RerenderedCardsRestoreSemanticFocusAndDoNotAcceptStaleClicks()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetModal(true);
                int clicks = 0;
                var old = ui.AddDossierCard("receipt", "EVIDENCE",
                    "投递队列", "短信 · 03:22", false, 0, () => clicks++);
                EventSystem.current.SetSelectedGameObject(old.gameObject);
                var id = ui.SelectedDossierCardId();
                ui.ClearModal();
                Assert.IsFalse(old.gameObject.activeSelf,
                    "Retired cards must become non-interactable before Unity's deferred Destroy");
                var rebuilt = ui.AddDossierCard("receipt", "EVIDENCE",
                    "投递队列", "短信 · 03:22", true, 1, () => clicks += 10);
                ui.FocusDossierCard(id);
                Assert.AreSame(rebuilt.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.AreSame(ui.ArchiveScroll, rebuilt.GetComponent<EchoScrollSelection>().Target,
                    "Restored focus must remain scroll-aware after the board rerenders");
                Assert.NotNull(rebuilt.GetComponent<EchoDossierReveal>());
                rebuilt.onClick.Invoke();
                Assert.AreEqual(10, clicks, "The new card must use the new callback only");
                yield return null;
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator MuteDisablesGeneratedEvidenceFeedbackSound()
        {
            var ui = new EchoGameUI();
            try
            {
                var sound = ui.Canvas.GetComponent<EchoDossierSound>();
                Assert.NotNull(sound);
                ui.PlayDossierSound(EchoDossierCue.Pin, true);
                Assert.IsFalse(ui.Canvas.GetComponent<AudioSource>().isPlaying);
                ui.PlayDossierSound(EchoDossierCue.Reject, true);
                ui.PlayDossierSound(EchoDossierCue.Solve, true);
                yield return null;
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }
    }
}
#endif

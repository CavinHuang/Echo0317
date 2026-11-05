#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Echo0317.UI;

namespace Echo0317.Tests
{
    /// <summary>Player-facing interaction regression tests; must run inside Unity PlayMode.</summary>
    public sealed class EchoStage15InteractionTests
    {
        [UnityTest]
        public IEnumerator ChoiceRebuildDisablesOldButtonsBeforeUnityDestroysThem()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetGameVisible(true);
                var oldClicks = 0;
                ui.AddChoice("旧的选择", () => oldClicks++);
                var stale = ui.ChoiceList.GetChild(0).gameObject;
                Assert.IsTrue(stale.activeSelf);
                ui.ClearChoices();
                Assert.IsFalse(stale.activeSelf,
                    "Deferred Destroy must not leave a previous-node choice clickable for one frame");
                var newClicks = 0;
                ui.AddChoice("新的选择", () => newClicks++);
                ui.ResetStoryScroll();
                Assert.AreEqual(0, oldClicks);
                yield return null;
                Assert.AreEqual(1, ui.ChoiceList.childCount, "Old choice should be destroyed on next frame");
                ui.ChoiceList.GetChild(0).GetComponent<Button>().onClick.Invoke();
                Assert.AreEqual(1, newClicks);
                Assert.AreEqual(0, oldClicks);
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator LongModalRebuildRetiresOldActionsAndRewindsScroll()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetModal(true);
                ui.AddModalButton("旧存档动作", () => { });
                var previous = ui.ModalList.GetChild(0).gameObject;
                ui.ClearModal();
                Assert.IsFalse(previous.activeSelf);
                ui.AddModalButton("确认读取", () => { });
                ui.ArchiveScroll.verticalNormalizedPosition = 0f;
                ui.ResetModalScroll();
                Assert.Greater(ui.ArchiveScroll.verticalNormalizedPosition, 0.99f);
                yield return null;
                Assert.AreEqual(1, ui.ModalList.childCount);
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator ClueFeedbackAndSaveFailureAreVisibleToThePlayer()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetGameVisible(true);
                Assert.IsFalse(ui.DiscoveryCard.activeSelf);
                ui.ShowDiscovery("新线索已收录 · 门锁记录");
                Assert.IsTrue(ui.DiscoveryCard.activeSelf);
                StringAssert.Contains("门锁记录", ui.DiscoveryText.text);
                ui.SetSaveWarning(true);
                StringAssert.Contains("自动存档失败", ui.SaveStatus.text);
                ui.SetSaveWarning(false);
                Assert.IsEmpty(ui.SaveStatus.text);
                ui.HideDiscovery();
                Assert.IsFalse(ui.DiscoveryCard.activeSelf);
                yield return null;
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }
    }
}
#endif

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
    public sealed class EchoStage21InteractionTests
    {
        [UnityTest]
        public IEnumerator GamepadCancelClosesModalWithoutActivatingLockedStoryChoices()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetGameVisible(true);
                ui.AddChoice("条件未满足", () => Assert.Fail("Locked choice activated"), false);
                ui.AddChoice("检查线索", () => { });
                var expected = ui.ChoiceList.GetChild(1).gameObject;
                Assert.AreSame(expected, EventSystem.current.currentSelectedGameObject);
                Assert.IsTrue(expected.GetComponent<Outline>().enabled,
                    "Focused choices must have a visible highlight");

                int backs = 0;
                ui.OnBack = () => { backs++; ui.SetModal(false); };
                ui.SetModal(true);
                Assert.IsFalse(ui.Game.GetComponent<CanvasGroup>().interactable);
                Assert.IsFalse(ui.Game.GetComponent<CanvasGroup>().blocksRaycasts);
                ui.AddModalText("存档管理");
                ui.AddModalButton("返回", () => { });
                var button = ui.ModalList.GetComponentInChildren<Button>();
                Assert.NotNull(button);
                Assert.AreSame(button.gameObject, EventSystem.current.currentSelectedGameObject);
                Assert.NotNull(button.GetComponent<EchoCancelAction>());
                ExecuteEvents.Execute(button.gameObject, new BaseEventData(EventSystem.current),
                    ExecuteEvents.cancelHandler);
                Assert.AreEqual(1, backs, "Gamepad cancel must invoke back exactly once");
                Assert.IsFalse(ui.Modal.activeSelf);
                Assert.IsTrue(ui.Game.GetComponent<CanvasGroup>().interactable);
                Assert.AreSame(expected, EventSystem.current.currentSelectedGameObject,
                    "Leaving a modal must restore the previous valid choice");
                yield return null;
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator SettingsRefreshPreservesSelectionAndSubtitleSize()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetModal(true);
                ui.AddModalButton("字幕开关", () => { });
                ui.AddModalButton("字幕字号 125%", () => { });
                EventSystem.current.SetSelectedGameObject(ui.ModalList.GetChild(1).gameObject);
                var index = ui.SelectedModalActionIndex();
                Assert.AreEqual(1, index);
                ui.SetModal(true);
                ui.ClearModal();
                ui.AddModalButton("字幕开关", () => { });
                ui.AddModalButton("字幕字号 125% ✓", () => { });
                ui.FocusModalAction(index);
                Assert.AreSame(ui.ModalList.GetChild(3).gameObject,
                    EventSystem.current.currentSelectedGameObject,
                    "Rebuilt settings should preserve the focused action");
                ui.SetSubtitleOptions(true, 1.5f);
                Assert.GreaterOrEqual(ui.Subtitle.fontSize, 39);
                Assert.IsTrue(ui.CaptionBackdrop.activeSelf);
                Assert.IsFalse(ui.CaptionBackdrop.GetComponent<Image>().raycastTarget);
                ui.SetSubtitleOptions(false, 1f);
                Assert.IsFalse(ui.CaptionBackdrop.activeSelf);
                Assert.IsFalse(ui.Subtitle.gameObject.activeSelf);
                yield return null;
            }
            finally { if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject); }
            yield return null;
        }
    }
}
#endif

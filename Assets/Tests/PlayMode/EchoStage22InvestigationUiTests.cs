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
    public sealed class EchoStage22InvestigationUiTests
    {
        [UnityTest]
        public IEnumerator DisabledAndRebuiltClueButtonsCannotTriggerOldEvidenceActions()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetModal(true);
                int pins = 0;
                ui.AddModalText("证据槽 1 / 2", "选择两条记录");
                ui.AddModalButton("反向影子", () => pins++);
                ui.AddModalButton("尚未获取的证据", () => pins += 100, false);
                var old = ui.ModalList.GetChild(2).GetComponent<Button>();
                var locked = ui.ModalList.GetChild(3).GetComponent<Button>();
                Assert.IsFalse(locked.interactable);
                old.onClick.Invoke();
                Assert.AreEqual(1, pins);
                ui.ClearModal();
                Assert.IsFalse(old.gameObject.activeSelf,
                    "Old clue actions must retire immediately when the board rerenders");
                ui.AddModalButton("新的线索", () => pins += 10);
                yield return null;
                Assert.AreEqual(1, ui.ModalList.childCount);
                var current = ui.ModalList.GetChild(0).GetComponent<Button>();
                current.onClick.Invoke();
                Assert.AreEqual(11, pins);
                Assert.AreSame(current.gameObject,
                    EventSystem.current.currentSelectedGameObject);
            }
            finally
            {
                if (ui.Canvas != null) Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }
    }
}
#endif

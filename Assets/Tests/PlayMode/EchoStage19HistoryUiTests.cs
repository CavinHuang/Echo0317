#if UNITY_INCLUDE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Echo0317.UI;

namespace Echo0317.Tests
{
    public sealed class EchoStage19HistoryUiTests
    {
        [UnityTest]
        public IEnumerator NativeHistoryButtonOpensTranscriptInsteadOfChangingChoices()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.SetGameVisible(true);
                var header = ui.Game.transform.Find("HeaderButtons");
                Assert.NotNull(header);
                var history = header.GetChild(0).GetComponent<Button>();
                Assert.NotNull(history);
                int opens = 0;
                ui.OnHistory = () => { opens++; ui.SetModal(true); };
                history.onClick.Invoke();
                Assert.AreEqual(1, opens);
                Assert.IsTrue(ui.Modal.activeSelf);
                ui.SetModal(false);
                Assert.IsFalse(ui.Modal.activeSelf);
                yield return null;
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

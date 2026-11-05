#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Echo0317.Core;
using Echo0317.Runtime;
using Echo0317.UI;

namespace Echo0317.Tests
{
    public sealed class EchoPlaybackSmokeTests
    {
        [UnityTest]
        public IEnumerator NativeUiAndVideoDirectorCanResetAndReplayTheSameShot()
        {
            var game = GameCatalog.LoadFromStreamingAssets();
            EchoGameUI ui = null;
            GameObject owner = null;
            try
            {
                ui = new EchoGameUI();
                owner = new GameObject("Echo0317-Video-Test");
                var player = owner.AddComponent<EchoVideoDirector>();
                player.Initialize(game, ui.VideoLayer);
                Assert.NotNull(ui.Canvas);
                Assert.NotNull(ui.ContinueButton);
                var changes = 0;
                player.OnClipChange = (_) => changes++;
                player.Show("V01");
                Assert.AreEqual("V01", player.CurrentClip);
                Assert.IsTrue(player.IsPreparing);
                Assert.AreEqual(1, changes);
                yield return null;
                // Replacing a preparation with another shot must keep the second
                // shot selected and must not leak the first decoder result.
                player.Show("V02");
                Assert.AreEqual("V02", player.CurrentClip);
                Assert.IsTrue(player.IsPreparing, "A cancelled preparation must be replaced by the new request");
                Assert.AreEqual(2, changes);
                player.Show("V04");
                Assert.AreEqual("V04", player.CurrentClip);
                Assert.IsTrue(player.IsPreparing, "Three rapid selections must not reuse an uncancelled decoder");
                Assert.AreEqual(3, changes);
                player.Halt();
                Assert.AreEqual("", player.CurrentClip);
                Assert.IsFalse(player.IsPreparing, "Halt must cancel any outstanding preparation");
                player.Show("V01");
                Assert.AreEqual("V01", player.CurrentClip);
                Assert.AreEqual(4, changes, "Same clip must be requested again after return to menu");
                player.Replay();
                Assert.AreEqual(5, changes, "Replay must not be swallowed by same-clip guard");
                player.Pause();
                Assert.IsTrue(player.IsPaused, "Pause must survive a clip still preparing");
                player.Resume();
                Assert.IsFalse(player.IsPaused);
                player.Halt();
                Assert.IsFalse(player.IsPreparing);
                yield return null;
            }
            finally
            {
                if (owner != null) UnityEngine.Object.Destroy(owner);
                if (ui != null && ui.Canvas != null) UnityEngine.Object.Destroy(ui.Canvas.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator NativeMenuAndModalHaveKeyboardNavigationFocus()
        {
            var ui = new EchoGameUI();
            try
            {
                ui.ContinueButton.interactable = false;
                ui.SetGameVisible(false);
                yield return null;
                Assert.AreSame(ui.MenuActions.GetChild(1).gameObject,
                    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject,
                    "A first-time player needs a selected New Game action for keyboard/gamepad UI navigation");
                ui.SetModal(true);
                ui.ClearModal();
                ui.AddModalButton("取消", () => { });
                Assert.AreSame(ui.ModalList.GetChild(0).gameObject,
                    UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject,
                    "The first available confirmation action must receive focus");
                ui.SetModal(false);
            }
            finally
            {
                if (ui.Canvas != null) UnityEngine.Object.Destroy(ui.Canvas.gameObject);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator NativeMenusAndChoiceButtonsActuallyInvokeCallbacks()
        {
            // This UI test is deliberately save-isolated: never touch a real player's
            // autosave when the test runner happens to use a persistent Editor profile.
            var ui = new EchoGameUI();
            try
            {
                bool requestedNewGame = false;
                ui.OnNewGame = () => requestedNewGame = true;
                ui.SetGameVisible(false);
                Assert.IsTrue(ui.Menu.activeSelf);
                var newGameButton = ui.MenuActions.GetChild(1).GetComponent<UnityEngine.UI.Button>();
                Assert.NotNull(newGameButton);
                newGameButton.onClick.Invoke();
                Assert.IsTrue(requestedNewGame);
                ui.SetGameVisible(true);
                Assert.IsTrue(ui.Game.activeSelf);
                Assert.IsFalse(ui.Menu.activeSelf);
                int choiceCount = 0;
                ui.AddChoice("检查猫眼", () => choiceCount++);
                var choiceButton = ui.ChoiceList.GetChild(0).GetComponent<UnityEngine.UI.Button>();
                choiceButton.onClick.Invoke();
                Assert.AreEqual(1, choiceCount);
                ui.SetModal(true);
                Assert.IsTrue(ui.Modal.activeSelf);
                ui.ShowStartupError("story.json missing");
                Assert.IsFalse(ui.ContinueButton.interactable);
                Assert.IsFalse(ui.MenuActions.GetChild(1).GetComponent<UnityEngine.UI.Button>().interactable);
                Assert.IsTrue(ui.Menu.activeSelf);
                Assert.IsFalse(ui.Modal.activeSelf);
                yield return null;
            }
            finally
            {
                if (ui.Canvas != null) UnityEngine.Object.Destroy(ui.Canvas.gameObject);
            }
        }
    }
}
#endif

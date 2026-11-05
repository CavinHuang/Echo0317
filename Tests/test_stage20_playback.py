"""Stage 20 non-Unity contracts. The same C# playback lifecycle executes in CoreHost."""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]


class PlaybackControlsContract(unittest.TestCase):
    def test_corehost_compiles_real_playback_lifecycle_and_executes_races(self):
        project = (ROOT / 'Tools/CoreHost/Echo0317.CoreHost.csproj').read_text(encoding='utf8')
        host = (ROOT / 'Tools/CoreHost/Program.cs').read_text(encoding='utf8')
        self.assertIn('Assets/Scripts/Core/EchoPlaybackState.cs', project)
        for fragment in ('new EchoPlaybackState()', 'playback.Skip()',
                         '!playback.CanPresent(pendingToken)',
                         '!playback.Complete(replayToken)',
                         'playback.Request("V02", true)'):
            self.assertIn(fragment, host)

    def test_unity_video_director_blocks_stale_prepared_frames(self):
        video = (ROOT / 'Assets/Scripts/Runtime/EchoVideoDirector.cs').read_text(encoding='utf8')
        self.assertIn('if (!playback.CanPresent(generation)) yield break;', video)
        self.assertGreaterEqual(video.count('if (!playback.CanPresent(generation)) yield break;'), 2)
        self.assertIn('playback.Complete(playback.Generation)', video)
        self.assertIn('if (!playback.Skip()) return;', video)
        self.assertIn('CancelPendingPreparation();', video)
        self.assertIn('active = -1; // Show the story artwork', video)
        self.assertIn('ShowClip(shot, true)', video)
        self.assertIn('if (playback.IsPaused) player.Pause();', video)
        self.assertIn('HasActiveVideo && players[active].isPrepared', video)

    def test_player_shortcuts_never_advance_story(self):
        game = (ROOT / 'Assets/Scripts/Runtime/EchoGame.cs').read_text(encoding='utf8')
        self.assertIn('if(Input.GetKeyDown(KeyCode.R)){video.Replay();return;}', game)
        self.assertIn('if(Input.GetKeyDown(KeyCode.K)){video.Skip();UpdateCaption();return;}', game)
        self.assertIn('R 重播 / K 跳过画面', game)
        self.assertIn('video.Replay();CloseModal();', game)
        self.assertNotIn('CloseModal();video.Replay();', game)

    def test_native_unity_regression_tests_are_present_but_not_claimed_executed(self):
        edit = (ROOT / 'Assets/Tests/EditMode/EchoPlaybackStateTests.cs').read_text()
        play = (ROOT / 'Assets/Tests/PlayMode/EchoStage20PlaybackTests.cs').read_text()
        self.assertIn('SkippingDuringPrepareInvalidatesThatRequestAndIsIdempotent', edit)
        self.assertIn('SkipDuringDecodeIsImmediateAndDoesNotRestartTheSkippedClip', play)


if __name__ == '__main__':
    unittest.main()

using System;

namespace Echo0317.Core
{
    /// <summary>
    /// Player intent for one movie plate. This class has no UnityEngine
    /// dependency, so cancellation/skip races can be executed by CoreHost
    /// before an activated Unity Editor is available.
    /// The decoder must check the request token again before revealing a frame.
    /// </summary>
    public sealed class EchoPlaybackState
    {
        int generation;
        string clipId = "";
        bool finished;
        bool paused;

        public int Generation => generation;
        public string ClipId => clipId;
        public bool IsFinished => finished;
        public bool IsPaused => paused;

        /// <returns>True when a new decoder request is required.</returns>
        public bool Request(string clip, bool forceReplay = false)
        {
            if (string.IsNullOrWhiteSpace(clip))
                throw new ArgumentException("A video request needs a clip ID.", nameof(clip));
            if (!forceReplay && clip == clipId) return false;
            generation++;
            clipId = clip;
            finished = false;
            // Do not clear pause: selecting a shot inside an open modal must
            // not emit a frame of audible playback before the modal closes.
            return true;
        }

        public bool CanPresent(int requestToken) =>
            requestToken == generation && !finished && clipId.Length != 0;

        /// <returns>True exactly once for the current, presented request.</returns>
        public bool Complete(int requestToken)
        {
            if (!CanPresent(requestToken)) return false;
            finished = true;
            return true;
        }

        /// <summary>
        /// Skip works even while video is being prepared. Invalidate the
        /// pending token so a late frame-ready callback cannot revive it.
        /// </summary>
        public bool Skip()
        {
            if (finished || clipId.Length == 0) return false;
            finished = true;
            generation++;
            return true;
        }

        public void Pause() => paused = true;
        public void Resume() => paused = false;

        /// <summary>Clear both decoder intent and modal pause on a real scene exit.</summary>
        public void Halt()
        {
            generation++;
            clipId = "";
            finished = false;
            paused = false;
        }
    }
}

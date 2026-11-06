using System;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>
    /// Accessibility-first subtitle policy for preview videos and static
    /// placeholder scenes. The subtitle of a previous film is NEVER used
    /// while its replacement is still preparing a fresh decoder frame.
    /// </summary>
    public static class EchoSubtitlePresentation
    {
        public static string Select(bool enabled, bool preparing,
            bool hasActiveMovie, string activeTimedCue, string[] storyLines)
        {
            if (!enabled) return "";
            // A film preparing/first-frame pending is not a ready scene.
            // Show no false subtitle from either a prior movie or the ending.
            if (preparing) return "";
            if (hasActiveMovie)
                return activeTimedCue ?? "";
            // Static fallback, decoded movie end or manually skipped plate:
            // surface the actual current narrative, not a previous clip cue.
            var lines = storyLines ?? Array.Empty<string>();
            return lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l)) ?? "";
        }
    }
}

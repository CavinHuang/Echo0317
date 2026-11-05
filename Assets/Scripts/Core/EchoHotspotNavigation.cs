using System;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>Deterministic directions for mouse-free hotspot exploration.</summary>
    public enum EchoHotspotDirection { Up, Down, Left, Right }

    /// <summary>
    /// Independent of Unity's EventSystem/CanvasScaleFactor. The same authored
    /// normalized coordinates guide every gamepad/keyboard focus movement.
    /// </summary>
    public static class EchoHotspotNavigation
    {
        public static string Next(HotspotDefinition[] hotspots, string current,
            EchoHotspotDirection direction)
        {
            if (hotspots == null || hotspots.Length == 0) return null;
            var origin = hotspots.FirstOrDefault(h => h != null && h.id == current);
            if (origin == null) return null;
            float centerX = origin.x + origin.w / 2f;
            float centerY = origin.y + origin.h / 2f;
            string best = null;
            float bestScore = float.MaxValue;
            foreach (var candidate in hotspots)
            {
                if (candidate == null || candidate.id == origin.id) continue;
                float dx = candidate.x + candidate.w / 2f - centerX;
                float dy = candidate.y + candidate.h / 2f - centerY;
                float forward, sideways;
                switch (direction)
                {
                    case EchoHotspotDirection.Right: forward = dx; sideways = dy; break;
                    case EchoHotspotDirection.Left: forward = -dx; sideways = dy; break;
                    case EchoHotspotDirection.Up: forward = dy; sideways = dx; break;
                    case EchoHotspotDirection.Down: forward = -dy; sideways = dx; break;
                    default: return null;
                }
                // Ignore candidates behind/orthogonal to the requested input.
                if (forward <= .0001f) continue;
                // Prefer the most aligned nearby target while avoiding a far
                // target straight ahead when a closer one is slightly diagonal.
                float score = forward + 4f * sideways * sideways / forward;
                if (score < bestScore - .00001f ||
                    (Math.Abs(score - bestScore) < .00001f &&
                     string.CompareOrdinal(candidate.id, best) < 0))
                {
                    bestScore = score;
                    best = candidate.id;
                }
            }
            return best;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace Echo0317.Core
{
    /// <summary>
    /// Original scene-specific Foley cues for the playable first-act previs.
    /// The timing is relative to the story NODE, not to a reused movie clip ID.
    /// This schedule never changes evidence, choices or save data.
    /// </summary>
    public sealed class EchoFoleyCue
    {
        public string Name;
        public float AtSeconds;
        public float DurationSeconds;
        public float Gain;
    }

    public static class EchoFirstActFoleyPlan
    {
        static EchoFoleyCue Cue(string name, float at, float duration, float gain) =>
            new EchoFoleyCue {
                Name = name, AtSeconds = at,
                DurationSeconds = duration, Gain = gain
            };

        static readonly Dictionary<string, EchoFoleyCue[]> Scenes =
            new Dictionary<string, EchoFoleyCue[]>(StringComparer.Ordinal)
            {
                { "intro", new[] { Cue("phone-buzz", .18f, .38f, .19f),
                                   Cue("metal-tick", 2.65f, .13f, .10f) } },
                { "warning", new[] { Cue("phone-buzz", .32f, .28f, .13f) } },
                { "c1_peephole", new[] { Cue("metal-tick", 1.05f, .13f, .14f),
                                         Cue("metal-tick", 3.35f, .13f, .09f) } },
                { "c1_photo", new[] { Cue("paper-rustle", .35f, .5f, .13f) } },
                { "c1_receipt", new[] { Cue("phone-buzz", .45f, .22f, .10f) } },
                { "c1_knock", new[] { Cue("door-knock", .65f, .20f, .27f),
                                     Cue("door-knock", 2.45f, .20f, .27f),
                                     Cue("door-knock", 4.82f, .20f, .32f) } },
                { "c1_password", new[] { Cue("keypad-tone", .65f, .22f, .11f) } },
                { "c1_playvoice", new[] { Cue("tape-click", .3f, .18f, .16f),
                                         Cue("tape-hiss", .8f, 1.1f, .08f) } },
                { "c1_trydoor", new[] { Cue("latch-warning", .5f, .44f, .19f) } },
                { "c1_hook", new[] { Cue("metal-tick", 2.9f, .13f, .08f) } }
            };

        public static EchoFoleyCue[] ForNode(string node)
        {
            if (string.IsNullOrEmpty(node) || !Scenes.TryGetValue(node, out var cues))
                return Array.Empty<EchoFoleyCue>();
            return cues.Select(c => new EchoFoleyCue {
                Name = c.Name, AtSeconds = c.AtSeconds,
                DurationSeconds = c.DurationSeconds, Gain = c.Gain
            }).ToArray();
        }

        public static bool Valid()
        {
            return Scenes.Values.All(cues => cues.Length > 0 &&
                cues.All(c => !string.IsNullOrWhiteSpace(c.Name) &&
                    c.AtSeconds >= 0 && c.DurationSeconds >= .05f &&
                    c.DurationSeconds <= 2f && c.Gain > 0 && c.Gain <= .4f) &&
                cues.Zip(cues.Skip(1), (a,b) => a.AtSeconds <= b.AtSeconds).All(v => v));
        }
    }
}

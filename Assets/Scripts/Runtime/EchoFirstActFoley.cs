using System;
using System.Collections.Generic;
using UnityEngine;
using Echo0317.Core;

namespace Echo0317.Runtime
{
    /// <summary>
    /// Small original procedural Foley cues for temporary first-act previews.
    /// Audio plays only in a scene without an approved final movie; it is NOT
    /// an attempt to replace actor dialogue or the final film sound mix.
    /// Follows pause, mute, menu exit and narration-only fallback.
    /// </summary>
    public sealed class EchoFirstActFoley : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> clips =
            new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        AudioSource output;
        EchoFoleyCue[] cues = Array.Empty<EchoFoleyCue>();
        string visitKey = "";
        int nextCue;
        float elapsed;
        bool paused, muted;

        public string CurrentVisit => visitKey;
        public int PendingCues => cues.Length - nextCue;
        public bool IsPaused => paused;

        void Awake()
        {
            output = gameObject.AddComponent<AudioSource>();
            output.playOnAwake = false;
            output.loop = false;
            output.spatialBlend = 0f;
            output.volume = .5f;
        }

        public void SetScene(string newVisitKey, string nodeId, bool hasApprovedFilmAudio)
        {
            if (newVisitKey == visitKey) return; // UI re-render is not a new shot.
            output.Stop();
            visitKey = newVisitKey ?? "";
            cues = hasApprovedFilmAudio ? Array.Empty<EchoFoleyCue>() :
                EchoFirstActFoleyPlan.ForNode(nodeId);
            nextCue = 0;
            elapsed = 0;
        }

        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (paused) output.Pause();
            else output.UnPause();
        }

        public void SetMuted(bool value)
        {
            muted = value;
            if (output != null) output.mute = muted;
        }

        public void StopScene()
        {
            output.Stop();
            cues = Array.Empty<EchoFoleyCue>();
            nextCue = 0;
            elapsed = 0;
            visitKey = "";
            paused = false;
        }

        void Update()
        {
            if (paused || nextCue >= cues.Length) return;
            elapsed += Time.unscaledDeltaTime;
            // Delayed frames cannot replay the same knock twice.
            while (nextCue < cues.Length && elapsed >= cues[nextCue].AtSeconds)
            {
                var cue = cues[nextCue++];
                if (!muted && output != null)
                    output.PlayOneShot(ClipFor(cue), cue.Gain);
            }
        }

        AudioClip ClipFor(EchoFoleyCue cue)
        {
            string key = cue.Name + ":" + cue.DurationSeconds.ToString("0.000",
                System.Globalization.CultureInfo.InvariantCulture);
            if (clips.TryGetValue(key, out var clip)) return clip;
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * cue.DurationSeconds);
            var samples = new float[count];
            uint seed = 2166136261;
            for (int i = 0; i < cue.Name.Length; i++)
                seed = (seed ^ cue.Name[i]) * 16777619;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float u = (float)i / Mathf.Max(1, count - 1);
                seed = 1664525u * seed + 1013904223u;
                float noise = ((seed >> 9) / 8388608f) * 2f - 1f;
                float sound;
                switch (cue.Name)
                {
                    case "phone-buzz":
                        sound = Mathf.Sin(2f * Mathf.PI * 155f * t) *
                            (Mathf.Sin(2f * Mathf.PI * 16f * t) > 0f ? .8f : .1f) *
                            Mathf.Pow(1f - u, .4f);
                        break;
                    case "door-knock":
                        sound = (Mathf.Sin(2f * Mathf.PI * 128f * t) * .8f + noise * .2f) *
                            Mathf.Exp(-27f * t);
                        break;
                    case "paper-rustle":
                        sound = noise * (.4f + .6f * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 7f * t))) *
                            Mathf.Sin(Mathf.PI * u);
                        break;
                    case "metal-tick":
                        sound = (Mathf.Sin(2f * Mathf.PI * 860f * t) + .25f * noise) *
                            Mathf.Exp(-40f * t);
                        break;
                    case "keypad-tone":
                        sound = .5f * (Mathf.Sin(2f * Mathf.PI * 660f * t) +
                            Mathf.Sin(2f * Mathf.PI * 880f * t)) * Mathf.Sin(Mathf.PI * u);
                        break;
                    case "tape-click":
                        sound = (noise * .6f + Mathf.Sin(2f * Mathf.PI * 310f * t) * .4f) *
                            Mathf.Exp(-30f * t);
                        break;
                    case "tape-hiss":
                        sound = noise * Mathf.Min(1f, t * 10f) *
                            Mathf.Min(1f, (cue.DurationSeconds - t) * 8f) * .24f;
                        break;
                    case "latch-warning":
                        sound = (Mathf.Sin(2f * Mathf.PI * (220f + 160f * u) * t) * .65f +
                            noise * .18f) * Mathf.Sin(Mathf.PI * u);
                        break;
                    default:
                        sound = 0f;
                        break;
                }
                samples[i] = Mathf.Clamp(sound, -1f, 1f);
            }
            clip = AudioClip.Create("EchoFoley-" + key, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clips[key] = clip;
            return clip;
        }

        void OnDestroy()
        {
            if (output != null) output.Stop();
            foreach (var clip in clips.Values)
                if (clip != null) Destroy(clip);
            clips.Clear();
        }
    }
}

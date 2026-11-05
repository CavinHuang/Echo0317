using UnityEngine;

namespace Echo0317.UI
{
    public enum EchoDossierCue { Pin, Reject, Solve }

    /// <summary>
    /// Original short UI tones synthesized in memory. No external samples,
    /// music dependencies, copyrighted sounds or network assets are required.
    /// Follows the game's existing mute setting.
    /// </summary>
    public sealed class EchoDossierSound : MonoBehaviour
    {
        AudioSource output;
        AudioClip pin, reject, solve;

        void Awake()
        {
            output = gameObject.AddComponent<AudioSource>();
            output.playOnAwake = false;
            output.spatialBlend = 0f;
            output.volume = .24f;
            pin = Tone("Evidence Pin", 470f, 640f, .10f);
            reject = Tone("Evidence Mismatch", 330f, 230f, .16f);
            solve = Tone("Deduction Confirm", 620f, 900f, .23f);
        }

        static AudioClip Tone(string name, float startHz, float endHz, float seconds)
        {
            const int frequency = 22050;
            int count = Mathf.CeilToInt(seconds * frequency);
            var samples = new float[count];
            double phase = 0;
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / Mathf.Max(1, count - 1);
                float envelope = Mathf.Sin(Mathf.PI * t);
                envelope *= envelope;
                float hz = Mathf.Lerp(startHz, endHz, t);
                phase += 2.0 * Mathf.PI * hz / frequency;
                samples[i] = Mathf.Sin((float)phase) * envelope * .55f;
            }
            var clip = AudioClip.Create(name, count, 1, frequency, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public void Play(EchoDossierCue cue, bool muted)
        {
            if (muted || output == null || !isActiveAndEnabled) return;
            var clip = cue == EchoDossierCue.Pin ? pin :
                cue == EchoDossierCue.Reject ? reject : solve;
            if (clip != null) output.PlayOneShot(clip);
        }

        void OnDestroy()
        {
            if (pin != null) Destroy(pin);
            if (reject != null) Destroy(reject);
            if (solve != null) Destroy(solve);
        }
    }
}

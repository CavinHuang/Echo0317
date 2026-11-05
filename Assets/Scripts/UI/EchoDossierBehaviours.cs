using UnityEngine;

namespace Echo0317.UI
{
    /// <summary>Stable evidence identity for restoring controller focus after a rerender.</summary>
    public sealed class EchoDossierCardKey : MonoBehaviour
    {
        public string Id;
    }

    /// <summary>
    /// A brief in-game reveal effect for newly pinned cards and deduction
    /// feedback. Uses unscaled time because the film is paused in casebook UI.
    /// </summary>
    public sealed class EchoDossierReveal : MonoBehaviour
    {
        CanvasGroup group;
        float elapsed;
        void Awake()
        {
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = .55f;
            transform.localScale = new Vector3(.992f, .992f, 1f);
        }
        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / .24f);
            var eased = 1f - (1f - t) * (1f - t);
            group.alpha = Mathf.Lerp(.55f, 1f, eased);
            var scale = Mathf.Lerp(.992f, 1f, eased);
            transform.localScale = new Vector3(scale, scale, 1f);
            if (t >= 1f) enabled = false;
        }
    }
}

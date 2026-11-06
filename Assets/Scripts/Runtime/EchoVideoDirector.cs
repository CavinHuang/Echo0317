using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Echo0317.Core;

namespace Echo0317.Runtime
{
    // Twin-player video: never expose a partly decoded frame while preparing a branch.
    // No playable asset is required: if it is missing, the game's narration stays usable.
    public sealed class EchoVideoDirector : MonoBehaviour
    {
        readonly VideoPlayer[] players = new VideoPlayer[2];
        readonly RenderTexture[] targets = new RenderTexture[2];
        readonly RawImage[] views = new RawImage[2];
        readonly CanvasGroup[] groups = new CanvasGroup[2];
        readonly Dictionary<string, Texture2D> backgroundCache = new Dictionary<string, Texture2D>();
        int active = -1;
        int preparingSlot = -1;
        Coroutine prepareRoutine;
        readonly bool[] firstFrameReady = new bool[2];
        readonly string[] pendingErrors = new string[2];
        // Single authoritative intent for requests, decoder completion, skips and pauses.
        readonly EchoPlaybackState playback = new EchoPlaybackState();
        GameCatalog catalog;
        public Action<string> OnClipChange;
        public Action<string> OnClipError;
        public Action OnEnded;
        public string CurrentClip => playback.ClipId;
        public bool HasActiveVideo => active >= 0 && preparingSlot < 0 && !playback.IsFinished;
        public bool IsPreparing => preparingSlot >= 0;
        public bool IsPaused => playback.IsPaused;
        public bool IsPlaying => HasActiveVideo && players[active].isPlaying;
        public bool IsFallback => playback.ClipId.Length > 0 && playback.IsFinished && active < 0;
        public string LastFallbackReason { get; private set; } = "";
        public string SceneArtId { get; private set; } = "";
        public double CurrentTime => HasActiveVideo ? players[active].time : 0;
        public float Speed { get; private set; } = 1f;
        public bool Muted { get; private set; } = false;
        public RawImage StaticImage { get; private set; }
        public void Initialize(GameCatalog game, RectTransform videoLayer)
        {
            catalog = game;
            StaticImage = NewFullImage(videoLayer, "Artwork");
            StaticImage.color = Color.white;
            for (int i = 0; i < 2; i++)
            {
                var image = NewFullImage(videoLayer, "VideoCanvas-" + i);
                views[i] = image;
                groups[i] = image.gameObject.AddComponent<CanvasGroup>();
                groups[i].alpha = 0;
                var player = new GameObject("Decode-" + i).AddComponent<VideoPlayer>();
                player.transform.SetParent(transform, false);
                player.playOnAwake = false;
                player.isLooping = false;
                player.source = VideoSource.Url;
                player.renderMode = VideoRenderMode.RenderTexture;
                player.audioOutputMode = VideoAudioOutputMode.Direct;
                player.waitForFirstFrame = true;
                player.skipOnDrop = false;
                int slotIndex = i;
                // The next frame must really have been decoded before showing its texture.
                // Prepare() alone is not a guarantee that a visible video frame exists.
                player.frameReady += (p, frame) => firstFrameReady[slotIndex] = true;
                player.errorReceived += (p, message) => {
                    if (preparingSlot >= 0 && p == players[preparingSlot])
                    {
                        pendingErrors[preparingSlot] = message;
                        return;
                    }
                    // Errors from the old held plate must not cancel the
                    // newer shot that is currently preparing.
                    if (preparingSlot < 0 && active >= 0 && p == players[active] &&
                        !playback.IsFinished)
                        FailCurrentRequest(message);
                };
                player.loopPointReached += (p) => {
                    // An obsolete plate can complete while a replacement is
                    // being decoded. Never fire a completion for that old shot.
                    if (preparingSlot < 0 && active >= 0 && p == players[active] &&
                        playback.Complete(playback.Generation))
                    {
                        players[active].Pause();
                        OnEnded?.Invoke();
                    }
                };
                targets[i] = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32) { name = "EchoFilm-" + i };
                targets[i].Create();
                player.targetTexture = targets[i];
                image.texture = targets[i];
                players[i] = player;
            }
        }
        static RawImage NewFullImage(RectTransform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            var t = go.GetComponent<RectTransform>(); t.SetParent(parent, false);
            t.anchorMin = Vector2.zero; t.anchorMax = Vector2.one;
            t.offsetMin = Vector2.zero; t.offsetMax = Vector2.zero;
            var image = go.GetComponent<RawImage>(); image.raycastTarget = false; return image;
        }
        public void SetSceneArt(string sceneId)
        {
            SceneArtId = sceneId ?? "";
            // Never leave the *previous* act's image visible if the requested
            // scene asset is missing or corrupt. The dark VideoStage is safe.
            StaticImage.texture = null;
            StaticImage.color = Color.clear;
            if (string.IsNullOrEmpty(sceneId)) return;
            if (backgroundCache.TryGetValue(sceneId, out var cached))
            {
                StaticImage.texture = cached;
                StaticImage.color = Color.white;
                return;
            }
            var path = Path.Combine(Application.streamingAssetsPath, "Art", sceneId + ".png");
            if (!File.Exists(path)) return;
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(path)))
            {
                Destroy(texture);
                return;
            }
            texture.name = "Scene-" + sceneId;
            backgroundCache[sceneId] = texture;
            StaticImage.texture = texture;
            StaticImage.color = Color.white;
        }
        string ResolveApproved(string shotId)
        {
            // Only a director-approved final asset can suppress placeholder Foley.
            if (!catalog.ShotTasks.TryGetValue(shotId, out var task) ||
                task.status != "approved_final" || string.IsNullOrEmpty(task.path))
                return null;
            var rel = task.path.Replace('\\', '/');
            if (Path.IsPathRooted(rel) || rel.Contains("..")) return null;
            var path = Path.Combine(Application.streamingAssetsPath, "Media",
                "Final", Path.GetFileName(rel));
            return File.Exists(path) ? path : null;
        }
        public bool HasApprovedFinal(string shotId) => ResolveApproved(shotId) != null;
        public bool HasPlayableAsset(string shotId) => Resolve(shotId) != null;
        string Resolve(string shotId)
        {
            if (string.IsNullOrWhiteSpace(shotId)) return null;
            var final = ResolveApproved(shotId);
            if (final != null) return final;
            var previous = Path.Combine(Application.streamingAssetsPath,
                "Media", "Previs", shotId + ".mp4");
            return File.Exists(previous) ? previous : null;
        }
        void CancelPendingPreparation()
        {
            if (prepareRoutine != null)
            {
                StopCoroutine(prepareRoutine);
                prepareRoutine = null;
            }
            if (preparingSlot >= 0 && preparingSlot != active)
            {
                // A fresh branch chosen during preparation must stop the *old*
                // decoder before the slot is reused. Checking a generation ID at
                // the end of a coroutine does not prevent a decoder race by itself.
                var pending = players[preparingSlot];
                pending.sendFrameReadyEvents = false;
                pending.Stop();
                groups[preparingSlot].alpha = 0;
            }
            preparingSlot = -1;
        }
        public void Show(string clipId) => ShowClip(clipId, false);

        void ShowClip(string clipId, bool forceReplay)
        {
            if (string.IsNullOrWhiteSpace(clipId)) { Halt(); return; }
            if (!playback.Request(clipId, forceReplay)) return;
            // Cancel previous frame-ready continuations before queuing a new
            // decoder. Request generation is checked again before presentation.
            CancelPendingPreparation();
            int generation = playback.Generation;
            var file = Resolve(clipId);
            LastFallbackReason = "";
            OnClipChange?.Invoke(clipId);
            if (file == null)
            {
                // Missing previews are a supported INTERNAL-ALPHA condition.
                // Mark the scene as a static fallback without erasing its clip
                // identity, so subtitles/status and Replay can still refer to it.
                RetireToFallback("视频资源尚未制作：" + clipId);
                return;
            }
            // Old shot is held visually but silent while the new shot prepares.
            if (active >= 0) players[active].Pause();
            preparingSlot = active == 0 ? 1 : 0;
            prepareRoutine = StartCoroutine(PrepareAndSwitch(file, generation, preparingSlot));
        }
        static void ClearDecodeTarget(RenderTexture target)
        {
            var before = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                GL.Clear(true, true, Color.black);
            }
            finally { RenderTexture.active = before; }
        }
        IEnumerator PrepareAndSwitch(string file, int generation, int next)
        {
            var player = players[next];
            firstFrameReady[next] = false;
            pendingErrors[next] = null;
            player.Stop();
            ClearDecodeTarget(targets[next]);
            player.url = new Uri(file).AbsoluteUri;
            player.playbackSpeed = Speed;
            player.SetDirectAudioMute(0, true);
            player.sendFrameReadyEvents = true;
            player.Prepare();
            float elapsed = 0;
            while (!player.isPrepared && string.IsNullOrEmpty(pendingErrors[next]) &&
                   playback.CanPresent(generation) && elapsed < 10f)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!playback.CanPresent(generation)) yield break;
            if (!player.isPrepared || !string.IsNullOrEmpty(pendingErrors[next]))
            {
                FailCurrentRequest(string.IsNullOrEmpty(pendingErrors[next])
                    ? "视频预备超时：" + Path.GetFileName(file)
                    : "视频解码错误：" + pendingErrors[next]);
                yield break;
            }
            // Start muted/hidden, so the first visual frame cannot expose an
            // uninitialized or stale RenderTexture. The old movie stays visible.
            player.Play();
            float firstFrameTimeout = 0;
            while (!firstFrameReady[next] && string.IsNullOrEmpty(pendingErrors[next]) &&
                   playback.CanPresent(generation) && firstFrameTimeout < 3f)
            {
                firstFrameTimeout += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!playback.CanPresent(generation)) yield break;
            player.sendFrameReadyEvents = false;
            if (!firstFrameReady[next] || !string.IsNullOrEmpty(pendingErrors[next]))
            {
                FailCurrentRequest(string.IsNullOrEmpty(pendingErrors[next])
                    ? "未取得影片首帧：" + Path.GetFileName(file)
                    : "首帧解码失败：" + pendingErrors[next]);
                yield break;
            }
            int old = active;
            if (old >= 0) players[old].Pause();
            active = next;
            groups[next].alpha = 1f;
            if (old >= 0) groups[old].alpha = 0f;
            player.SetDirectAudioMute(0, Muted);
            if (playback.IsPaused) player.Pause();
            if (preparingSlot == next)
            {
                preparingSlot = -1;
                prepareRoutine = null;
            }
        }
        void RetireToFallback(string reason)
        {
            playback.Skip(); // invalidate pending decoder tokens; retain clip ID
            prepareRoutine = null;
            preparingSlot = -1;
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                {
                    players[i].sendFrameReadyEvents = false;
                    players[i].Stop();
                }
                if (groups[i] != null) groups[i].alpha = 0f;
            }
            active = -1;
            LastFallbackReason = reason;
            OnClipError?.Invoke(reason);
        }
        void FailCurrentRequest(string reason)
        {
            // Retire a failed decode but keep the current scene identity for
            // accessibility subtitles and a valid manual Replay request.
            RetireToFallback(reason);
        }
        public void Replay()
        {
            // Replaying a finished or skipped clip must be an explicit new request.
            var shot = playback.ClipId;
            if (string.IsNullOrEmpty(shot)) return;
            ShowClip(shot, true);
        }
        public void Pause()
        {
            playback.Pause();
            if (active >= 0) players[active].Pause();
        }
        public void Resume()
        {
            playback.Resume();
            if (HasActiveVideo && players[active].isPrepared) players[active].Play();
        }
        public void Skip()
        {
            // Skip applies to the requested plate, not the previous held decoder.
            // It must also work during Prepare()/first-frame wait.
            if (!playback.Skip()) return;
            CancelPendingPreparation();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null) players[i].Pause();
                if (groups[i] != null) groups[i].alpha = 0f;
            }
            active = -1; // Show the story artwork, not a frozen/skipped frame.
            OnEnded?.Invoke();
        }
        public void SetMuted(bool muted)
        {
            Muted = muted;
            for (int i = 0; i < players.Length; i++)
                if (players[i] != null)
                    // A hidden decoder must stay silent even when the player unmutes.
                    players[i].SetDirectAudioMute(0, Muted || i != active);
        }
        public void SetSpeed(float value)
        {
            Speed = Mathf.Clamp(value, 0.75f, 1.5f);
            foreach (var p in players) if (p != null) p.playbackSpeed = Speed;
        }
        public void Halt()
        {
            playback.Halt();
            LastFallbackReason = "";
            CancelPendingPreparation();
            // Returning to the menu invalidates all pending video callbacks.
            for (int i = 0; i < 2; i++)
            {
                if (players[i] != null) { players[i].sendFrameReadyEvents = false; players[i].Stop(); }
                if (groups[i] != null) groups[i].alpha = 0;
            }
            active = -1;
        }
        void OnDestroy()
        {
            foreach (var rt in targets) if (rt != null) { rt.Release(); Destroy(rt); }
            foreach (var texture in backgroundCache.Values) Destroy(texture);
        }
    }
}

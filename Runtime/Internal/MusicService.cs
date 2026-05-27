using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Audio.Internal
{
    /// <summary>
    /// Default <see cref="IMusicService"/> implementation. Owns two pre-allocated <see cref="AudioSource"/>
    /// objects under a <c>DontDestroyOnLoad</c> root GameObject and crossfades between them.
    /// Only one music cue is active at a time.
    /// </summary>
    internal sealed class MusicService : IMusicService
    {
        private const string ROOT_GO_NAME = "[GDF.Audio] MusicRoot";
        private const string SOURCE_GO_NAME = "MusicSource";
        private const string LOG_NULL_CUE = "[GDF.Audio.MusicService] Play called with null cue.";
        private const string LOG_NO_CLIP = "[GDF.Audio.MusicService] Cue has no clip.";

        private readonly IAudioSettingsService _settings;
        private readonly GameObject _root;
        private AudioSource _active;
        private AudioSource _idle;
        private MusicCue _current;
        private CancellationTokenSource _fadeCts;
        private bool _isPaused;
        private bool _disposed;

        /// <inheritdoc />
        public MusicCue Current => _current;
        /// <inheritdoc />
        public bool IsPlaying => _current != null;
        /// <inheritdoc />
        public event Action CurrentChanged;

        public MusicService(IAudioSettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _root = new GameObject(ROOT_GO_NAME);
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(_root);
            }

            _active = CreateSource();
            _idle = CreateSource();

            _settings.VolumeChanged += OnSettingsChanged;
            _settings.MutedChanged += OnSettingsChanged;
        }

        /// <inheritdoc />
        public void Play(MusicCue cue)
        {
            if (_disposed) return;
            if (cue == null)
            {
                Debug.LogWarning(LOG_NULL_CUE);
                return;
            }
            if (cue.Clip == null)
            {
                Debug.LogWarning(LOG_NO_CLIP);
                return;
            }
            if (ReferenceEquals(cue, _current)) return;

            CancelOngoingFade();
            CancellationTokenSource cts = new();
            _fadeCts = cts;

            MusicCue previous = _current;
            _current = cue;
            _isPaused = false;

            _idle.clip = cue.Clip;
            _idle.outputAudioMixerGroup = cue.MixerGroup;
            _idle.loop = cue.Loop;
            _idle.volume = 0f;
            _idle.Play();

            CrossfadeAsync(cts.Token, previous, cue).Forget();

            CurrentChanged?.Invoke();
        }

        /// <inheritdoc />
        public void Stop(float? fadeOverrideSeconds = null)
        {
            if (_disposed) return;
            if (_current == null) return;

            MusicCue stopping = _current;
            CancelOngoingFade();
            CancellationTokenSource cts = new();
            _fadeCts = cts;

            _current = null;
            _isPaused = false;

            float duration = fadeOverrideSeconds ?? stopping.FadeOutSeconds;
            duration = Mathf.Max(0f, duration);
            FadeOutAsync(cts.Token, _active, duration).Forget();

            CurrentChanged?.Invoke();
        }

        /// <inheritdoc />
        public void Pause()
        {
            if (_disposed) return;
            if (_isPaused) return;
            _active.Pause();
            _idle.Pause();
            _isPaused = true;
        }

        /// <inheritdoc />
        public void Resume()
        {
            if (_disposed) return;
            if (!_isPaused) return;
            _active.UnPause();
            _idle.UnPause();
            _isPaused = false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _settings.VolumeChanged -= OnSettingsChanged;
            _settings.MutedChanged -= OnSettingsChanged;

            CancelOngoingFade();

            if (_root != null) DestroyRoot(_root);
        }

        private static void DestroyRoot(GameObject root)
        {
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(root);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private async UniTask CrossfadeAsync(CancellationToken ct, MusicCue previous, MusicCue next)
        {
            AudioSource fadeOut = _active;
            AudioSource fadeIn = _idle;

            float fadeOutDuration = previous != null ? Mathf.Max(0f, previous.FadeOutSeconds) : 0f;
            float fadeInDuration = Mathf.Max(0f, next.FadeInSeconds);
            float maxDuration = Mathf.Max(fadeOutDuration, fadeInDuration);

            float startTime = Time.unscaledTime;
            float fadeOutFrom = fadeOut.volume;
            float fadeInTarget = ComputeTargetVolume(next);

            try
            {
                if (maxDuration > 0f)
                {
                    while (true)
                    {
                        float elapsed = Time.unscaledTime - startTime;
                        if (elapsed >= maxDuration) break;

                        float outT = fadeOutDuration > 0f ? Mathf.Clamp01(elapsed / fadeOutDuration) : 1f;
                        float inT = fadeInDuration > 0f ? Mathf.Clamp01(elapsed / fadeInDuration) : 1f;

                        fadeOut.volume = Mathf.Lerp(fadeOutFrom, 0f, outT);
                        fadeIn.volume = Mathf.Lerp(0f, fadeInTarget, inT);

                        await UniTask.Yield(PlayerLoopTiming.Update, ct);
                    }
                }

                fadeOut.Stop();
                fadeOut.clip = null;
                fadeIn.volume = fadeInTarget;
                (_active, _idle) = (fadeIn, fadeOut);
                ClearFadeCts(ct);
            }
            catch (OperationCanceledException)
            {
                // Fade was superseded by another Play/Stop call; new fade owns volume from here.
            }
        }

        private async UniTask FadeOutAsync(CancellationToken ct, AudioSource source, float duration)
        {
            try
            {
                if (duration <= 0f)
                {
                    source.Stop();
                    source.clip = null;
                    ClearFadeCts(ct);
                    return;
                }

                float startTime = Time.unscaledTime;
                float startVolume = source.volume;

                while (true)
                {
                    float elapsed = Time.unscaledTime - startTime;
                    if (elapsed >= duration) break;
                    float t = Mathf.Clamp01(elapsed / duration);
                    source.volume = Mathf.Lerp(startVolume, 0f, t);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                source.Stop();
                source.clip = null;
                ClearFadeCts(ct);
            }
            catch (OperationCanceledException)
            {
                // Fade was superseded.
            }
        }

        private void ClearFadeCts(CancellationToken ct)
        {
            if (_fadeCts != null && _fadeCts.Token == ct)
            {
                _fadeCts.Dispose();
                _fadeCts = null;
            }
        }

        private void OnSettingsChanged(EAudioBus bus)
        {
            if (bus != EAudioBus.Music && bus != EAudioBus.Master) return;
            if (_current == null) return;
            if (_fadeCts != null) return; // Fade owns volume during transition.
            _active.volume = ComputeTargetVolume(_current);
        }

        private float ComputeTargetVolume(MusicCue cue)
        {
            return _settings.GetEffectiveVolume(EAudioBus.Music) * Mathf.Clamp01(cue.Volume);
        }

        private void CancelOngoingFade()
        {
            if (_fadeCts == null) return;
            _fadeCts.Cancel();
            _fadeCts.Dispose();
            _fadeCts = null;
        }

        private AudioSource CreateSource()
        {
            GameObject go = new(SOURCE_GO_NAME);
            go.transform.SetParent(_root.transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }
    }
}

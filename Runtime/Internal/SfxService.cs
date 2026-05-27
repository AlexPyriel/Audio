using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio.Internal
{
    /// <summary>
    /// Default <see cref="ISfxService"/> implementation. Owns a pool of pre-allocated
    /// <see cref="AudioSource"/> objects under a <c>DontDestroyOnLoad</c> root GameObject.
    /// Cooldown and max-voices are tracked by cue reference; reuse cue values across calls.
    /// </summary>
    internal sealed class SfxService : ISfxService
    {
        private const int INITIAL_POOL_SIZE = 16;
        private const string ROOT_GO_NAME = "[GDF.Audio] SfxRoot";
        private const string POOLED_GO_NAME = "SfxSource";
        private const string LOG_NULL_CUE = "[GDF.Audio.SfxService] Play called with null cue.";
        private const string LOG_NO_CLIPS = "[GDF.Audio.SfxService] Cue has no clips.";
        private const string LOG_NULL_CLIP = "[GDF.Audio.SfxService] Selected clip is null.";

        private readonly IAudioSettingsService _settings;
        private readonly GameObject _root;
        private readonly List<AudioSource> _pool;
        private readonly List<ActivePlayback> _active;
        private readonly Dictionary<SfxCue, float> _lastPlayTime;
        private bool _disposed;

        public SfxService(IAudioSettingsService settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));

            _root = new GameObject(ROOT_GO_NAME);
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(_root);
            }

            _pool = new List<AudioSource>(INITIAL_POOL_SIZE);
            _active = new List<ActivePlayback>();
            _lastPlayTime = new Dictionary<SfxCue, float>();

            for (int i = 0; i < INITIAL_POOL_SIZE; i++)
            {
                _pool.Add(CreatePooledSource());
            }

            _settings.VolumeChanged += OnSettingsChanged;
            _settings.MutedChanged += OnSettingsChanged;
        }

        /// <inheritdoc />
        public void Play(SfxCue cue)
        {
            PlayInternal(cue, hasPosition: false, worldPosition: Vector3.zero);
        }

        /// <inheritdoc />
        public void Play(SfxCue cue, Vector3 worldPosition)
        {
            PlayInternal(cue, hasPosition: true, worldPosition: worldPosition);
        }

        /// <inheritdoc />
        public void Stop(SfxCue cue)
        {
            if (_disposed) return;
            if (cue == null) return;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_active[i].Cue, cue)) continue;
                AudioSource source = _active[i].Source;
                source.Stop();
                ReleaseToPool(source);
                _active.RemoveAt(i);
            }
        }

        /// <inheritdoc />
        public void StopAll()
        {
            if (_disposed) return;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                AudioSource source = _active[i].Source;
                source.Stop();
                ReleaseToPool(source);
            }
            _active.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _settings.VolumeChanged -= OnSettingsChanged;
            _settings.MutedChanged -= OnSettingsChanged;

            for (int i = 0; i < _active.Count; i++)
            {
                AudioSource source = _active[i].Source;
                if (source != null) source.Stop();
            }
            _active.Clear();
            _pool.Clear();
            _lastPlayTime.Clear();

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

        private void PlayInternal(SfxCue cue, bool hasPosition, Vector3 worldPosition)
        {
            if (_disposed) return;
            if (cue == null)
            {
                Debug.LogWarning(LOG_NULL_CUE);
                return;
            }
            if (cue.Clips == null || cue.Clips.Length == 0)
            {
                Debug.LogWarning(LOG_NO_CLIPS);
                return;
            }

            CleanupFinished();

            if (cue.CooldownSeconds > 0f)
            {
                if (_lastPlayTime.TryGetValue(cue, out float lastPlay))
                {
                    if (Time.unscaledTime - lastPlay < cue.CooldownSeconds) return;
                }
            }

            if (cue.MaxVoices > 0)
            {
                int activeCount = 0;
                for (int i = 0; i < _active.Count; i++)
                {
                    if (ReferenceEquals(_active[i].Cue, cue)) activeCount++;
                }
                if (activeCount >= cue.MaxVoices) return;
            }

            AudioClip clip = cue.Clips[UnityEngine.Random.Range(0, cue.Clips.Length)];
            if (clip == null)
            {
                Debug.LogWarning(LOG_NULL_CLIP);
                return;
            }

            AudioSource source = AcquireSource();
            ConfigureSource(source, cue, clip, hasPosition, worldPosition);
            source.Play();

            _active.Add(new ActivePlayback(source, cue));
            _lastPlayTime[cue] = Time.unscaledTime;
        }

        private void CleanupFinished()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                SfxCue cue = _active[i].Cue;
                AudioSource source = _active[i].Source;
                if (cue.Loop) continue;
                if (source.isPlaying) continue;
                ReleaseToPool(source);
                _active.RemoveAt(i);
            }
        }

        private AudioSource AcquireSource()
        {
            if (_pool.Count > 0)
            {
                int last = _pool.Count - 1;
                AudioSource src = _pool[last];
                _pool.RemoveAt(last);
                return src;
            }
            return CreatePooledSource();
        }

        private void ReleaseToPool(AudioSource source)
        {
            if (source == null) return;
            source.clip = null;
            source.outputAudioMixerGroup = null;
            source.loop = false;
            source.spatialBlend = 0f;
            source.transform.localPosition = Vector3.zero;
            _pool.Add(source);
        }

        private AudioSource CreatePooledSource()
        {
            GameObject go = new(POOLED_GO_NAME);
            go.transform.SetParent(_root.transform);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            return src;
        }

        private void ConfigureSource(AudioSource source, SfxCue cue, AudioClip clip, bool hasPosition, Vector3 worldPosition)
        {
            source.clip = clip;
            float busVol = _settings.GetEffectiveVolume(EAudioBus.Sfx);
            source.volume = busVol * Mathf.Clamp01(cue.Volume);
            source.pitch = UnityEngine.Random.Range(cue.PitchRange.x, cue.PitchRange.y);
            source.outputAudioMixerGroup = cue.MixerGroup;
            source.priority = Mathf.Clamp(cue.Priority, 0, 255);
            source.spatialBlend = Mathf.Clamp01(cue.SpatialBlend);
            source.loop = cue.Loop;
            if (hasPosition)
            {
                source.transform.position = worldPosition;
            }
            else
            {
                source.transform.localPosition = Vector3.zero;
            }
        }

        private void OnSettingsChanged(EAudioBus bus)
        {
            if (bus != EAudioBus.Sfx && bus != EAudioBus.Master) return;
            float busVol = _settings.GetEffectiveVolume(EAudioBus.Sfx);
            for (int i = 0; i < _active.Count; i++)
            {
                _active[i].Source.volume = busVol * Mathf.Clamp01(_active[i].Cue.Volume);
            }
        }

        private readonly struct ActivePlayback
        {
            public readonly AudioSource Source;
            public readonly SfxCue Cue;

            public ActivePlayback(AudioSource source, SfxCue cue)
            {
                Source = source;
                Cue = cue;
            }
        }
    }
}

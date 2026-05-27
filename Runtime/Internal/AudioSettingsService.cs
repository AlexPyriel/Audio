using System;
using LocalDataStorage;
using UnityEngine;

namespace Audio.Internal
{
    /// <summary>
    /// Default <see cref="IAudioSettingsService"/> implementation backed by <see cref="ILocalDataStorage"/>.
    /// Stored volumes are linear in the <c>[0, 1]</c> range; <see cref="GetEffectiveVolume"/> applies a
    /// perceptual <c>linear^2</c> curve and the master bus cascade.
    /// </summary>
    internal sealed class AudioSettingsService : IAudioSettingsService
    {
        private readonly ILocalDataStorage _storage;
        private AudioSettingsData _data;

        /// <inheritdoc />
        public event Action<EAudioBus> VolumeChanged;
        /// <inheritdoc />
        public event Action<EAudioBus> MutedChanged;

        public AudioSettingsService(ILocalDataStorage storage)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));

            if (!_storage.TryLoad(out AudioSettingsData loaded) || loaded == null)
            {
                _data = new AudioSettingsData();
                _storage.Save(_data);
            }
            else
            {
                _data = loaded;
            }
        }

        /// <inheritdoc />
        public float GetVolume(EAudioBus bus)
        {
            switch (bus)
            {
                case EAudioBus.Master: return _data.MasterVolume;
                case EAudioBus.Music: return _data.MusicVolume;
                case EAudioBus.Sfx: return _data.SfxVolume;
                default: throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            }
        }

        /// <inheritdoc />
        public void SetVolume(EAudioBus bus, float volume)
        {
            float clamped = Mathf.Clamp01(volume);
            bool changed;
            switch (bus)
            {
                case EAudioBus.Master:
                    changed = !Mathf.Approximately(_data.MasterVolume, clamped);
                    _data.MasterVolume = clamped;
                    break;
                case EAudioBus.Music:
                    changed = !Mathf.Approximately(_data.MusicVolume, clamped);
                    _data.MusicVolume = clamped;
                    break;
                case EAudioBus.Sfx:
                    changed = !Mathf.Approximately(_data.SfxVolume, clamped);
                    _data.SfxVolume = clamped;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            }
            if (!changed) return;
            _storage.Save(_data);
            VolumeChanged?.Invoke(bus);
        }

        /// <inheritdoc />
        public bool IsMuted(EAudioBus bus)
        {
            switch (bus)
            {
                case EAudioBus.Master: return _data.MasterMuted;
                case EAudioBus.Music: return _data.MusicMuted;
                case EAudioBus.Sfx: return _data.SfxMuted;
                default: throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            }
        }

        /// <inheritdoc />
        public void SetMuted(EAudioBus bus, bool muted)
        {
            bool changed;
            switch (bus)
            {
                case EAudioBus.Master:
                    changed = _data.MasterMuted != muted;
                    _data.MasterMuted = muted;
                    break;
                case EAudioBus.Music:
                    changed = _data.MusicMuted != muted;
                    _data.MusicMuted = muted;
                    break;
                case EAudioBus.Sfx:
                    changed = _data.SfxMuted != muted;
                    _data.SfxMuted = muted;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            }
            if (!changed) return;
            _storage.Save(_data);
            MutedChanged?.Invoke(bus);
        }

        /// <inheritdoc />
        public float GetEffectiveVolume(EAudioBus bus)
        {
            float master = _data.MasterMuted ? 0f : ApplyCurve(_data.MasterVolume);
            if (bus == EAudioBus.Master) return master;

            float busLinear;
            bool busMuted;
            switch (bus)
            {
                case EAudioBus.Music:
                    busLinear = _data.MusicVolume;
                    busMuted = _data.MusicMuted;
                    break;
                case EAudioBus.Sfx:
                    busLinear = _data.SfxVolume;
                    busMuted = _data.SfxMuted;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bus), bus, null);
            }
            if (busMuted) return 0f;
            return master * ApplyCurve(busLinear);
        }

        /// <inheritdoc />
        public void ResetToDefaults()
        {
            AudioSettingsData defaults = new();
            bool changedMasterVol = !Mathf.Approximately(_data.MasterVolume, defaults.MasterVolume);
            bool changedMusicVol = !Mathf.Approximately(_data.MusicVolume, defaults.MusicVolume);
            bool changedSfxVol = !Mathf.Approximately(_data.SfxVolume, defaults.SfxVolume);
            bool changedMasterMute = _data.MasterMuted != defaults.MasterMuted;
            bool changedMusicMute = _data.MusicMuted != defaults.MusicMuted;
            bool changedSfxMute = _data.SfxMuted != defaults.SfxMuted;

            _data = defaults;
            _storage.Save(_data);

            if (changedMasterVol) VolumeChanged?.Invoke(EAudioBus.Master);
            if (changedMusicVol) VolumeChanged?.Invoke(EAudioBus.Music);
            if (changedSfxVol) VolumeChanged?.Invoke(EAudioBus.Sfx);
            if (changedMasterMute) MutedChanged?.Invoke(EAudioBus.Master);
            if (changedMusicMute) MutedChanged?.Invoke(EAudioBus.Music);
            if (changedSfxMute) MutedChanged?.Invoke(EAudioBus.Sfx);
        }

        private static float ApplyCurve(float linear)
        {
            return linear * linear;
        }
    }
}

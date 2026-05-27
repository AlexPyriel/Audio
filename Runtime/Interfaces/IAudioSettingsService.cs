using System;

namespace Audio
{
    /// <summary>
    /// Stores and exposes per-bus audio settings (volume and mute) persisted through
    /// <see cref="LocalDataStorage.ILocalDataStorage"/>. Settings are auto-saved on every mutation.
    /// </summary>
    /// <remarks>
    /// Volumes are stored as linear values in the <c>[0, 1]</c> range. The master bus volume and mute state
    /// cascade into the music and SFX bus values exposed through <see cref="GetEffectiveVolume(EAudioBus)"/>.
    /// </remarks>
    public interface IAudioSettingsService
    {
        /// <summary>
        /// Gets the stored linear volume scalar for the bus. Independent of mute state and of the master cascade.
        /// </summary>
        /// <param name="bus">Target bus.</param>
        /// <returns>Linear volume in the <c>[0, 1]</c> range.</returns>
        public float GetVolume(EAudioBus bus);

        /// <summary>
        /// Sets the linear volume for the bus and persists the change. Raises <see cref="VolumeChanged"/>
        /// for the bus when the value actually changes.
        /// </summary>
        /// <param name="bus">Target bus.</param>
        /// <param name="volume">New linear volume; clamped to the <c>[0, 1]</c> range.</param>
        public void SetVolume(EAudioBus bus, float volume);

        /// <summary>
        /// Gets whether the bus is muted. Mute state is preserved independently of the stored volume.
        /// </summary>
        /// <param name="bus">Target bus.</param>
        /// <returns><see langword="true"/> when the bus is muted; otherwise <see langword="false"/>.</returns>
        public bool IsMuted(EAudioBus bus);

        /// <summary>
        /// Sets the mute flag for the bus and persists the change. Raises <see cref="MutedChanged"/>
        /// for the bus when the value actually changes.
        /// </summary>
        /// <param name="bus">Target bus.</param>
        /// <param name="muted">New mute state.</param>
        public void SetMuted(EAudioBus bus, bool muted);

        /// <summary>
        /// Gets the effective playback volume for the bus after applying mute and the master bus cascade.
        /// </summary>
        /// <param name="bus">Target bus.</param>
        /// <returns>Effective linear volume in the <c>[0, 1]</c> range. Returns <c>0</c> when the bus or the
        /// master bus is muted.</returns>
        public float GetEffectiveVolume(EAudioBus bus);

        /// <summary>
        /// Raised when the stored volume of any bus changes. The argument identifies which bus changed.
        /// </summary>
        public event Action<EAudioBus> VolumeChanged;

        /// <summary>
        /// Raised when the mute flag of any bus changes. The argument identifies which bus changed.
        /// </summary>
        public event Action<EAudioBus> MutedChanged;

        /// <summary>
        /// Resets every bus to its default volume (<c>1</c>) and clears every mute flag. Persists the change
        /// and raises the corresponding events for buses whose values actually changed.
        /// </summary>
        public void ResetToDefaults();
    }
}

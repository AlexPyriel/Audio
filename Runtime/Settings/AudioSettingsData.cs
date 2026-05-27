using System;
using LocalDataStorage;

namespace Audio
{
    /// <summary>
    /// Persisted audio settings payload stored through <see cref="ILocalDataStorage"/>.
    /// Master, music, and SFX buses each keep an independent volume scalar and mute flag.
    /// </summary>
    [Serializable]
    public sealed class AudioSettingsData : LocalData
    {
        /// <summary>
        /// Master volume scalar in the <c>[0, 1]</c> range, applied on top of music and SFX bus volumes.
        /// </summary>
        public float MasterVolume = 1f;

        /// <summary>
        /// Music bus volume scalar in the <c>[0, 1]</c> range.
        /// </summary>
        public float MusicVolume = 1f;

        /// <summary>
        /// SFX bus volume scalar in the <c>[0, 1]</c> range.
        /// </summary>
        public float SfxVolume = 1f;

        /// <summary>
        /// Whether the master bus is muted. Mute state is preserved independently of <see cref="MasterVolume"/>
        /// so the user-facing volume slider can be restored when unmuted.
        /// </summary>
        public bool MasterMuted;

        /// <summary>
        /// Whether the music bus is muted. Mute state is preserved independently of <see cref="MusicVolume"/>.
        /// </summary>
        public bool MusicMuted;

        /// <summary>
        /// Whether the SFX bus is muted. Mute state is preserved independently of <see cref="SfxVolume"/>.
        /// </summary>
        public bool SfxMuted;
    }
}

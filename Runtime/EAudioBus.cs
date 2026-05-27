namespace Audio
{
    /// <summary>
    /// Identifies an audio bus exposed by the settings service. The master bus volume and mute state
    /// cascade into the effective volumes of the music and SFX buses.
    /// </summary>
    public enum EAudioBus
    {
        /// <summary>
        /// Master bus. Its volume and mute state cascade into the music and SFX bus effective volumes.
        /// </summary>
        Master = 0,

        /// <summary>
        /// Music bus. Routes music cues played through <see cref="IMusicService"/>.
        /// </summary>
        Music = 1,

        /// <summary>
        /// SFX bus. Routes sound effect cues played through <see cref="ISfxService"/>.
        /// </summary>
        Sfx = 2,
    }
}

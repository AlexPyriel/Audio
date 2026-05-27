using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    /// <summary>
    /// Resolved sound effect cue passed to <c>ISfxService</c> for playback.
    /// </summary>
    /// <remarks>
    /// Catalog ownership and clip resolution stay in the host project. Build a cue once and
    /// pass it to the service; the package keeps no opinion on how cues are stored.
    /// </remarks>
    [Serializable]
    public class SfxCue
    {
        /// <summary>
        /// Candidate clips for the cue. The player picks one at random for each playback.
        /// </summary>
        public AudioClip[] Clips;

        /// <summary>
        /// Linear playback volume scalar in the <c>[0, 1]</c> range, applied on top of the SFX bus volume.
        /// </summary>
        [Range(0f, 1f)]
        public float Volume = 1f;

        /// <summary>
        /// Inclusive minimum (<c>x</c>) and maximum (<c>y</c>) pitch multipliers sampled randomly on each playback.
        /// </summary>
        public Vector2 PitchRange = new Vector2(1f, 1f);

        /// <summary>
        /// Optional mixer group the cue is routed to. When <see langword="null"/>, the cue plays
        /// directly through <c>AudioSource.volume</c> without mixer routing.
        /// </summary>
        public AudioMixerGroup MixerGroup;

        /// <summary>
        /// Minimum interval in seconds between two consecutive playbacks of the cue.
        /// <c>0</c> disables cooldown.
        /// </summary>
        public float CooldownSeconds;

        /// <summary>
        /// Maximum simultaneous voices for the cue. <c>0</c> means unlimited.
        /// </summary>
        public int MaxVoices;

        /// <summary>
        /// Unity voice-stealing priority. Lower values are less likely to be culled by the engine when the
        /// platform voice budget is exhausted.
        /// </summary>
        [Range(0, 255)]
        public int Priority = 128;

        /// <summary>
        /// Blend between 2D (<c>0</c>) and 3D positional (<c>1</c>) playback. Positional playback uses the
        /// world position passed to <c>ISfxService.Play</c>.
        /// </summary>
        [Range(0f, 1f)]
        public float SpatialBlend;

        /// <summary>
        /// Whether the cue loops until explicitly stopped through the returned <c>SfxHandle</c>.
        /// </summary>
        public bool Loop;
    }
}

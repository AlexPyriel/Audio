using System;
using UnityEngine;

namespace Audio
{
    /// <summary>
    /// Plays resolved sound effect cues. Catalog ownership and clip resolution stay in the host project;
    /// build a cue once and pass it to the service for playback.
    /// </summary>
    public interface ISfxService : IDisposable
    {
        /// <summary>
        /// Plays the cue with 2D positioning. Respects the cue's volume, pitch range, mixer group,
        /// cooldown, max-voices, priority, spatial blend, and loop settings.
        /// </summary>
        /// <param name="cue">Resolved cue value to play.</param>
        public void Play(SfxCue cue);

        /// <summary>
        /// Plays the cue at the specified world position. Use this overload for 3D positional playback
        /// when the cue's <see cref="SfxCue.SpatialBlend"/> is greater than zero.
        /// </summary>
        /// <param name="cue">Resolved cue value to play.</param>
        /// <param name="worldPosition">World-space position the audio source is placed at.</param>
        public void Play(SfxCue cue, Vector3 worldPosition);

        /// <summary>
        /// Stops every currently active playback of the cue. Commonly used to cancel looped cues such as
        /// engine hums or ambient layers.
        /// </summary>
        /// <param name="cue">Resolved cue value whose active playbacks should be stopped.</param>
        public void Stop(SfxCue cue);

        /// <summary>
        /// Stops every currently active SFX playback regardless of cue.
        /// </summary>
        public void StopAll();
    }
}

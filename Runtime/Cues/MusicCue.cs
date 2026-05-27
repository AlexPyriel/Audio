using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Audio
{
    /// <summary>
    /// Resolved music cue passed to <c>IMusicService</c> for playback.
    /// </summary>
    /// <remarks>
    /// Catalog ownership and clip resolution stay in the host project. Build a cue once and
    /// pass it to the service; the package keeps no opinion on how cues are stored.
    /// </remarks>
    [Serializable]
    public class MusicCue
    {
        /// <summary>
        /// Music clip to play.
        /// </summary>
        public AudioClip Clip;

        /// <summary>
        /// Linear playback volume scalar in the <c>[0, 1]</c> range, applied on top of the music bus volume.
        /// </summary>
        [Range(0f, 1f)]
        public float Volume = 1f;

        /// <summary>
        /// Optional mixer group the cue is routed to. When <see langword="null"/>, the cue plays
        /// directly through <c>AudioSource.volume</c> without mixer routing.
        /// </summary>
        public AudioMixerGroup MixerGroup;

        /// <summary>
        /// Fade-in duration in seconds applied when this cue becomes the active music track.
        /// <c>0</c> starts the cue at full volume immediately.
        /// </summary>
        public float FadeInSeconds;

        /// <summary>
        /// Fade-out duration in seconds applied when this cue is replaced or explicitly stopped.
        /// <c>0</c> stops the cue immediately.
        /// </summary>
        public float FadeOutSeconds;

        /// <summary>
        /// Whether the music clip loops continuously. <see langword="true"/> for background music;
        /// set <see langword="false"/> for one-shot stingers, intros, and outros.
        /// </summary>
        public bool Loop = true;
    }
}

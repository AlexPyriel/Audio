using System;

namespace Audio
{
    /// <summary>
    /// Plays resolved music cues with crossfade transitions, looping support for background tracks,
    /// and pause/resume control. Only a single music cue is active at a time.
    /// </summary>
    public interface IMusicService : IDisposable
    {
        /// <summary>
        /// Gets the currently active music cue, or <see langword="null"/> when no music is playing.
        /// </summary>
        public MusicCue Current { get; }

        /// <summary>
        /// Gets whether music is currently playing or transitioning. <see langword="true"/> during fade-in,
        /// steady playback, and fade-out.
        /// </summary>
        public bool IsPlaying { get; }

        /// <summary>
        /// Raised when <see cref="Current"/> changes due to a new cue starting, the active cue stopping,
        /// or replacement of the active cue.
        /// </summary>
        public event Action CurrentChanged;

        /// <summary>
        /// Plays the cue, crossfading from the currently active cue using its fade settings.
        /// Passing the cue that is already <see cref="Current"/> is a no-op; call <see cref="Stop"/>
        /// first to force a restart.
        /// </summary>
        /// <param name="cue">Resolved cue value to play.</param>
        public void Play(MusicCue cue);

        /// <summary>
        /// Stops the active music with the cue's fade-out duration. Pass <paramref name="fadeOverrideSeconds"/>
        /// to override the fade duration for this stop only.
        /// </summary>
        /// <param name="fadeOverrideSeconds">Optional fade-out override in seconds. <see langword="null"/> uses
        /// the cue's configured <see cref="MusicCue.FadeOutSeconds"/>.</param>
        public void Stop(float? fadeOverrideSeconds = null);

        /// <summary>
        /// Pauses the active music in place. No-op when nothing is currently playing.
        /// </summary>
        public void Pause();

        /// <summary>
        /// Resumes paused music from the position it was paused at. No-op when music is not paused.
        /// </summary>
        public void Resume();
    }
}

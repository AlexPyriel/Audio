using Audio.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Audio.Tests.EditMode
{
    /// <summary>
    /// Smoke tests for <see cref="MusicService"/> covering construction, invalid input handling, and disposal.
    /// Crossfade timing requires PlayMode tests and is not covered here.
    /// </summary>
    public sealed class MusicServiceSmokeTests
    {
        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.That(() => new MusicService(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_ValidSettings_DoesNotThrow()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            music.Dispose();
        }

        [Test]
        public void Play_NullCue_LogsWarning()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            try
            {
                LogAssert.Expect(LogType.Warning, "[GDF.Audio.MusicService] Play called with null cue.");
                music.Play((MusicCue)null);
            }
            finally
            {
                music.Dispose();
            }
        }

        [Test]
        public void Play_CueWithNullClip_LogsWarning()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            try
            {
                MusicCue cue = new MusicCue { Clip = null };
                LogAssert.Expect(LogType.Warning, "[GDF.Audio.MusicService] Cue has no clip.");
                music.Play(cue);
            }
            finally
            {
                music.Dispose();
            }
        }

        [Test]
        public void Stop_BeforePlay_NoOp()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            try
            {
                Assert.That(() => music.Stop(), Throws.Nothing);
                Assert.That(music.IsPlaying, Is.False);
            }
            finally
            {
                music.Dispose();
            }
        }

        [Test]
        public void Pause_BeforePlay_NoOp()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            try
            {
                Assert.That(() => music.Pause(), Throws.Nothing);
            }
            finally
            {
                music.Dispose();
            }
        }

        [Test]
        public void Resume_WithoutPause_NoOp()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            try
            {
                Assert.That(() => music.Resume(), Throws.Nothing);
            }
            finally
            {
                music.Dispose();
            }
        }

        [Test]
        public void Dispose_Idempotent()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            MusicService music = new MusicService(settings);
            music.Dispose();
            Assert.That(() => music.Dispose(), Throws.Nothing);
        }
    }
}

using Audio.Internal;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Audio.Tests.EditMode
{
    /// <summary>
    /// Smoke tests for <see cref="SfxService"/> covering construction, invalid input handling, and disposal.
    /// Behavior involving <c>Time</c>, <c>AudioSource</c> playback state, and the pool requires PlayMode tests.
    /// </summary>
    public sealed class SfxServiceSmokeTests
    {
        [Test]
        public void Constructor_NullSettings_Throws()
        {
            Assert.That(() => new SfxService(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_ValidSettings_DoesNotThrow()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            sfx.Dispose();
        }

        [Test]
        public void Play_NullCue_LogsWarning()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            try
            {
                LogAssert.Expect(LogType.Warning, "[GDF.Audio.SfxService] Play called with null cue.");
                sfx.Play((SfxCue)null);
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [Test]
        public void Play_CueWithNullClips_LogsWarning()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            try
            {
                SfxCue cue = new SfxCue { Clips = null };
                LogAssert.Expect(LogType.Warning, "[GDF.Audio.SfxService] Cue has no clips.");
                sfx.Play(cue);
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [Test]
        public void Play_CueWithEmptyClips_LogsWarning()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            try
            {
                SfxCue cue = new SfxCue { Clips = new AudioClip[0] };
                LogAssert.Expect(LogType.Warning, "[GDF.Audio.SfxService] Cue has no clips.");
                sfx.Play(cue);
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [Test]
        public void Stop_NullCue_DoesNotThrow()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            try
            {
                Assert.That(() => sfx.Stop((SfxCue)null), Throws.Nothing);
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [Test]
        public void StopAll_DoesNotThrow_WhenIdle()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            try
            {
                Assert.That(() => sfx.StopAll(), Throws.Nothing);
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [Test]
        public void Dispose_Idempotent()
        {
            AudioSettingsService settings = new AudioSettingsService(new InMemoryLocalDataStorage());
            SfxService sfx = new SfxService(settings);
            sfx.Dispose();
            Assert.That(() => sfx.Dispose(), Throws.Nothing);
        }
    }
}

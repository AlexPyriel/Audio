using Audio.Internal;
using NUnit.Framework;
using UnityEngine;

namespace Audio.Tests.EditMode
{
    /// <summary>
    /// Verifies that <see cref="SfxService"/> and <see cref="MusicService"/> copy the cue's
    /// <c>IgnoreListenerPause</c> flag onto the <see cref="AudioSource"/> that plays it.
    /// </summary>
    public sealed class IgnoreListenerPauseTests
    {
        private const int SAMPLE_RATE = 44100;

        private AudioClip _clip;
        private AudioSettingsService _settings;

        [SetUp]
        public void SetUp()
        {
            _clip = AudioClip.Create("IgnoreListenerPauseTestClip", SAMPLE_RATE, 1, SAMPLE_RATE, false);
            _settings = new AudioSettingsService(new InMemoryLocalDataStorage());
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_clip);
        }

        [Test]
        public void Defaults_SfxFreezes_MusicIgnoresPause()
        {
            Assert.That(new SfxCue().IgnoreListenerPause, Is.False);
            Assert.That(new MusicCue().IgnoreListenerPause, Is.True);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void SfxPlay_CopiesFlagToSource(bool ignoreListenerPause)
        {
            SfxService sfx = new SfxService(_settings);
            try
            {
                sfx.Play(new SfxCue { Clips = new[] { _clip }, IgnoreListenerPause = ignoreListenerPause });

                Assert.That(FindSourcePlaying(_clip).ignoreListenerPause, Is.EqualTo(ignoreListenerPause));
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [Test]
        public void SfxPlay_ReusedSource_TakesFlagFromNewCue()
        {
            SfxService sfx = new SfxService(_settings);
            try
            {
                sfx.Play(new SfxCue { Clips = new[] { _clip }, IgnoreListenerPause = true });
                sfx.StopAll();
                sfx.Play(new SfxCue { Clips = new[] { _clip }, IgnoreListenerPause = false });

                Assert.That(FindSourcePlaying(_clip).ignoreListenerPause, Is.False);
            }
            finally
            {
                sfx.Dispose();
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void MusicPlay_CopiesFlagToSource(bool ignoreListenerPause)
        {
            MusicService music = new MusicService(_settings);
            try
            {
                music.Play(new MusicCue { Clip = _clip, IgnoreListenerPause = ignoreListenerPause });

                Assert.That(FindSourcePlaying(_clip).ignoreListenerPause, Is.EqualTo(ignoreListenerPause));
            }
            finally
            {
                music.Dispose();
            }
        }

        private static AudioSource FindSourcePlaying(AudioClip clip)
        {
            AudioSource[] sources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (AudioSource source in sources)
            {
                if (source.clip == clip) return source;
            }

            Assert.Fail($"No AudioSource holds clip '{clip.name}'.");
            return null;
        }
    }
}

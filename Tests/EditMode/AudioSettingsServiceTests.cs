using System;
using Audio.Internal;
using NUnit.Framework;

namespace Audio.Tests.EditMode
{
    /// <summary>
    /// Behavior tests for <see cref="AudioSettingsService"/>.
    /// </summary>
    public sealed class AudioSettingsServiceTests
    {
        private const float CURVE_TOLERANCE = 0.0001f;

        [Test]
        public void Constructor_NullStorage_Throws()
        {
            Assert.That(() => new AudioSettingsService(null), Throws.ArgumentNullException);
        }

        [Test]
        public void Constructor_EmptyStorage_UsesDefaults()
        {
            InMemoryLocalDataStorage storage = new InMemoryLocalDataStorage();
            AudioSettingsService service = new AudioSettingsService(storage);

            Assert.That(service.GetVolume(EAudioBus.Master), Is.EqualTo(1f));
            Assert.That(service.GetVolume(EAudioBus.Music), Is.EqualTo(1f));
            Assert.That(service.GetVolume(EAudioBus.Sfx), Is.EqualTo(1f));
            Assert.That(service.IsMuted(EAudioBus.Master), Is.False);
            Assert.That(service.IsMuted(EAudioBus.Music), Is.False);
            Assert.That(service.IsMuted(EAudioBus.Sfx), Is.False);
        }

        [Test]
        public void Constructor_EmptyStorage_PersistsDefaultsOnce()
        {
            InMemoryLocalDataStorage storage = new InMemoryLocalDataStorage();
            AudioSettingsService service = new AudioSettingsService(storage);
            Assert.That(storage.SaveCount, Is.EqualTo(1));
            Assert.That(storage.LastSaved, Is.InstanceOf<AudioSettingsData>());
        }

        [Test]
        public void Constructor_NonEmptyStorage_LoadsStoredValues()
        {
            InMemoryLocalDataStorage storage = new InMemoryLocalDataStorage();
            AudioSettingsData seed = new AudioSettingsData
            {
                MasterVolume = 0.3f,
                MusicVolume = 0.7f,
                SfxVolume = 0.5f,
                MasterMuted = true,
                MusicMuted = false,
                SfxMuted = true,
            };
            storage.Save(seed);
            int saveCountBefore = storage.SaveCount;

            AudioSettingsService service = new AudioSettingsService(storage);

            Assert.That(service.GetVolume(EAudioBus.Master), Is.EqualTo(0.3f));
            Assert.That(service.GetVolume(EAudioBus.Music), Is.EqualTo(0.7f));
            Assert.That(service.GetVolume(EAudioBus.Sfx), Is.EqualTo(0.5f));
            Assert.That(service.IsMuted(EAudioBus.Master), Is.True);
            Assert.That(service.IsMuted(EAudioBus.Music), Is.False);
            Assert.That(service.IsMuted(EAudioBus.Sfx), Is.True);
            Assert.That(storage.SaveCount, Is.EqualTo(saveCountBefore), "Constructor must not re-save when storage already has data.");
        }

        [Test]
        public void SetVolume_ClampsBelowZero()
        {
            AudioSettingsService service = CreateService();
            service.SetVolume(EAudioBus.Music, -0.5f);
            Assert.That(service.GetVolume(EAudioBus.Music), Is.EqualTo(0f));
        }

        [Test]
        public void SetVolume_ClampsAboveOne()
        {
            AudioSettingsService service = CreateService();
            service.SetVolume(EAudioBus.Music, 1.5f);
            Assert.That(service.GetVolume(EAudioBus.Music), Is.EqualTo(1f));
        }

        [Test]
        public void SetVolume_RoundTripsInRange()
        {
            AudioSettingsService service = CreateService();
            service.SetVolume(EAudioBus.Sfx, 0.42f);
            Assert.That(service.GetVolume(EAudioBus.Sfx), Is.EqualTo(0.42f).Within(0.0001f));
        }

        [Test]
        public void SetVolume_FiresVolumeChanged_OnlyOnChange()
        {
            AudioSettingsService service = CreateService();
            int count = 0;
            EAudioBus? lastBus = null;
            service.VolumeChanged += bus => { count++; lastBus = bus; };

            service.SetVolume(EAudioBus.Music, 1f); // default, no change
            Assert.That(count, Is.EqualTo(0));

            service.SetVolume(EAudioBus.Music, 0.5f);
            Assert.That(count, Is.EqualTo(1));
            Assert.That(lastBus, Is.EqualTo(EAudioBus.Music));

            service.SetVolume(EAudioBus.Music, 0.5f); // same as current
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void SetVolume_PersistsToStorage()
        {
            InMemoryLocalDataStorage storage = new InMemoryLocalDataStorage();
            AudioSettingsService service = new AudioSettingsService(storage);
            int saveCountBefore = storage.SaveCount;

            service.SetVolume(EAudioBus.Music, 0.25f);

            Assert.That(storage.SaveCount, Is.EqualTo(saveCountBefore + 1));
            AudioSettingsData saved = (AudioSettingsData)storage.LastSaved;
            Assert.That(saved.MusicVolume, Is.EqualTo(0.25f));
        }

        [Test]
        public void SetMuted_RoundTrips()
        {
            AudioSettingsService service = CreateService();
            service.SetMuted(EAudioBus.Sfx, true);
            Assert.That(service.IsMuted(EAudioBus.Sfx), Is.True);
            service.SetMuted(EAudioBus.Sfx, false);
            Assert.That(service.IsMuted(EAudioBus.Sfx), Is.False);
        }

        [Test]
        public void SetMuted_FiresMutedChanged_OnlyOnChange()
        {
            AudioSettingsService service = CreateService();
            int count = 0;
            service.MutedChanged += bus => count++;

            service.SetMuted(EAudioBus.Master, false); // default false, no change
            Assert.That(count, Is.EqualTo(0));

            service.SetMuted(EAudioBus.Master, true);
            Assert.That(count, Is.EqualTo(1));

            service.SetMuted(EAudioBus.Master, true);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void GetEffectiveVolume_Master_AppliesSquareCurve()
        {
            AudioSettingsService service = CreateService();
            service.SetVolume(EAudioBus.Master, 0.5f);

            float effective = service.GetEffectiveVolume(EAudioBus.Master);
            Assert.That(effective, Is.EqualTo(0.25f).Within(CURVE_TOLERANCE));
        }

        [Test]
        public void GetEffectiveVolume_Master_ZeroWhenMuted()
        {
            AudioSettingsService service = CreateService();
            service.SetMuted(EAudioBus.Master, true);
            Assert.That(service.GetEffectiveVolume(EAudioBus.Master), Is.EqualTo(0f));
        }

        [Test]
        public void GetEffectiveVolume_Music_AppliesMasterCascade()
        {
            AudioSettingsService service = CreateService();
            service.SetVolume(EAudioBus.Master, 0.5f);
            service.SetVolume(EAudioBus.Music, 0.5f);

            // (0.5^2) * (0.5^2) = 0.0625
            float effective = service.GetEffectiveVolume(EAudioBus.Music);
            Assert.That(effective, Is.EqualTo(0.0625f).Within(CURVE_TOLERANCE));
        }

        [Test]
        public void GetEffectiveVolume_Music_ZeroWhenMasterMuted()
        {
            AudioSettingsService service = CreateService();
            service.SetMuted(EAudioBus.Master, true);
            Assert.That(service.GetEffectiveVolume(EAudioBus.Music), Is.EqualTo(0f));
        }

        [Test]
        public void GetEffectiveVolume_Music_ZeroWhenMusicMuted()
        {
            AudioSettingsService service = CreateService();
            service.SetMuted(EAudioBus.Music, true);
            Assert.That(service.GetEffectiveVolume(EAudioBus.Music), Is.EqualTo(0f));
        }

        [Test]
        public void ResetToDefaults_RestoresDefaults_AndFiresEventsOnlyForChanged()
        {
            AudioSettingsService service = CreateService();
            service.SetVolume(EAudioBus.Music, 0.3f);
            service.SetMuted(EAudioBus.Sfx, true);

            int volumeChanges = 0;
            int muteChanges = 0;
            service.VolumeChanged += _ => volumeChanges++;
            service.MutedChanged += _ => muteChanges++;

            service.ResetToDefaults();

            Assert.That(service.GetVolume(EAudioBus.Music), Is.EqualTo(1f));
            Assert.That(service.IsMuted(EAudioBus.Sfx), Is.False);
            Assert.That(volumeChanges, Is.EqualTo(1), "Only Music volume changed back to default.");
            Assert.That(muteChanges, Is.EqualTo(1), "Only Sfx mute changed back to default.");
        }

        [Test]
        public void GetVolume_InvalidBus_Throws()
        {
            AudioSettingsService service = CreateService();
            Assert.That(() => service.GetVolume((EAudioBus)99), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SetVolume_InvalidBus_Throws()
        {
            AudioSettingsService service = CreateService();
            Assert.That(() => service.SetVolume((EAudioBus)99, 0.5f), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void GetEffectiveVolume_InvalidBus_Throws()
        {
            AudioSettingsService service = CreateService();
            Assert.That(() => service.GetEffectiveVolume((EAudioBus)99), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static AudioSettingsService CreateService()
        {
            return new AudioSettingsService(new InMemoryLocalDataStorage());
        }
    }
}

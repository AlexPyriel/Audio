using Audio.Internal;
using Zenject;

namespace Audio.Installers.Zenject
{
    /// <summary>
    /// Registers GDF Audio package services in a Zenject container.
    /// </summary>
    public sealed class AudioZenjectInstaller : Installer<AudioZenjectInstaller>
    {
        /// <summary>
        /// Adds the package runtime services to the current container.
        /// </summary>
        public override void InstallBindings()
        {
            Container
                .Bind<IAudioSettingsService>()
                .To<AudioSettingsService>()
                .AsSingle();

            Container
                .Bind<ISfxService>()
                .To<SfxService>()
                .AsSingle();

            Container
                .Bind<IMusicService>()
                .To<MusicService>()
                .AsSingle();
        }
    }
}

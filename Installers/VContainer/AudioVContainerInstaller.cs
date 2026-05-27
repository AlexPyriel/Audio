using Audio.Internal;
using VContainer;
using VContainer.Unity;

namespace Audio.Installers.VContainer
{
    /// <summary>
    /// Registers GDF Audio package services in a VContainer container.
    /// </summary>
    public sealed class AudioVContainerInstaller : IInstaller
    {
        /// <summary>
        /// Adds the package runtime services to the provided container builder.
        /// </summary>
        /// <param name="builder">Container builder used for service registrations.</param>
        public void Install(IContainerBuilder builder)
        {
            builder
                .Register<AudioSettingsService>(Lifetime.Singleton)
                .As<IAudioSettingsService>();

            builder
                .Register<SfxService>(Lifetime.Singleton)
                .As<ISfxService>();

            builder
                .Register<MusicService>(Lifetime.Singleton)
                .As<IMusicService>();
        }
    }
}

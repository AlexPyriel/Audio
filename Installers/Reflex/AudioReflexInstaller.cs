using Audio.Internal;
using Reflex.Core;
using UnityEngine;

namespace Audio.Installers.Reflex
{
    /// <summary>
    /// Registers GDF Audio package services in a Reflex container.
    /// </summary>
    public sealed class AudioReflexInstaller : MonoBehaviour, IInstaller
    {
        /// <summary>
        /// Adds the package runtime services to the current Reflex container.
        /// </summary>
        /// <param name="builder">Container builder used for service registrations.</param>
        public void InstallBindings(ContainerBuilder builder)
        {
            builder.AddSingleton(
                typeof(AudioSettingsService),
                typeof(IAudioSettingsService));

            builder.AddSingleton(
                typeof(SfxService),
                typeof(ISfxService));

            builder.AddSingleton(
                typeof(MusicService),
                typeof(IMusicService));
        }
    }
}

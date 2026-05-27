# GDF Audio

Cue-based audio playback service for Unity: SFX pooling, music crossfade, and persisted audio settings.

Package name: `com.gdf.audio`  
Assembly name: `GDF.Audio`

## Features

- Public API: `ISfxService`, `IMusicService`, `IAudioSettingsService`.
- Cue-driven playback: pass a resolved `SfxCue` or `MusicCue` value and play. Catalog ownership and clip resolution stay in the host project.
- `SfxCue` supports clip variations, pitch randomization, mixer group routing, cooldown, max-voices, priority, spatial blend, and looping.
- `MusicCue` supports per-cue mixer group, symmetric fade-in/out, and loop control for background tracks or one-shot stingers.
- 2-source music crossfade orchestrated with `UniTask`.
- World-positional SFX via `Play(cue, Vector3)` overload.
- Looped SFX with explicit `Stop(SfxCue)` and `StopAll()` control.
- Optional `AudioMixer` integration: cues without a mixer group play through `AudioSource.volume` directly.
- Per-bus settings (master, music, SFX) persisted via `com.gdf.local-data-storage`, exposed through `IAudioSettingsService`.
- Perceptual `linear^2` volume curve applied on the playback path; stored volumes stay linear for UI sliders.
- DI installers for VContainer, Zenject/Extenject, and Reflex.

## Requirements

- Unity `6000.0+`
- `com.gdf.local-data-storage` `1.0.0+`
- `com.cysharp.unitask`
- One of the supported DI containers (optional, required only when using the matching installer):
  - `jp.hadashikick.vcontainer`
  - `com.mathijsbakker.extenject`
  - `com.gustavopsantos.reflex`

## Installation (UPM via Git)

Add this dependency to `Packages/manifest.json`:

```json
"com.gdf.audio": "https://github.com/AlexPyriel/Audio.git#v1.0.0"
```

For development tracking (not recommended for production), use `#main` instead of a tag.

You can also install it from Unity Package Manager:

- `Window -> Package Manager -> + -> Add package from git URL...`
- `https://github.com/AlexPyriel/Audio.git#v1.0.0`

## Usage

### 1. Register the package services

Install the package once at the composition root. The audio settings service depends on
`ILocalDataStorage` from `com.gdf.local-data-storage`, so register both installers in the
same scope.

```csharp
using Audio.Installers.VContainer;
using LocalDataStorage.Installers.VContainer;
using VContainer;
using VContainer.Unity;

public sealed class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        new LocalDataStorageVContainerInstaller().Install(builder);
        new AudioVContainerInstaller().Install(builder);
    }
}
```

For Zenject use `AudioZenjectInstaller`, for Reflex use `AudioReflexInstaller`. See the
`DI Policy` section below.

### 2. Inject the services

```csharp
using Audio;

public sealed class GameplayPresenter
{
    private readonly ISfxService _sfx;
    private readonly IMusicService _music;
    private readonly IAudioSettingsService _settings;

    public GameplayPresenter(ISfxService sfx, IMusicService music, IAudioSettingsService settings)
    {
        _sfx = sfx;
        _music = music;
        _settings = settings;
    }
}
```

### 3. Build cues

A cue is a plain `[Serializable]` value object: `SfxCue` for sound effects, `MusicCue` for
music. The package keeps no opinion on where cues live — store them in a `ConfigRepository`
asset, a `ScriptableObject` catalog, an Addressables-backed resolver, hardcoded constants,
or anywhere else that fits the host project.

```csharp
using Audio;
using UnityEngine;

public static class GameSfx
{
    public static readonly SfxCue Click = new()
    {
        Clips = new[] { /* assigned through ScriptableObject in the host project */ },
        Volume = 1f,
        PitchRange = new Vector2(0.95f, 1.05f),
        CooldownSeconds = 0.05f,
        MaxVoices = 4,
    };

    public static readonly SfxCue EngineHum = new()
    {
        Clips = new[] { /* ... */ },
        Volume = 0.6f,
        Loop = true,
    };
}

public static class GameMusic
{
    public static readonly MusicCue MainTheme = new()
    {
        Clip = /* ... */,
        Volume = 0.7f,
        FadeInSeconds = 1.5f,
        FadeOutSeconds = 1.5f,
        Loop = true,
    };
}
```

**Reuse cue instances across calls.** Cooldown, max-voices, and `Stop(SfxCue)` are tracked
by reference identity, so a `static readonly` field or a cached lookup is the expected
pattern. Constructing a new `SfxCue` per call disables cooldown/max-voices for that cue.

### 4. Play sound effects

```csharp
_sfx.Play(GameSfx.Click);                            // 2D playback
_sfx.Play(GameSfx.Explosion, transform.position);    // 3D playback when cue.SpatialBlend > 0
_sfx.Play(GameSfx.EngineHum);                        // looping cue, keeps playing
_sfx.Stop(GameSfx.EngineHum);                        // stops every active playback of the cue
_sfx.StopAll();                                      // useful at scene transitions
```

### 5. Play music

```csharp
_music.Play(GameMusic.MainTheme);    // fades in over cue.FadeInSeconds
_music.Play(GameMusic.BossTheme);    // crossfades: old cue fades out, new cue fades in
_music.Stop();                       // fades out over cue.FadeOutSeconds
_music.Stop(fadeOverrideSeconds: 0); // stops immediately, ignoring the cue's fade
_music.Pause();                      // pauses in place
_music.Resume();                     // resumes from the paused position

if (_music.IsPlaying)
{
    MusicCue current = _music.Current;
}

_music.CurrentChanged += () => Debug.Log($"Music: {_music.Current?.Clip.name ?? "(none)"}");
```

### 6. Audio settings UI

Volumes are stored linearly in the `[0, 1]` range so a UI slider can be bound directly to
`GetVolume` / `SetVolume`. The perceptual `linear^2` curve is applied internally on the
playback path through `GetEffectiveVolume`.

```csharp
musicSlider.value = _settings.GetVolume(EAudioBus.Music);
musicSlider.onValueChanged.AddListener(v => _settings.SetVolume(EAudioBus.Music, v));

masterMuteToggle.isOn = _settings.IsMuted(EAudioBus.Master);
masterMuteToggle.onValueChanged.AddListener(m => _settings.SetMuted(EAudioBus.Master, m));

resetButton.onClick.AddListener(_settings.ResetToDefaults);

_settings.VolumeChanged += bus => Debug.Log($"{bus} volume changed to {_settings.GetVolume(bus)}");
_settings.MutedChanged += bus => Debug.Log($"{bus} muted: {_settings.IsMuted(bus)}");
```

Changes are auto-saved on every mutation. SFX and music services subscribe to settings
events internally and update the volume of active sources live without restart.

### How pooling works

`ISfxService` allocates a root `[GDF.Audio] SfxRoot` GameObject (marked `DontDestroyOnLoad`)
on construction and pre-allocates 16 `AudioSource` children. `Play` acquires a source from
the pool; non-looping cues return to the pool the next time `Play` is called and the source
has finished. Looping cues stay active until `Stop(cue)` or `StopAll()` releases them. The
pool grows on demand if peak demand exceeds the initial capacity and never shrinks.

`IMusicService` allocates two `AudioSource` children under `[GDF.Audio] MusicRoot` for
crossfade between the active and the next cue.

Both root GameObjects are destroyed when the service is disposed by the DI container.

## Assembly Definition Notes

`GDF.Audio` uses `autoReferenced: true`.

In most cases, no explicit `.asmdef` reference is required.

## DI Policy

The package exposes service interfaces and keeps concrete runtime players internal.
Catalog ownership stays in the host project: build cues from whatever storage you prefer
(ConfigRepository, plain ScriptableObjects, Addressables, hardcoded constants) and pass
the resolved cue value to the package services.

Use the DI installer that matches your project's container to wire the package services.

### VContainer

Use the VContainer installer when your project uses VContainer. UPM installs are enabled automatically when the
`jp.hadashikick.vcontainer` package is present. Source-based or asset-based installs require the
`AUDIO_VCONTAINER` scripting define and a `VContainer` assembly in the project.

### Zenject

Use the Zenject installer when your project uses Zenject or Extenject. UPM installs are enabled automatically when the
`com.mathijsbakker.extenject` package is present. Source-based or asset-based installs require the
`AUDIO_ZENJECT` scripting define and a `Zenject` assembly in the project.

### Reflex

Use the Reflex installer when your project uses Reflex. UPM installs are enabled automatically when the
`com.gustavopsantos.reflex` package is present. Source-based or asset-based installs require the
`AUDIO_REFLEX` scripting define and a `Reflex` assembly in the project.

# Changelog

## [2.0.0] - 2026-11-06

Breaking release for projects on Zenject or Reflex. VContainer projects upgrade with no code changes.

### Changed

- `AudioSettingsService`, `SfxService` and `MusicService` are `public sealed`, so any DI container can bind them.
- Depends on `com.gdf.local-data-storage >= 3.0.0`.

### Removed

- Zenject installer (`AudioZenjectInstaller`) and Reflex installer (`AudioReflexInstaller`). The README section
  "Using with Zenject / Reflex" shows the replacement bindings.

## [1.1.0] - 2026-11-01

### Changed

- **Depends on `com.gdf.local-data-storage >= 2.0.0`.** The runtime API that the package uses
  (`Save<T>` / `TryLoad<T>`) is unchanged by the dependency bump; no code changes are required for
  consumers. Downstream projects that keep the default installer configuration (PlayerPrefs backend
  + AES encryption + legacy pass phrase) read existing v1 saves without migration.

### Internal

- Test fake `InMemoryLocalDataStorage` extended to the full v2 interface (slot overloads, async
  variants, events). No public API or behavior change for the Audio package itself.

## [1.0.0] - 2026-05-27

- Initial public release of the package.
- Cue-driven playback API exposed through `ISfxService` and `IMusicService`.
- Per-bus audio settings (master, music, SFX) persisted via `com.gdf.local-data-storage`, exposed through `IAudioSettingsService`.
- `SfxCue` supports clip variations, pitch randomization, mixer group routing, cooldown, max-voices, priority, spatial blend, and looping.
- `MusicCue` supports per-cue mixer group, symmetric fade-in/out, and loop control.
- Two-source music crossfade orchestrated with `UniTask`.
- World-positional SFX via `Play(cue, Vector3)` overload.
- Looped SFX with explicit `Stop(cue)` and `StopAll()` control.
- Optional `AudioMixer` integration: cues without a mixer group play through `AudioSource.volume` directly.
- Perceptual `linear²` volume curve applied through `GetEffectiveVolume`.
- DI installers for VContainer, Zenject/Extenject, and Reflex.

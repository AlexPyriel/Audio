# Changelog

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

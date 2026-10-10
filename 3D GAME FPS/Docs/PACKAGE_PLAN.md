# Package and asset plan

Goal: smoother and more responsive play (Call of Duty Mobile feel), better looking, still fast on mid-range phones.
Nothing in "Install now" is paid. Package names are the exact Unity registry IDs (checked against the registry).

## Already in the project

| Package | What we use it for |
|---|---|
| Input System 1.18 | keyboard, mouse, gamepad and touch, read directly in `InputManager` |
| Cinemachine 3.1 | FPS and TPS cameras, ADS and sprint FOV, recoil impulse |
| PrimeTween 1.4 | allocation-free UI and game-feel tweens |
| AI Navigation 2.0 | enemy NavMesh |
| Memory Profiler, Profile Analyzer | profiling on device |
| URP 17.3 | renderer, bloom, vignette, SRP Batcher |
| Adaptive Performance (built-in module in Unity 6.3) | available, not wired up yet |

## Tier 1: install now (free, official, high payoff)

| Package ID | Why | Where it plugs in |
|---|---|---|
| `com.unity.animation.rigging` 1.4 | Two Bone IK, Multi-Aim, Override Transform as real constraints with weights. Better than hand-written IK for weapon aim, hand-on-wall, foot-on-wall. | Replace the hand written parts of `ProceduralParkour` and `PlayerAnimator` IK step by step |
| `com.unity.adaptiveperformance` 6.0 + the Android provider | Reads thermal state and frame time on the phone and lowers render scale, shadow distance, particle count before the phone throttles. Keeps 60 or 120 FPS steady for longer. | Hook into `GameSettings` / `VfxQuality` (they already have quality levels) |
| `com.unity.mobile.android-logcat` 1.4 | Live logs, memory stats and screen capture from a real phone. Needed for the Android test pass. | Window > Analysis > Android Logcat |
| `com.unity.probuilder` 6.1 | Block out new arenas quickly, real collision, export to FBX. | New levels |
| `com.unity.splines` 2.9 | Patrol routes, moving platforms, rails, camera paths. | Enemy patrol, parkour course |

## Tier 2: assets from the Asset Store or free sites (pick what you like)

| Need | Options | Notes |
|---|---|---|
| Environment art | Kenney (CC0, free), Quaternius (CC0, free), Synty POLYGON packs (paid, low-poly, mobile friendly) | Keep to one style so the Mixamo-style character fits |
| More animation | Mixamo (free): strafe, rifle walk, crouch walk, climb | Only if procedural lean ever looks wrong. Re-use the `CreateFromThisModel` import setup |
| Sound effects | Sonniss GDC audio bundles (free), Freesound (check licence), Zapsplat | Our procedural SFX stay as the fallback |
| Music | Pixabay Music, Incompetech (credit required) | Add a `MusicPlayer` on the Music bus |
| VFX | Unity Particle Pack (free), KriptoFX or Hovl Studio (paid, built for mobile) | Use Particle System, not VFX Graph, on low-end phones |
| Character skins | Mixamo characters, Synty characters | Store items: reuse `WeaponVisualStyler` idea for skins |

## Tier 3: later, when the game loop is fun

| Package | Why |
|---|---|
| Addressables | download levels and skins after install, smaller first download |
| Unity Gaming Services: Authentication, Cloud Save, Leaderboards, Remote Config | cloud save, ranking, tune the difficulty curve without a new build |
| Localization | Filipino and English text |
| Unity Test Framework (installed) | play mode tests for `PlayerMotor` jump buffer and coyote time |
| Firebase Crashlytics | crash reports from players' phones |

## Things to avoid for now

* Final IK and Feel are paid and large. Animation Rigging, PrimeTween and Cinemachine already cover what we need.
* VFX Graph on mobile: needs compute shaders, breaks on older Android phones.
* Kinematic Character Controller: only worth it if the built-in CharacterController starts to jitter on slopes or moving platforms.

## Responsiveness checklist (what was done and what is next)

Done in this pass:
* Input read straight from the Input System, runs before every other script (execution order -200).
* Raw mouse look with no smoothing, joystick damping 20 -> 45.
* Constant acceleration movement (0.1 s to full speed), momentum kept in the air.
* Jump buffer 0.15 s, slide buffer 0.2 s, coyote time 0.12 s, hold for a high jump, tap for a hop.
* Slide, wall run and wall jump physics rebuilt; wall jump now really kicks off the wall.

Next:
* Real-device test: touch latency, 120 Hz, battery saver.
* Optional: Android "Optimized Frame Pacing" in Player Settings, and Adaptive Performance wiring.
* Optional: haptic feedback on land, slide and hit (a small plugin or Android vibration API).

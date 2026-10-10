# ROADMAP.md
# 🗺️ 3D GAME FPS — Feature Roadmap & Next Tasks

> **AI INSTRUCTION:**
> Reference this document to understand what features are completed, in progress, and planned for future sprints. Always confirm with the user before starting a major task from this list.

---

## 🟢 1. Implemented & Stable Systems
- [x] **Character Controller & Movement:** Complete with walking, sprinting, crouching, and camera look.
- [x] **Advanced Parkour Locomotion:** Wall-running, vaulting, mantling, double-jump, sliding, and airborne states.
- [x] **Weapon System Architecture:** Hitscan, projectile, recoil kick, sway, reload logic, bullet casing/tracers.
- [x] **Enemy AI & Spawner:** Finite State Machine (Patrol, Chase, Attack, Alert, Ragdoll death) and wave spawner.
- [x] **Combat & Events Pipeline:** Damage calculation, health system, hit registration, damage numbers, and kill feed.
- [x] **Audio & Procedural SFX:** Audio manager, player footsteps, procedural sound generation.
- [x] **Cross-Platform UI / Mobile HUD:** Desktop crosshairs, mobile touch controls (dual-stick / joystick), pause menu, settings, run summary.
- [x] **Main Menu & Test Arena Scenes:** Dedicated scenes for UI navigation and sandbox testing.

---

## 🟡 2. In Progress / Needs Polishing
- [ ] **Combat Polish & Balance:**
  - Tuning enemy attack range, damage falloff, and audio cues.
  - Adding weapon sound variety (distinct gunfire for pistols vs rifles).
- [ ] **Animation Blending:**
  - Hooking up character Mixamo animations to the Animator Controller parameters for smooth transitions between locomotion and parkour.
- [ ] **Scene Setup & Lighting:**
  - Baking lightmaps / configuring Realtime URP lighting and shadows for `TestArena.unity`.
  - Adding post-processing volumes (Bloom, Vignette, Motion Blur) via `PostFxController.cs`.

---

## 🔴 3. Future Planned Features
- [ ] **Multiplayer / Co-Op:** Utilizing Unity Multiplayer Center & Netcode for GameObjects.
- [ ] **Progression & Inventory Upgrades:** Perk selection during runs (roguelike elements via `PerkPickerUI.cs`).
- [ ] **Advanced Level Generation:** Modular arena tiles and dynamic objective placement.
- [ ] **Boss Encounters:** Specialized enemy AI with multi-phase mechanics and telegraph attacks.

# PROJECT_CONTEXT.md
# 🎮 3D GAME FPS — AI Project Context & Ground Truth

> **CRITICAL FOR ALL AI AGENTS:**
> This document contains the absolute ground truth of this project. **Do NOT hallucinate** missing systems or assume this is an empty template. A huge portion of the core architecture is already implemented, highly structured, and written in modular C# for Unity 6 (URP). Always read this file before suggesting or writing code.

---

## 1. Project Identity & Architecture
- **Engine Version:** Unity 6 (`6000.3.6f1`, changeset `bbb010bdb8a3`)
- **Render Pipeline:** Universal Render Pipeline (URP 17.3.0)
- **Input Handling:** New Unity Input System (`com.unity.inputsystem` 1.18.0)
- **Root Unity Folder:** `3D GAME FPS/` (Nested under root repo folder).
- **Primary Game Scenes:**
  - `Assets/_Project/Scenes/MainMenu.unity` — Start menu, loadout, character showcase, settings.
  - `Assets/_Project/Scenes/TestArena.unity` — Gameplay test environment with parkour objects, enemies, and shooting range.

---

## 2. Directory Structure Conventions
All custom game code and assets **MUST** strictly adhere to the `Assets/_Project/` folder layout:
```
3D GAME FPS/Assets/_Project/
├── Art/
│   ├── Animations/      # Player, Enemies, Weapons animations
│   ├── Materials/       # URP materials
│   ├── Models/          # 3D Meshes (.fbx tracked via Git LFS)
│   ├── Shaders/         # Custom ShaderGraphs / URP Shaders
│   ├── Textures/        # Textures & UI sprites
│   └── VFX/             # Visual effects, particles, and muzzle flashes
├── Audio/               # SFX, Music, and AudioMixers
├── Character/           # Main Character Models (BOY & GIRL rigs and animations)
├── Prefabs/             # Player, Enemies, Weapons, Projectiles, Managers, UI
├── Resources/           # Dynamic runtime loaded resources (e.g., VFX pools)
├── ScriptableObjects/   # Definitions & instances (Weapons, Enemies, Parkour)
├── Scripts/             # All C# source code (categorized by subsystem)
└── Settings/            # Input Actions and URP assets
```

---

## 3. Implemented Subsystems (Current State)

### A. Player & Parkour Movement
- **Locomotion:** `PlayerMotor.cs`, `PlayerController.cs`, `PlayerLook.cs`.
- **Parkour State Machine (`ParkourStateMachine.cs`):**
  - Fully implemented states: `AirborneState`, `CrouchState`, `DoubleJumpState`, `MantleState`, `RunningState`, `SlidingState`, `SprintingState`, `VaultState`, `WallJumpState`, `WallRunState`.
  - Environment detection via `ParkourDetector.cs` and `ParkourClipData.cs`.
  - Procedural smoothing: `ProceduralLocomotion.cs`, `ProceduralParkour.cs`, `SpeedLines.cs`, `FovController.cs`, `CameraModeController.cs`.

### B. Weapons & Combat
- **Base Architecture:** `WeaponBase.cs`, `HitscanWeapon.cs`, `ProjectileWeapon.cs`.
- **Handling & Feel:** `WeaponManager.cs`, `WeaponRecoil.cs`, `WeaponSway.cs`, `Bullet.cs`.
- **Damage Pipeline:** `HealthSystem.cs`, `DamageSystem.cs`, `KillFeedSystem.cs`, `CombatEvents.cs`.
- **Catalogs:** `WeaponCatalog.cs`, `ConsumableCatalog.cs`, `SkillCatalog.cs`.

### C. Enemy AI & Spawning
- **FSM-based AI:** `EnemyAI.cs`, `EnemyBase.cs`, `RagdollController.cs`, `EnemyVisuals.cs`.
- **States:** `AlertState.cs`, `AttackState.cs`, `ChaseState.cs`, `PatrolState.cs`, `DeadState.cs`.
- **Spawning:** `EnemySpawner.cs`, `DifficultyCurve.cs`.

### D. Audio & Visuals
- **Audio:** `AudioManager.cs`, `PlayerAudio.cs`, `ProceduralSfx.cs`.
- **VFX Pools:** `VfxPool.cs`, `VfxTextures.cs`.

### E. UI & HUD (Desktop & Mobile Ready)
- **HUD Systems:** `HUDManager.cs`, `AdaptiveHUDController.cs`, `RunHud.cs`, `DamageIndicator.cs`, `DamageNumbers.cs`, `HitMarkerUI.cs`, `HudJuice.cs`, `ConsumableBar.cs`.
- **Touch / Mobile:** `MobileInputUI.cs`, `FloatingJoystick.cs`, `TouchButton.cs`, `TouchLookZone.cs`, `MoveTouchZone.cs`.
- **Menus:** `MainMenuUI.cs`, `PauseMenuUI.cs`, `SettingsUI.cs`, `SettingsPanel.cs`, `PerkPickerUI.cs`, `RunResultUI.cs`.
- **Procedural UI Vector Rendering:** `VectorPanel.cs`, `VectorRing.cs`, `VectorSegmentedBar.cs`, `VectorCrosshair.cs`.

### F. Core Session & Persistence
- `GameManager.cs`, `GameSession.cs`, `GameSettings.cs`, `LevelDirector.cs`, `LevelManager.cs`, `ObjectPooler.cs`, `PlayerProfile.cs`, `PlayerStats.cs`, `RunModifiers.cs`, `SaveSystem.cs`.

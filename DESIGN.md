# Game Design Document — 3D GAME FPS

> **Status:** Early Development | **Version:** 0.1.0 | **Engine:** Unity 6 (URP)

---

## 1. Game Concept

A first-person shooter where the player navigates 3D environments, engages enemies with various weapons, and completes objectives.

### Core Pillars

- **Responsive Controls** — Tight, satisfying FPS movement and shooting
- **AI Challenge** — Enemies with believable NavMesh-driven behavior
- **Progression** — Weapons, levels, and difficulty scaling

---

## 2. Player Systems

### 2.1 Movement

| Parameter        | Value (Suggested)  |
|------------------|--------------------|
| Walk Speed       | 5 m/s              |
| Sprint Speed     | 8 m/s              |
| Jump Force       | 5 m                |
| Gravity          | -9.81 m/s²         |
| Mouse Sensitivity| Configurable       |

- First-person camera locked to the player
- CharacterController-based or Rigidbody-based (TBD)
- Sprint, crouch, and jump mechanics

### 2.2 Health System

- Player starts with **100 HP**
- Regeneration or health pickups (TBD)
- Death → respawn or game-over screen

---

## 3. Weapon System

### 3.1 Architecture

- Modular weapon system via ScriptableObject definitions
- Each weapon defines: damage, fire rate, magazine size, reload time, spread
- Weapon switching via scroll wheel or number keys

### 3.2 Initial Weapons (Planned)

| Weapon      | Type       | Damage | Fire Rate   | Magazine |
|-------------|------------|--------|-------------|----------|
| Pistol      | Semi-auto  | 15     | 3 rps       | 12       |
| Rifle       | Full-auto  | 10     | 10 rps      | 30       |
| Shotgun     | Pump       | 8 × 8  | 1 rps       | 6        |

### 3.3 Shooting Mechanics

- Raycasting for hitscan weapons
- Projectile physics for special weapons (rockets, grenades)
- Muzzle flash, tracer, and impact VFX
- Recoil with camera kick + spread increase

---

## 4. Enemy AI

### 4.1 Navigation

- Unity AI Navigation (NavMesh) for pathfinding
- Patrol → detect → chase → attack state machine

### 4.2 Behavior States

```
┌─────────┐    player detected    ┌─────────┐
│  PATROL  │ ──────────────────► │  CHASE   │
└─────────┘                      └────┬─────┘
     ▲                                │
     │ lost player                    │ in range
     │                                ▼
     │                           ┌─────────┐
     └────────────────────────── │  ATTACK  │
              target lost        └─────────┘
```

### 4.3 Enemy Types (Planned)

| Type     | HP  | Speed | Attack          |
|----------|-----|-------|-----------------|
| Grunt    | 50  | 3 m/s | Melee           |
| Soldier  | 80  | 4 m/s | Hitscan rifle   |
| Heavy    | 200 | 2 m/s | Explosive shots |

---

## 5. UI / HUD

### In-Game HUD

- Crosshair (dynamic spread feedback)
- Health bar
- Ammo counter (current / reserve)
- Damage direction indicator
- Minimap (optional)

### Menus

- **Main Menu:** Play, Settings, Quit
- **Pause Menu:** Resume, Settings, Main Menu
- **Settings:** Volume, mouse sensitivity, graphics quality
- **Game Over / Victory Screen**

---

## 6. Level Design Guidelines

- Start simple: one arena-style level for prototyping
- Use ProBuilder or modular kit for blockout
- Cover points, elevation changes, flanking routes
- Spawn points for enemies and items

---

## 7. Audio

| Category       | Examples                        |
|----------------|---------------------------------|
| Weapons        | Gunshots, reload, empty click   |
| Player         | Footsteps, jump, damage grunt   |
| Enemies        | Alert, death, patrol sounds     |
| Environment    | Ambient, doors, explosions      |
| UI             | Button clicks, notifications    |
| Music          | Menu theme, combat music        |

---

## 8. Technical Notes

- **Render Pipeline:** URP with PC and Mobile renderer configurations
- **Input:** New Input System — supports keyboard/mouse and gamepad
- **Physics:** Unity built-in 3D physics
- **AI:** NavMesh with NavMeshAgent components
- **Architecture Pattern:** Singleton managers + ScriptableObject events (recommended)

---

## 9. Milestones

| Milestone         | Target             | Status  |
|-------------------|--------------------|---------|
| Project Setup     | October 2026       | ✅ Done  |
| Player Controller | —                  | 🔲 Todo |
| Weapon Prototype  | —                  | 🔲 Todo |
| Enemy AI v1       | —                  | 🔲 Todo |
| First Playable    | —                  | 🔲 Todo |
| UI / Menus        | —                  | 🔲 Todo |
| Level Blockout    | —                  | 🔲 Todo |
| Polish & Audio    | —                  | 🔲 Todo |
| Alpha Build       | —                  | 🔲 Todo |

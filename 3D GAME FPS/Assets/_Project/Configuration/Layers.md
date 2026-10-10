# Unity Layers Configuration

This document defines the custom layers used in **3D GAME FPS**.

## Layer Assignments

| Layer # | Name          | Purpose                              |
|---------|---------------|--------------------------------------|
| 0       | Default       | Unity default                        |
| 1       | TransparentFX | Unity default                        |
| 2       | Ignore Raycast| Unity default                        |
| 3       | (unused)      |                                      |
| 4       | Water         | Unity default                        |
| 5       | UI            | Unity default                        |
| **6**   | **Player**    | Player character                     |
| **7**   | **Enemy**     | Enemy characters                     |
| **8**   | **Ground**    | Walkable surfaces                    |
| **9**   | **Wall**      | Parkour wall-runnable surfaces       |
| **10**  | **Obstacle**  | Vaultable / mantleable objects       |
| **11**  | **Projectile**| Bullets and projectiles              |
| **12**  | **Interactable**| Pickups, doors, buttons            |

## Physics Collision Matrix Notes

- `Projectile` should NOT collide with `Player` (prevent self-hits)
- `Player` should collide with `Ground`, `Wall`, `Obstacle`
- `Enemy` should collide with `Ground`, `Wall`, `Projectile`
- `Projectile` should collide with `Enemy`, `Ground`, `Wall`, `Obstacle`

## Usage in Code

Use `Constants.cs` for layer references:
```csharp
Constants.LAYER_WALL    // int 9
Constants.WallMask      // LayerMask for raycasting
```

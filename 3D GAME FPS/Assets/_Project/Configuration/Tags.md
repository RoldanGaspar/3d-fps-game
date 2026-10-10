# Unity Tags Configuration

This document defines the custom tags used in **3D GAME FPS**.

## Tag Assignments

| Tag              | Applied To                     | Purpose                          |
|------------------|--------------------------------|----------------------------------|
| `Player`         | Player root GameObject         | (Built-in) Identify player       |
| `Enemy`          | Enemy root GameObjects         | Identify enemies for targeting   |
| `WallRunnable`   | Wall surfaces                  | Parkour: enable wall running     |
| `Vaultable`      | Low obstacles                  | Parkour: enable vaulting         |
| `Mantleable`     | Ledges / high edges            | Parkour: enable mantling         |
| `WeaponPickup`   | Weapon items in world          | Identify collectible weapons     |

## Usage in Code

Use `Constants.cs` for tag references:
```csharp
if (other.CompareTag(Constants.TAG_WALL_RUNNABLE))
{
    // Start wall run
}
```

## Adding Tags in Unity Editor

1. Select any GameObject
2. In the Inspector, click the Tag dropdown → Add Tag...
3. Add each tag from the table above

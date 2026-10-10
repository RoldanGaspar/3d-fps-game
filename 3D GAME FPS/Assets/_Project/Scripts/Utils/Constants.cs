using UnityEngine;

/// <summary>
/// Global game constants for tags, layers, and gameplay values.
/// </summary>
public static class Constants
{
    // Layers
    public const int LAYER_PLAYER = 6;
    public const int LAYER_ENEMY = 7;
    public const int LAYER_GROUND = 8;
    public const int LAYER_WALL = 9;
    public const int LAYER_OBSTACLE = 10;
    public const int LAYER_PROJECTILE = 11;
    public const int LAYER_INTERACTABLE = 12;

    // Layer Masks
    public static readonly LayerMask GroundMask = 1 << LAYER_GROUND;
    public static readonly LayerMask WallMask = 1 << LAYER_WALL;
    public static readonly LayerMask ObstacleMask = 1 << LAYER_OBSTACLE;

    // Tags
    public const string TAG_PLAYER = "Player";
    public const string TAG_ENEMY = "Enemy";
    public const string TAG_WALL_RUNNABLE = "WallRunnable";
    public const string TAG_VAULTABLE = "Vaultable";
    public const string TAG_MANTLEABLE = "Mantleable";
    public const string TAG_WEAPON_PICKUP = "WeaponPickup";
}
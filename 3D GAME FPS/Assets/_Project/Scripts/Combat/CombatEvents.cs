using System;
using UnityEngine;

public enum SurfaceType
{
    Concrete,
    Metal,
    Wood,
    Flesh
}

/// <summary>
/// Optional marker to tell the combat feedback (impact particles, sounds, footsteps)
/// what a collider is made of. Unmarked colliders are treated as concrete.
/// </summary>
public class SurfaceMarker : MonoBehaviour
{
    public SurfaceType type = SurfaceType.Concrete;

    public static SurfaceType Resolve(Collider collider)
    {
        if (collider == null) return SurfaceType.Concrete;

        var marker = collider.GetComponentInParent<SurfaceMarker>();
        if (marker != null) return marker.type;

        if (collider.GetComponentInParent<IDamageable>() != null) return SurfaceType.Flesh;
        return SurfaceType.Concrete;
    }
}

public struct HitInfo
{
    public Vector3 point;
    public Vector3 normal;
    public float damage;
    public SurfaceType surface;
    /// <summary>True when the shot landed on something that takes damage.</summary>
    public bool hitTarget;
    /// <summary>True when this shot killed the target.</summary>
    public bool killed;
}

/// <summary>
/// Static event hub so weapons can announce hits without knowing about the HUD, audio or VFX.
/// </summary>
public static class CombatEvents
{
    /// <summary>Raised whenever a player shot lands (on a target or on the world).</summary>
    public static event Action<HitInfo> OnPlayerShotLanded;

    public static void RaisePlayerShotLanded(in HitInfo info)
    {
        OnPlayerShotLanded?.Invoke(info);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        OnPlayerShotLanded = null;
    }
}

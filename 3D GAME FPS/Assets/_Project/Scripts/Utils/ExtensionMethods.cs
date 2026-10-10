using UnityEngine;

/// <summary>
/// Utility extension methods for common types.
/// </summary>
public static class ExtensionMethods
{
    public static Vector3 Flat(this Vector3 v)
    {
        return new Vector3(v.x, 0f, v.z);
    }

    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
    }
}
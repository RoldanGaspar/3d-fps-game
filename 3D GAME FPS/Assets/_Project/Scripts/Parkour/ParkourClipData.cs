using UnityEngine;

/// <summary>
/// Timing data sampled from the Mixamo vault / mantle clips. The clips are scrubbed with the
/// "ParkourTime" animator parameter while the capsule follows a path computed from the real geometry,
/// so the pose and the body stay in sync for any obstacle height.
/// Values were measured in 0.1 steps of normalised clip time.
/// </summary>
public static class ParkourClipData
{
    // --- 10_Braced_Hang_To_Crouch (mantle) ---
    public const float MantleClipLength = 1.133f;

    // Body rise / forward travel the clip originally contained (hips, metres).
    private static readonly float[] MantleRise = { 0.85f, 1.04f, 1.44f, 1.89f, 2.25f, 2.48f, 2.65f, 2.76f, 2.85f, 2.85f, 2.85f };
    private static readonly float[] MantleForward = { -0.11f, -0.11f, -0.01f, 0.12f, 0.16f, 0.19f, 0.25f, 0.38f, 0.53f, 0.64f, 0.69f };

    // Height of the hands above the root (feet) in the pose, i.e. where the ledge sits relative to the body.
    private static readonly float[] MantleHandsUp = { 1.66f, 1.61f, 1.39f, 1.16f, 0.99f, 0.88f, 0.83f, 0.92f, 0.97f, 0.96f, 0.92f };

    // --- 09_Vault_Over_Box ---
    public const float VaultClipLength = 3.533f;
    public const float VaultStart = 0.12f;
    public const float VaultEnd = 0.52f;
    private static readonly float[] VaultForward = { -0.07f, 1.02f, 1.59f, 2.17f, 2.94f, 3.25f, 3.36f, 3.37f, 3.33f, 3.34f, 3.34f };

    public static float Sample(float[] table, float t)
    {
        t = Mathf.Clamp01(t) * (table.Length - 1);
        int i = Mathf.Min((int)t, table.Length - 2);
        return Mathf.Lerp(table[i], table[i + 1], t - i);
    }

    /// <summary>Progress 0..1 of the body rising from clip time a to clip time b.</summary>
    public static float MantleRiseProgress(float t, float from, float to)
    {
        float a = Sample(MantleRise, from);
        float b = Sample(MantleRise, to);
        return Mathf.Clamp01((Sample(MantleRise, t) - a) / Mathf.Max(0.01f, b - a));
    }

    public static float MantleForwardProgress(float t, float from, float to)
    {
        float a = Sample(MantleForward, from);
        float b = Sample(MantleForward, to);
        return Mathf.Clamp01((Sample(MantleForward, t) - a) / Mathf.Max(0.01f, b - a));
    }

    public static float MantleHandsHeight(float t) => Sample(MantleHandsUp, t);

    /// <summary>Normalised clip time at which the hands are 'handsUp' metres above the root.</summary>
    public static float MantleStartForHandHeight(float handsUp)
    {
        // The table decreases from 0 to ~0.6, so scan for the first crossing.
        for (int i = 0; i < 6; i++)
        {
            float a = MantleHandsUp[i], b = MantleHandsUp[i + 1];
            if (handsUp <= a && handsUp >= b)
                return Mathf.Lerp(i, i + 1, Mathf.InverseLerp(a, b, handsUp)) / 10f;
        }
        return handsUp > MantleHandsUp[0] ? 0f : 0.6f;
    }

    public static float VaultForwardProgress(float t)
    {
        float a = Sample(VaultForward, VaultStart);
        float b = Sample(VaultForward, VaultEnd);
        return Mathf.Clamp01((Sample(VaultForward, t) - a) / (b - a));
    }

    /// <summary>Smooth bump: 0 before 'a', 1 between 'b' and 'c', 0 after 'd'.</summary>
    public static float Bump(float x, float a, float b, float c, float d)
    {
        if (x <= a || x >= d) return 0f;
        if (x < b) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));
        if (x <= c) return 1f;
        return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(c, d, x));
    }
}

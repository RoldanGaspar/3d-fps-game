using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxId
{
    RifleShot,
    PistolShot,
    EnemyShot,
    ShotgunShot,
    SniperShot,
    Reload,
    Empty,
    HitMarker,
    KillConfirm,
    ImpactConcrete,
    ImpactMetal,
    ImpactWood,
    ImpactFlesh,
    Footstep,
    FootstepMetal,
    FootstepWood,
    Land,
    Slide,
    Jump,
    PlayerHurt
}

/// <summary>
/// Synthesises simple, royalty-free placeholder sound effects in code (no asset files needed).
/// Clips are generated once on first use and cached.
/// </summary>
public static class ProceduralSfx
{
    private const int SampleRate = 44100;
    private static readonly Dictionary<SfxId, AudioClip> Cache = new Dictionary<SfxId, AudioClip>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Cache.Clear();
    }

    public static SfxId FootstepFor(SurfaceType surface)
    {
        switch (surface)
        {
            case SurfaceType.Metal: return SfxId.FootstepMetal;
            case SurfaceType.Wood: return SfxId.FootstepWood;
            default: return SfxId.Footstep;
        }
    }

    public static AudioClip Get(SfxId id)
    {
        if (Cache.TryGetValue(id, out var clip) && clip != null) return clip;
        clip = Build(id);
        Cache[id] = clip;
        return clip;
    }

    private static AudioClip Build(SfxId id)
    {
        switch (id)
        {
            case SfxId.RifleShot:      return Make(id, Gunshot(0.34f, 16f, 85f, 0.55f, 1));
            case SfxId.PistolShot:     return Make(id, Gunshot(0.28f, 22f, 125f, 0.7f, 2));
            case SfxId.EnemyShot:      return Make(id, Gunshot(0.30f, 18f, 105f, 0.35f, 3));
            case SfxId.ShotgunShot:    return Make(id, Gunshot(0.45f, 11f, 70f, 1.0f, 21));
            case SfxId.SniperShot:     return Make(id, Gunshot(0.7f, 7f, 60f, 1.1f, 22));
            case SfxId.Reload:         return Make(id, Reload());
            case SfxId.Empty:          return Make(id, Click(0.07f, 1200f, 4));
            case SfxId.HitMarker:      return Make(id, Tones(0.07f, 70f, 0.45f, 1900f, 2850f));
            case SfxId.KillConfirm:    return Make(id, KillDing());
            case SfxId.ImpactConcrete: return Make(id, NoiseHit(0.18f, 28f, 0.35f, 0f, 5));
            case SfxId.ImpactMetal:    return Make(id, MetalPing());
            case SfxId.ImpactWood:     return Make(id, NoiseHit(0.16f, 26f, 0.5f, 380f, 6));
            case SfxId.ImpactFlesh:    return Make(id, NoiseHit(0.17f, 24f, 0.28f, 110f, 7));
            case SfxId.Footstep:       return Make(id, NoiseHit(0.13f, 34f, 0.3f, 140f, 8));
            case SfxId.FootstepMetal:  return Make(id, NoiseHit(0.16f, 26f, 0.22f, 820f, 15));
            case SfxId.FootstepWood:   return Make(id, NoiseHit(0.14f, 30f, 0.35f, 260f, 16));
            case SfxId.Land:           return Make(id, NoiseHit(0.28f, 14f, 0.4f, 70f, 9));
            case SfxId.Slide:          return Make(id, Slide());
            case SfxId.Jump:           return Make(id, NoiseHit(0.12f, 30f, 0.18f, 200f, 10));
            case SfxId.PlayerHurt:     return Make(id, NoiseHit(0.22f, 16f, 0.5f, 95f, 11));
            default:                   return Make(id, new float[SampleRate / 10]);
        }
    }

    private static AudioClip Make(SfxId id, float[] samples)
    {
        Normalize(samples, 0.9f);
        var clip = AudioClip.Create("sfx_" + id, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void Normalize(float[] s, float peak)
    {
        float max = 0.0001f;
        for (int i = 0; i < s.Length; i++) max = Mathf.Max(max, Mathf.Abs(s[i]));
        float g = peak / max;
        for (int i = 0; i < s.Length; i++) s[i] *= g;
    }

    // --- Building blocks ---

    /// <summary>Low-passed noise crack plus a pitch-dropping thump.</summary>
    private static float[] Gunshot(float length, float decay, float thumpHz, float crack, int seed)
    {
        var rng = new System.Random(seed);
        int n = (int)(length * SampleRate);
        var s = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Exp(-t * decay);
            float cutoff = Mathf.Lerp(0.9f, 0.08f, Mathf.Clamp01(t * 14f));
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * cutoff;
            float thumpFreq = thumpHz * Mathf.Exp(-t * 14f) + 38f;
            float thump = Mathf.Sin(2f * Mathf.PI * thumpFreq * t) * Mathf.Exp(-t * 26f);
            float click = t < 0.004f ? (float)(rng.NextDouble() * 2.0 - 1.0) * crack : 0f;
            s[i] = lp * env * 0.9f + thump * 0.9f + click;
        }
        return s;
    }

    private static float[] NoiseHit(float length, float decay, float noiseAmount, float toneHz, int seed)
    {
        var rng = new System.Random(seed);
        int n = (int)(length * SampleRate);
        var s = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Exp(-t * decay);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.35f;
            float tone = toneHz > 0f ? Mathf.Sin(2f * Mathf.PI * toneHz * t) : 0f;
            s[i] = (lp * noiseAmount * 2f + tone * (1f - noiseAmount)) * env;
        }
        return s;
    }

    private static float[] Tones(float length, float decay, float amp, float f1, float f2)
    {
        int n = (int)(length * SampleRate);
        var s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 800f);
            s[i] = (Mathf.Sin(2f * Mathf.PI * f1 * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * f2 * t)) * env * amp;
        }
        return s;
    }

    private static float[] KillDing()
    {
        int n = (int)(0.3f * SampleRate);
        var s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float f = t < 0.09f ? 1250f : 1880f;
            float local = t < 0.09f ? t : t - 0.09f;
            float env = Mathf.Exp(-local * 22f) * Mathf.Min(1f, local * 800f);
            s[i] = (Mathf.Sin(2f * Mathf.PI * f * t) + 0.35f * Mathf.Sin(2f * Mathf.PI * f * 2.01f * t)) * env;
        }
        return s;
    }

    private static float[] MetalPing()
    {
        var rng = new System.Random(12);
        int n = (int)(0.3f * SampleRate);
        var s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float ping = Mathf.Sin(2f * Mathf.PI * 2150f * t) * Mathf.Exp(-t * 16f)
                       + 0.6f * Mathf.Sin(2f * Mathf.PI * 3320f * t) * Mathf.Exp(-t * 22f)
                       + 0.4f * Mathf.Sin(2f * Mathf.PI * 5100f * t) * Mathf.Exp(-t * 30f);
            float tick = t < 0.006f ? (float)(rng.NextDouble() * 2.0 - 1.0) * 0.7f : 0f;
            s[i] = ping * 0.6f + tick;
        }
        return s;
    }

    private static float[] Click(float length, float hz, int seed)
    {
        var rng = new System.Random(seed);
        int n = (int)(length * SampleRate);
        var s = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float env = Mathf.Exp(-t * 90f);
            s[i] = ((float)(rng.NextDouble() * 2.0 - 1.0) * 0.5f + Mathf.Sin(2f * Mathf.PI * hz * t)) * env;
        }
        return s;
    }

    private static float[] Reload()
    {
        var rng = new System.Random(13);
        int n = (int)(0.9f * SampleRate);
        var s = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * 0.25f;

            float mag = 0f;
            // magazine out
            mag += ClickEnv(t, 0.05f) * (lp * 0.8f + Mathf.Sin(2f * Mathf.PI * 700f * t) * 0.5f);
            // slide of the mag back in
            float slideT = (t - 0.35f) / 0.25f;
            if (slideT > 0f && slideT < 1f) mag += lp * 0.25f * Mathf.Sin(slideT * Mathf.PI);
            // magazine seat
            mag += ClickEnv(t, 0.62f) * (lp * 1.0f + Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.6f);
            // charging handle
            mag += ClickEnv(t, 0.8f) * (lp * 0.9f + Mathf.Sin(2f * Mathf.PI * 950f * t) * 0.4f);
            s[i] = mag;
        }
        return s;
    }

    private static float ClickEnv(float t, float at)
    {
        float d = t - at;
        return d < 0f ? 0f : Mathf.Exp(-d * 70f);
    }

    private static float[] Slide()
    {
        var rng = new System.Random(14);
        int n = (int)(0.6f * SampleRate);
        var s = new float[n];
        float lp = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SampleRate;
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += (noise - lp) * Mathf.Lerp(0.05f, 0.4f, t / 0.6f);
            float env = Mathf.Sin(Mathf.Clamp01(t / 0.6f) * Mathf.PI);
            s[i] = lp * env;
        }
        return s;
    }
}

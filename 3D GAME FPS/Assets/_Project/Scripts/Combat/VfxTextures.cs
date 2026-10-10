using UnityEngine;

/// <summary>
/// Generates the small textures used by the combat VFX in code (no asset files needed):
/// soft glow, smoke puff, muzzle star and bullet hole. Built once on first use.
/// </summary>
public static class VfxTextures
{
    private static Texture2D _soft, _smoke, _star, _hole;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _soft = _smoke = _star = _hole = null;
    }

    public static Texture2D SoftCircle => _soft != null ? _soft : (_soft = Build(32, SoftPixel));
    public static Texture2D Smoke => _smoke != null ? _smoke : (_smoke = Build(64, SmokePixel));
    public static Texture2D Star => _star != null ? _star : (_star = Build(64, StarPixel));
    public static Texture2D BulletHole => _hole != null ? _hole : (_hole = Build(32, HolePixel));

    private delegate Color32 PixelFunc(float x, float y, float r);

    private static Texture2D Build(int size, PixelFunc func)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var px = new Color32[size * size];
        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - half) / half;
                float ny = (y + 0.5f - half) / half;
                px[y * size + x] = func(nx, ny, Mathf.Sqrt(nx * nx + ny * ny));
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return tex;
    }

    private static Color32 SoftPixel(float x, float y, float r)
    {
        float a = Mathf.Clamp01(1f - r);
        a = a * a * (3f - 2f * a);
        return new Color32(255, 255, 255, (byte)(a * 255f));
    }

    // cheap value noise from a hash, good enough for a smoke puff
    private static float Hash(int x, int y)
    {
        int h = x * 374761393 + y * 668265263;
        h = (h ^ (h >> 13)) * 1274126177;
        return ((h ^ (h >> 16)) & 0xFFFF) / 65535f;
    }

    private static float Noise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float fx = x - xi, fy = y - yi;
        fx = fx * fx * (3f - 2f * fx);
        fy = fy * fy * (3f - 2f * fy);
        float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
    }

    private static Color32 SmokePixel(float x, float y, float r)
    {
        float n = Noise(x * 3f + 5f, y * 3f + 5f) * 0.55f + Noise(x * 7f + 11f, y * 7f + 11f) * 0.3f + Noise(x * 15f, y * 15f) * 0.15f;
        float falloff = Mathf.Clamp01(1f - r);
        falloff = falloff * falloff * (3f - 2f * falloff);
        float a = Mathf.Clamp01(falloff * (0.35f + n * 1.1f));
        return new Color32(255, 255, 255, (byte)(a * 255f));
    }

    private static Color32 StarPixel(float x, float y, float r)
    {
        float core = Mathf.Exp(-r * r * 9f);
        float h = Mathf.Exp(-Mathf.Abs(y) * 14f) * Mathf.Exp(-Mathf.Abs(x) * 1.6f);
        float v = Mathf.Exp(-Mathf.Abs(x) * 14f) * Mathf.Exp(-Mathf.Abs(y) * 1.6f);
        // diagonal spikes
        float dx = (x + y) * 0.7071f, dy = (x - y) * 0.7071f;
        float d1 = Mathf.Exp(-Mathf.Abs(dy) * 16f) * Mathf.Exp(-Mathf.Abs(dx) * 2.4f) * 0.55f;
        float d2 = Mathf.Exp(-Mathf.Abs(dx) * 16f) * Mathf.Exp(-Mathf.Abs(dy) * 2.4f) * 0.55f;
        float a = Mathf.Clamp01(core + h + v + d1 + d2);
        a *= Mathf.Clamp01(1.4f - r);
        return new Color32(255, 255, 255, (byte)(a * 255f));
    }

    private static Color32 HolePixel(float x, float y, float r)
    {
        float edge = 0.55f + Noise(x * 5f + 3f, y * 5f + 3f) * 0.3f;
        float hole = Mathf.Clamp01((edge - r) * 6f);
        float scorch = Mathf.Clamp01(1f - Mathf.Abs(r - edge * 0.95f) * 4f) * 0.35f * Mathf.Clamp01(1.2f - r);
        float a = Mathf.Clamp01(hole + scorch);
        byte c = (byte)(hole > 0.5f ? 8 : 40);
        return new Color32(c, c, c, (byte)(a * 255f));
    }
}

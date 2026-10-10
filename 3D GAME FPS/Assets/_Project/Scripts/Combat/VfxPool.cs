using UnityEngine;

/// <summary>
/// Global VFX quality scale (particle counts). Set from the graphics settings.
/// </summary>
public static class VfxQuality
{
    public static float ParticleScale = 1f;
}

/// <summary>
/// Self-contained, allocation-free pool for combat VFX: bullet tracers, surface impact bursts, dust and
/// smoke puffs, muzzle flashes, ejected shell casings, bullet-hole decals, the muzzle flash light and
/// pooled 3D sound sources. Everything is built in code on first use, so no prefabs or scene setup are needed.
/// </summary>
public class VfxPool : MonoBehaviour
{
    private const int TracerCount = 24;
    private const int ImpactPerSurface = 10;
    private const int DecalCount = 24;
    private const int FlashCount = 6;
    private const int PuffCount = 10;
    private const int CasingCount = 10;
    private const float TracerSpeed = 320f;
    private const float TracerLength = 7f;

    private static VfxPool _instance;

    public static VfxPool Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[VfxPool]");
                _instance = go.AddComponent<VfxPool>();
            }
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }

    private struct Tracer
    {
        public LineRenderer line;
        public Vector3 start;
        public Vector3 dir;
        public float distance;
        public float travelled;
        public bool active;
    }

    private struct Casing
    {
        public Rigidbody body;
        public float despawnAt;
    }

    private Tracer[] _tracers;
    private int _nextTracer;

    // impact debris per surface + soft puff per surface
    private ParticleSystem[][] _impacts;
    private ParticleSystem[][] _impactPuffs;
    private int[] _nextImpact;

    private ParticleSystem[] _flashes;
    private ParticleSystem[] _muzzleSmoke;
    private ParticleSystem[] _dust;
    private int _nextFlash, _nextSmoke, _nextDust;

    private Casing[] _casings;
    private int _nextCasing;

    private SpriteRenderer[] _decals;
    private int _nextDecal;

    private AudioSource[] _sfxSources;
    private int _nextSfx;

    private Light _flashLight;
    private float _flashEnd;

    private Material _additive;
    private Material _alpha;
    private Material _tracerMaterial;
    private Sprite _decalSprite;

    // --- Setup ---

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        LoadMaterials();
        BuildTracers();
        BuildImpacts();
        BuildMuzzleEffects();
        BuildDust();
        BuildCasings();
        BuildDecals();
        BuildFlashLight();
        BuildSfxSources();
    }

    private void LoadMaterials()
    {
        _additive = Resources.Load<Material>("VFX/VFX_Additive");
        _alpha = Resources.Load<Material>("VFX/VFX_Alpha");

        // Fallbacks keep the effects working even if the material assets are missing.
        var fallback = Shader.Find("Sprites/Default");
        if (_additive == null) _additive = new Material(fallback);
        if (_alpha == null) _alpha = new Material(fallback);

        _tracerMaterial = new Material(_additive) { name = "Tracer" };
        _tracerMaterial.SetTexture("_BaseMap", VfxTextures.SoftCircle);
        _tracerMaterial.mainTexture = VfxTextures.SoftCircle;
    }

    private Material Textured(Material source, Texture2D texture, string name)
    {
        var m = new Material(source) { name = name };
        m.SetTexture("_BaseMap", texture);
        m.mainTexture = texture;
        return m;
    }

    // --- Public API ---

    public void SpawnTracer(Vector3 from, Vector3 to, Color color)
    {
        Vector3 delta = to - from;
        float dist = delta.magnitude;
        if (dist < 0.5f) return;

        ref Tracer t = ref _tracers[_nextTracer];
        _nextTracer = (_nextTracer + 1) % _tracers.Length;

        t.start = from;
        t.dir = delta / dist;
        t.distance = dist;
        t.travelled = 0f;
        t.active = true;
        t.line.startColor = color;
        t.line.endColor = new Color(color.r, color.g, color.b, 0f);
        t.line.enabled = true;
        UpdateTracerPositions(ref t);
    }

    public void SpawnImpact(Vector3 point, Vector3 normal, SurfaceType surface, bool leaveDecal)
    {
        int idx = (int)surface;
        int slot = _nextImpact[idx];
        _nextImpact[idx] = (slot + 1) % ImpactPerSurface;
        Quaternion rot = Quaternion.LookRotation(normal);
        Vector3 pos = point + normal * 0.02f;

        float scale = VfxQuality.ParticleScale;
        var debris = _impacts[idx][slot];
        debris.transform.SetPositionAndRotation(pos, rot);
        debris.Clear(true);
        debris.Emit(Mathf.Max(2, Mathf.RoundToInt(ImpactBurstCount(surface) * scale)));

        var puff = _impactPuffs[idx][slot];
        puff.transform.SetPositionAndRotation(pos, rot);
        puff.Clear(true);
        puff.Emit(Mathf.Max(1, Mathf.RoundToInt(PuffBurstCount(surface) * scale)));

        if (leaveDecal && surface != SurfaceType.Flesh)
        {
            var d = _decals[_nextDecal];
            _nextDecal = (_nextDecal + 1) % _decals.Length;
            d.transform.SetPositionAndRotation(point + normal * 0.012f, Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f)));
            d.transform.localScale = Vector3.one * Random.Range(0.1f, 0.16f);
            d.gameObject.SetActive(true);
        }
    }

    /// <summary>Additive star flash, a puff of smoke and a spark spray at the muzzle.</summary>
    public void SpawnMuzzleFlash(Vector3 position, Vector3 forward, float scale = 1f)
    {
        var flash = _flashes[_nextFlash];
        _nextFlash = (_nextFlash + 1) % _flashes.Length;
        flash.transform.position = position;
        var ep = new ParticleSystem.EmitParams
        {
            position = position,
            startSize = Random.Range(0.28f, 0.4f) * scale,
            rotation = Random.Range(0f, 360f),
            startLifetime = 0.06f,
            startColor = new Color(1f, 0.85f, 0.55f, 1f)
        };
        flash.Emit(ep, 1);

        if (VfxQuality.ParticleScale < 0.5f) return;

        var smoke = _muzzleSmoke[_nextSmoke];
        _nextSmoke = (_nextSmoke + 1) % _muzzleSmoke.Length;
        smoke.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        smoke.Emit(Mathf.Max(1, Mathf.RoundToInt(2 * VfxQuality.ParticleScale)));
    }

    /// <summary>A soft cloud of dust (landing, footsteps, slides).</summary>
    public void SpawnDust(Vector3 position, float radius, int count, Color tint)
    {
        var ps = _dust[_nextDust];
        _nextDust = (_nextDust + 1) % _dust.Length;
        ps.transform.position = position;
        count = Mathf.Max(1, Mathf.RoundToInt(count * VfxQuality.ParticleScale));
        for (int i = 0; i < count; i++)
        {
            Vector2 c = Random.insideUnitCircle * radius;
            var ep = new ParticleSystem.EmitParams
            {
                position = position + new Vector3(c.x, 0.05f, c.y),
                velocity = new Vector3(c.x, Random.Range(0.2f, 0.7f), c.y).normalized * Random.Range(0.4f, 1.6f),
                startSize = Random.Range(0.35f, 0.7f),
                startLifetime = Random.Range(0.5f, 0.9f),
                rotation = Random.Range(0f, 360f),
                startColor = tint
            };
            ps.Emit(ep, 1);
        }
    }

    /// <summary>Ejects a spent shell casing that bounces on the floor for a moment.</summary>
    public void SpawnCasing(Vector3 position, Vector3 right, Vector3 up)
    {
        if (VfxQuality.ParticleScale < 0.5f) return;
        ref Casing c = ref _casings[_nextCasing];
        _nextCasing = (_nextCasing + 1) % _casings.Length;

        var body = c.body;
        body.gameObject.SetActive(true);
        body.transform.SetPositionAndRotation(position, Random.rotation);
        body.linearVelocity = right * Random.Range(1.6f, 2.6f) + up * Random.Range(1.2f, 2f) + Random.insideUnitSphere * 0.3f;
        body.angularVelocity = Random.insideUnitSphere * 25f;
        c.despawnAt = Time.time + 1.8f;
    }

    /// <summary>Plays a clip as a 3D one-shot from a pooled AudioSource (no per-call allocations).</summary>
    public void PlaySfxAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        var src = _sfxSources[_nextSfx];
        _nextSfx = (_nextSfx + 1) % _sfxSources.Length;
        src.transform.position = position;
        src.pitch = pitch;
        src.PlayOneShot(clip, volume * GameAudio.Sfx);
    }

    public void FlashLight(Vector3 position, float intensity = 4.5f, float duration = 0.05f)
    {
        _flashLight.transform.position = position;
        _flashLight.intensity = intensity;
        _flashLight.enabled = true;
        _flashEnd = Time.time + duration;
    }

    // --- Update ---

    private void Update()
    {
        float dt = Time.deltaTime;
        for (int i = 0; i < _tracers.Length; i++)
        {
            ref Tracer t = ref _tracers[i];
            if (!t.active) continue;

            t.travelled += TracerSpeed * dt;
            if (t.travelled - TracerLength >= t.distance)
            {
                t.active = false;
                t.line.enabled = false;
                continue;
            }
            UpdateTracerPositions(ref t);
        }

        if (_flashLight.enabled && Time.time >= _flashEnd)
            _flashLight.enabled = false;

        for (int i = 0; i < _casings.Length; i++)
        {
            if (_casings[i].despawnAt > 0f && Time.time >= _casings[i].despawnAt)
            {
                _casings[i].despawnAt = 0f;
                _casings[i].body.gameObject.SetActive(false);
            }
        }
    }

    private static void UpdateTracerPositions(ref Tracer t)
    {
        float head = Mathf.Min(t.travelled, t.distance);
        float tail = Mathf.Max(0f, t.travelled - TracerLength);
        t.line.SetPosition(0, t.start + t.dir * head);
        t.line.SetPosition(1, t.start + t.dir * tail);
    }

    // --- Construction ---

    private void BuildTracers()
    {
        _tracers = new Tracer[TracerCount];
        for (int i = 0; i < TracerCount; i++)
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.15f));
            lr.widthMultiplier = 0.05f;
            lr.sharedMaterial = _tracerMaterial;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.View;
            lr.enabled = false;
            _tracers[i].line = lr;
        }
    }

    private ParticleSystem MakeSystem(string name, Material material, int maxParticles)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;

        var emission = ps.emission;
        emission.enabled = false;

        var shape = ps.shape;
        shape.enabled = false;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return ps;
    }

    private void BuildImpacts()
    {
        int surfaces = System.Enum.GetValues(typeof(SurfaceType)).Length;
        _impacts = new ParticleSystem[surfaces][];
        _impactPuffs = new ParticleSystem[surfaces][];
        _nextImpact = new int[surfaces];

        var chips = Textured(_alpha, VfxTextures.SoftCircle, "ImpactChips");
        var sparks = Textured(_additive, VfxTextures.SoftCircle, "ImpactSparks");
        var smoke = Textured(_alpha, VfxTextures.Smoke, "ImpactPuff");

        for (int s = 0; s < surfaces; s++)
        {
            var type = (SurfaceType)s;
            _impacts[s] = new ParticleSystem[ImpactPerSurface];
            _impactPuffs[s] = new ParticleSystem[ImpactPerSurface];
            for (int i = 0; i < ImpactPerSurface; i++)
            {
                var debris = MakeSystem("Impact_" + type, type == SurfaceType.Metal ? sparks : chips, 32);
                ConfigureDebris(debris, type);
                _impacts[s][i] = debris;

                var puff = MakeSystem("Puff_" + type, smoke, 12);
                ConfigurePuff(puff, type);
                _impactPuffs[s][i] = puff;
            }
        }
    }

    private static int ImpactBurstCount(SurfaceType s)
    {
        switch (s)
        {
            case SurfaceType.Metal: return 14;
            case SurfaceType.Flesh: return 12;
            case SurfaceType.Wood: return 9;
            default: return 9;
        }
    }

    private static int PuffBurstCount(SurfaceType s)
    {
        switch (s)
        {
            case SurfaceType.Metal: return 1;
            case SurfaceType.Flesh: return 3;
            default: return 3;
        }
    }

    private static void ConfigureDebris(ParticleSystem ps, SurfaceType surface)
    {
        var main = ps.main;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 38f;
        shape.radius = 0.01f;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();

        switch (surface)
        {
            case SurfaceType.Metal:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.032f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.5f, 0.12f, 1f));
                main.gravityModifier = 0.6f;
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.05f;
                renderer.lengthScale = 3.5f;
                break;
            case SurfaceType.Flesh:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.05f, 0.05f, 1f), new Color(0.5f, 0.02f, 0.02f, 1f));
                main.gravityModifier = 1.6f;
                break;
            case SurfaceType.Wood:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.42f, 0.2f, 1f), new Color(0.4f, 0.27f, 0.13f, 1f));
                main.gravityModifier = 1.7f;
                break;
            default:
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4.5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.06f);
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.82f, 0.76f, 1f), new Color(0.6f, 0.58f, 0.55f, 1f));
                main.gravityModifier = 1.5f;
                break;
        }

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        fade.color = FadeGradient();
    }

    private static void ConfigurePuff(ParticleSystem ps, SurfaceType surface)
    {
        var main = ps.main;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.02f;

        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.3f);
        main.gravityModifier = -0.05f;

        switch (surface)
        {
            case SurfaceType.Metal:
                main.startColor = new Color(0.8f, 0.8f, 0.82f, 0.25f);
                break;
            case SurfaceType.Flesh:
                main.startColor = new Color(0.55f, 0.04f, 0.04f, 0.55f);
                break;
            case SurfaceType.Wood:
                main.startColor = new Color(0.55f, 0.42f, 0.28f, 0.4f);
                break;
            default:
                main.startColor = new Color(0.78f, 0.75f, 0.7f, 0.45f);
                break;
        }

        var fade = ps.colorOverLifetime;
        fade.enabled = true;
        fade.color = FadeGradient();

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.7f));
    }

    private static Gradient FadeGradient()
    {
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        return grad;
    }

    private void BuildMuzzleEffects()
    {
        var flashMat = Textured(_additive, VfxTextures.Star, "MuzzleFlash");
        _flashes = new ParticleSystem[FlashCount];
        for (int i = 0; i < FlashCount; i++)
        {
            var ps = MakeSystem("MuzzleFlash", flashMat, 4);
            var main = ps.main;
            main.startLifetime = 0.06f;
            main.startSpeed = 0f;
            main.startSize = 0.35f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.6f));
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            fade.color = FadeGradient();
            _flashes[i] = ps;
        }

        var smokeMat = Textured(_alpha, VfxTextures.Smoke, "MuzzleSmoke");
        _muzzleSmoke = new ParticleSystem[PuffCount];
        for (int i = 0; i < PuffCount; i++)
        {
            var ps = MakeSystem("MuzzleSmoke", smokeMat, 8);
            var main = ps.main;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = new Color(0.85f, 0.85f, 0.85f, 0.28f);
            main.gravityModifier = -0.08f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.01f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            fade.color = FadeGradient();
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.6f, 1f, 2.4f));
            _muzzleSmoke[i] = ps;
        }
    }

    private void BuildDust()
    {
        var mat = Textured(_alpha, VfxTextures.Smoke, "Dust");
        _dust = new ParticleSystem[PuffCount];
        for (int i = 0; i < PuffCount; i++)
        {
            var ps = MakeSystem("Dust", mat, 40);
            var main = ps.main;
            main.startSpeed = 0f;
            main.startLifetime = 0.7f;
            main.startSize = 0.5f;
            main.gravityModifier = -0.04f;
            var fade = ps.colorOverLifetime;
            fade.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.15f), new GradientAlphaKey(0f, 1f) });
            fade.color = grad;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.8f));
            _dust[i] = ps;
        }
    }

    private void BuildCasings()
    {
        var prim = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        var mesh = prim.GetComponent<MeshFilter>().sharedMesh;
        Destroy(prim);

        var brass = Resources.Load<Material>("VFX/VFX_Brass");
        _casings = new Casing[CasingCount];
        for (int i = 0; i < CasingCount; i++)
        {
            var go = new GameObject("Casing");
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(0.012f, 0.02f, 0.012f);
            go.layer = LayerMask.NameToLayer("Projectile");

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            if (brass != null) mr.sharedMaterial = brass;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var col = go.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.height = 2f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            go.SetActive(false);
            _casings[i].body = rb;
        }
    }

    private void BuildDecals()
    {
        var tex = VfxTextures.BulletHole;
        _decalSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
        _decals = new SpriteRenderer[DecalCount];
        for (int i = 0; i < DecalCount; i++)
        {
            var go = new GameObject("BulletHole");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _decalSprite;
            sr.color = Color.white;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.SetActive(false);
            _decals[i] = sr;
        }
    }

    private void BuildFlashLight()
    {
        var go = new GameObject("MuzzleFlashLight");
        go.transform.SetParent(transform, false);
        _flashLight = go.AddComponent<Light>();
        _flashLight.type = LightType.Point;
        _flashLight.color = new Color(1f, 0.78f, 0.45f);
        _flashLight.range = 7f;
        _flashLight.shadows = LightShadows.None;
        _flashLight.enabled = false;
    }

    private void BuildSfxSources()
    {
        _sfxSources = new AudioSource[16];
        for (int i = 0; i < _sfxSources.Length; i++)
        {
            var go = new GameObject("Sfx3D");
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 45f;
            _sfxSources[i] = src;
        }
    }
}

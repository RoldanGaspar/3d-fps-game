using UnityEngine;

/// <summary>
/// Floating health bar above the enemy's head and a short colour flash when it takes damage.
/// Built entirely in code from sprite renderers (no canvas needed).
/// </summary>
[RequireComponent(typeof(EnemyBase))]
public class EnemyVisuals : MonoBehaviour
{
    [SerializeField] private Renderer[] _renderers;
    [SerializeField] private float _barHeight = 2.25f;
    [SerializeField] private float _barWidth = 0.9f;
    [SerializeField] private float _barThickness = 0.09f;
    [SerializeField] private float _flashTime = 0.09f;
    [SerializeField] private Color _flashColor = new Color(2.2f, 1.7f, 1.7f, 1f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static Sprite _pixel;

    private EnemyBase _health;
    private Transform _bar;
    private SpriteRenderer _fill;
    private MaterialPropertyBlock _block;
    private float _flashEnd;
    private bool _flashing;

    private void Awake()
    {
        _health = GetComponent<EnemyBase>();
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        _block = new MaterialPropertyBlock();
        BuildBar();
    }

    private void OnEnable()
    {
        _health.Damaged += OnDamaged;
        _health.OnHealthChanged.AddListener(OnHealthChanged);
        _health.OnDeath.AddListener(OnDeath);
    }

    private void OnDisable()
    {
        _health.Damaged -= OnDamaged;
        _health.OnHealthChanged.RemoveListener(OnHealthChanged);
        _health.OnDeath.RemoveListener(OnDeath);
    }

    private void BuildBar()
    {
        if (_pixel == null)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _pixel = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);
        }

        _bar = new GameObject("HealthBar").transform;
        _bar.SetParent(transform, false);
        _bar.localPosition = new Vector3(0f, _barHeight, 0f);

        var bg = MakeSprite("BG", new Color(0.05f, 0.05f, 0.05f, 0.75f), -1);
        bg.transform.localPosition = new Vector3(-_barWidth * 0.5f - 0.02f, 0f, 0f);
        bg.transform.localScale = new Vector3(_barWidth + 0.04f, _barThickness + 0.04f, 1f);

        _fill = MakeSprite("Fill", new Color(0.95f, 0.2f, 0.2f, 1f), 0);
        _fill.transform.localPosition = new Vector3(-_barWidth * 0.5f, 0f, -0.001f);
        _fill.transform.localScale = new Vector3(_barWidth, _barThickness, 1f);
    }

    private SpriteRenderer MakeSprite(string spriteName, Color color, int order)
    {
        var go = new GameObject(spriteName);
        go.transform.SetParent(_bar, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = _pixel;
        sr.color = color;
        sr.sortingOrder = order;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return sr;
    }

    private void OnHealthChanged(float normalized)
    {
        if (_fill == null) return;
        normalized = Mathf.Clamp01(normalized);
        var s = _fill.transform.localScale;
        s.x = _barWidth * normalized;
        _fill.transform.localScale = s;
        _fill.color = Color.Lerp(new Color(0.95f, 0.15f, 0.1f), new Color(0.3f, 0.9f, 0.3f), normalized);
    }

    private void OnDamaged(float amount)
    {
        _flashing = true;
        _flashEnd = Time.time + _flashTime;
        _block.SetColor(BaseColorId, _flashColor);
        foreach (var r in _renderers)
            if (r != null) r.SetPropertyBlock(_block);
    }

    private void OnDeath()
    {
        if (_bar != null) _bar.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_flashing && Time.time >= _flashEnd)
        {
            _flashing = false;
            foreach (var r in _renderers)
                if (r != null) r.SetPropertyBlock(null);
        }

        var cam = Camera.main;
        if (cam != null && _bar != null && _bar.gameObject.activeSelf)
            _bar.rotation = cam.transform.rotation;
    }
}

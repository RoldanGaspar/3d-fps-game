using PrimeTween;
using TMPro;
using UnityEngine;

/// <summary>
/// Floating damage numbers: pooled world-space TextMeshPro labels that pop, rise and fade
/// at the point where the player's shot landed on a target.
/// </summary>
public class DamageNumbers : MonoBehaviour
{
    [SerializeField] private int _poolSize = 16;
    [SerializeField] private float _riseHeight = 0.9f;
    [SerializeField] private float _lifetime = 0.7f;
    [SerializeField] private Color _hitColor = new Color(1f, 0.95f, 0.8f, 1f);
    [SerializeField] private Color _killColor = new Color(1f, 0.3f, 0.25f, 1f);

    private class Label
    {
        public Transform transform;
        public TextMeshPro text;
        public Tween tween;
        public bool active;
        public Vector3 start;
        public Color color;
        public DamageNumbers owner;
    }

    private Label[] _labels;
    private int _next;
    private Camera _cam;

    private void Awake()
    {
        _labels = new Label[_poolSize];
        for (int i = 0; i < _poolSize; i++)
        {
            var go = new GameObject("DamageNumber");
            go.transform.SetParent(transform, false);
            var tmp = go.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 3f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.outlineWidth = 0.2f;
            tmp.outlineColor = new Color32(0, 0, 0, 255);
            go.SetActive(false);
            _labels[i] = new Label { transform = go.transform, text = tmp };
        }
    }

    private void OnEnable()
    {
        CombatEvents.OnPlayerShotLanded += HandleShot;
    }

    private void OnDisable()
    {
        CombatEvents.OnPlayerShotLanded -= HandleShot;
        if (_labels == null) return;
        foreach (var l in _labels) l.tween.Stop();
    }

    private void HandleShot(HitInfo info)
    {
        if (!info.hitTarget) return;

        var label = _labels[_next];
        _next = (_next + 1) % _labels.Length;
        label.tween.Stop();

        label.start = info.point + info.normal * 0.15f + Vector3.up * 0.25f
                      + new Vector3(Random.Range(-0.15f, 0.15f), 0f, Random.Range(-0.15f, 0.15f));
        label.color = info.killed ? _killColor : _hitColor;
        label.text.text = NumberString(Mathf.RoundToInt(info.damage));
        label.text.fontSize = info.killed ? 4.5f : 3f;
        label.text.color = label.color;
        label.transform.position = label.start;
        label.transform.gameObject.SetActive(true);
        label.active = true;

        // 0..1 progress drives rise, fade and a quick pop-in scale.
        label.owner = this;
        label.tween = Tween.Custom(label, 0f, 1f, _lifetime, (l, k) => l.owner.Animate(l, k), Ease.OutCubic)
            .OnComplete(label, l =>
            {
                l.active = false;
                l.transform.gameObject.SetActive(false);
            });
    }

    // Cached strings so showing a damage number doesn't allocate for typical damage values.
    private static readonly string[] NumberCache = new string[256];

    private static string NumberString(int value)
    {
        if (value < 0 || value >= NumberCache.Length) return value.ToString();
        return NumberCache[value] ?? (NumberCache[value] = value.ToString());
    }

    private void Animate(Label l, float k)
    {
        l.transform.position = l.start + Vector3.up * (_riseHeight * k);

        float pop = k < 0.15f ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, (k - 0.15f) / 0.25f);

        // keep the number a readable size regardless of distance
        if (_cam == null) _cam = Camera.main;
        float distScale = 1f;
        if (_cam != null)
            distScale = Mathf.Clamp(Vector3.Distance(_cam.transform.position, l.transform.position) * 0.12f, 0.6f, 3f);
        l.transform.localScale = Vector3.one * (Mathf.Max(0.5f, pop) * distScale);

        Color c = l.color;
        c.a = k < 0.55f ? 1f : 1f - (k - 0.55f) / 0.45f;
        l.text.color = c;
    }

    private void LateUpdate()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Quaternion rot = _cam.transform.rotation;
        foreach (var l in _labels)
        {
            if (l.active) l.transform.rotation = rot;
        }
    }
}

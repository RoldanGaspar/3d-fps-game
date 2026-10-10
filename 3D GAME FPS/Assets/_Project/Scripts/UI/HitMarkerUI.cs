using PrimeTween;
using UnityEngine;
using Project.UI;

/// <summary>
/// Flashes the hit marker and plays a confirm sound whenever the player's shot lands on a target.
/// Listens to <see cref="CombatEvents"/>, so it needs no reference to the weapons.
/// </summary>
public class HitMarkerUI : MonoBehaviour
{
    [SerializeField] private VectorHitMarker _marker;
    [SerializeField] private Color _hitColor = Color.white;
    [SerializeField] private Color _killColor = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float _showTime = 0.18f;
    [SerializeField] private float _punchScale = 1.35f;
    [SerializeField] private float _hitVolume = 0.5f;

    private AudioSource _audio;
    private Tween _tween;
    private bool _kill;

    private void Awake()
    {
        if (_marker == null) _marker = GetComponentInChildren<VectorHitMarker>(true);
        _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;
        _audio.spatialBlend = 0f;
        Hide();
    }

    private void OnEnable()
    {
        CombatEvents.OnPlayerShotLanded += HandleShot;
    }

    private void OnDisable()
    {
        CombatEvents.OnPlayerShotLanded -= HandleShot;
        _tween.Stop();
    }

    private void HandleShot(HitInfo info)
    {
        if (!info.hitTarget) return;

        _kill = info.killed;
        _audio.PlayOneShot(ProceduralSfx.Get(_kill ? SfxId.KillConfirm : SfxId.HitMarker), _hitVolume * GameAudio.Sfx);

        _tween.Stop();
        Apply(1f, _punchScale);
        float duration = _kill ? _showTime * 1.8f : _showTime;
        _tween = Tween.Custom(this, 1f, 0f, duration,
                (t, k) => t.Apply(k, Mathf.Lerp(1f, t._punchScale, k)),
                Ease.OutQuad, useUnscaledTime: true)
            .OnComplete(this, t => t.Hide());
    }

    private void Apply(float alpha, float scale)
    {
        if (_marker == null) return;
        Color c = _kill ? _killColor : _hitColor;
        c.a = alpha;
        _marker.color = c;
        _marker.rectTransform.localScale = Vector3.one * scale;
        _marker.enabled = true;
    }

    private void Hide()
    {
        if (_marker != null) _marker.enabled = false;
    }
}

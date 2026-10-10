using PrimeTween;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Drives the URP post-processing for gameplay feedback: a red pulsing vignette at low health,
/// a chromatic-aberration kick when damaged and a subtle speed vignette while sprinting or sliding.
/// Post effects can be switched off for low-end devices with <see cref="SetEnabled"/>.
/// </summary>
public class PostFxController : MonoBehaviour
{
    [Header("Low health")]
    [SerializeField] private float _lowHealthThreshold = 0.35f;
    [SerializeField] private Color _hurtColor = new Color(0.55f, 0f, 0f, 1f);

    [Header("Base look")]
    [SerializeField] private float _baseVignette = 0.16f;
    [SerializeField] private float _speedVignette = 0.1f;

    private Volume _volume;
    private Vignette _vignette;
    private ChromaticAberration _aberration;
    private Bloom _bloom;
    private PlayerHealth _health;
    private PlayerMotor _motor;
    private UniversalAdditionalCameraData _cameraData;

    private float _damageKick;
    private Tween _kickTween;
    private bool _enabledFx = true;

    private void Start()
    {
        _health = GetComponent<PlayerHealth>();
        _motor = GetComponent<PlayerMotor>();

        _volume = FindFirstObjectByType<Volume>();
        if (_volume != null)
        {
            // Work on an instance so the shared asset isn't modified in the editor.
            var profile = _volume.profile;
            profile.TryGet(out _vignette);
            profile.TryGet(out _aberration);
            profile.TryGet(out _bloom);
        }

        // Post effects follow the graphics quality setting (off on Low / battery saver).
        _enabledFx = SaveSystem.Profile.graphicsQuality >= GameSettings.QualityMedium && !SaveSystem.Profile.batterySaver;

        var cam = Camera.main;
        if (cam != null)
        {
            _cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (_cameraData != null) _cameraData.renderPostProcessing = _enabledFx;
        }

        if (_health != null) _health.OnDamaged.AddListener(OnDamaged);
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDamaged.RemoveListener(OnDamaged);
        _kickTween.Stop();
    }

    public void SetEnabled(bool on)
    {
        _enabledFx = on;
        if (_cameraData != null) _cameraData.renderPostProcessing = on;
    }

    private void OnDamaged()
    {
        _kickTween.Stop();
        _damageKick = 1f;
        _kickTween = Tween.Custom(this, 1f, 0f, 0.4f, (c, v) => c._damageKick = v, Ease.OutQuad, useUnscaledTime: true);
    }

    private void Update()
    {
        if (!_enabledFx) return;

        float ratio = _health != null ? _health.CurrentHealth / Mathf.Max(1f, _health.MaxHealth) : 1f;
        float low = _health != null && _health.IsAlive ? Mathf.Clamp01((_lowHealthThreshold - ratio) / _lowHealthThreshold) : 0f;

        if (_vignette != null)
        {
            float speed = _motor != null ? Mathf.InverseLerp(0.6f, 1f, _motor.GetNormalizedSpeed()) : 0f;
            float pulse = low > 0f ? (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.Lerp(5f, 9f, low))) : 0f;

            float intensity = _baseVignette + _speedVignette * speed + low * (0.18f + 0.22f * pulse) + _damageKick * 0.2f;
            _vignette.intensity.Override(Mathf.Clamp01(intensity));
            _vignette.color.Override(Color.Lerp(Color.black, _hurtColor, Mathf.Clamp01(low + _damageKick)));
        }

        if (_aberration != null)
            _aberration.intensity.Override(Mathf.Clamp01(_damageKick * 0.7f + low * 0.15f));
    }
}

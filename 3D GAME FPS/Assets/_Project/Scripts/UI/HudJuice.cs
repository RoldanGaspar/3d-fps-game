using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Adds feedback to the HUD without touching HUDManager: ammo counter punch on every shot and reload,
/// health panel shake and a red screen flash when hit, plus a heartbeat pulse at low health.
/// </summary>
public class HudJuice : MonoBehaviour
{
    [Header("Targets (found by name if left empty)")]
    [SerializeField] private RectTransform _ammoPanel;
    [SerializeField] private RectTransform _healthPanel;
    [SerializeField] private RectTransform _weaponSub;

    [Header("Low health")]
    [SerializeField] private float _lowHealthThreshold = 0.3f;

    private PlayerHealth _health;
    private WeaponManager _weapons;
    private WeaponBase _subscribed;
    private Image _damageFlash;
    private float _nextHeartbeat;

    private void Awake()
    {
        if (_ammoPanel == null) _ammoPanel = FindChild("AmmoPanel");
        if (_healthPanel == null) _healthPanel = FindChild("HealthPanel");
        if (_weaponSub == null) _weaponSub = FindChild("WeaponSub");
        BuildDamageFlash();
    }

    private void Start()
    {
        _health = FindFirstObjectByType<PlayerHealth>();
        if (_health != null) _health.OnDamaged.AddListener(OnDamaged);

        _weapons = FindFirstObjectByType<WeaponManager>();
        if (_weapons != null)
        {
            _weapons.OnWeaponSwitched += OnWeaponSwitched;
            OnWeaponSwitched(_weapons.CurrentWeapon);
        }
    }

    private void OnDestroy()
    {
        if (_health != null) _health.OnDamaged.RemoveListener(OnDamaged);
        if (_weapons != null) _weapons.OnWeaponSwitched -= OnWeaponSwitched;
        Unsubscribe();
    }

    private RectTransform FindChild(string childName)
    {
        foreach (var rt in GetComponentsInChildren<RectTransform>(true))
            if (rt.name == childName) return rt;
        return null;
    }

    private void BuildDamageFlash()
    {
        var go = new GameObject("DamageFlash", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        go.transform.SetAsFirstSibling();
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        _damageFlash = go.AddComponent<Image>();
        _damageFlash.color = new Color(0.9f, 0.05f, 0.05f, 0f);
        _damageFlash.raycastTarget = false;
    }

    // --- Weapon feedback ---

    private void OnWeaponSwitched(WeaponBase weapon)
    {
        Unsubscribe();
        _subscribed = weapon;
        if (_subscribed != null)
        {
            _subscribed.OnFired += OnFired;
            _subscribed.OnReloadCompleted += OnReloaded;
        }
        Punch(_weaponSub, 0.18f, 0.2f);
    }

    private void Unsubscribe()
    {
        if (_subscribed == null) return;
        _subscribed.OnFired -= OnFired;
        _subscribed.OnReloadCompleted -= OnReloaded;
        _subscribed = null;
    }

    private void OnFired() => Punch(_ammoPanel, 0.05f, 0.1f);
    private void OnReloaded() => Punch(_ammoPanel, 0.14f, 0.28f);

    // --- Damage feedback ---

    private void OnDamaged()
    {
        Punch(_healthPanel, 0.15f, 0.25f);
        if (_healthPanel != null)
            Tween.ShakeLocalPosition(_healthPanel, new Vector3(10f, 8f, 0f), 0.3f, frequency: 18f, useUnscaledTime: true);

        if (_damageFlash != null)
        {
            Tween.StopAll(_damageFlash);
            _damageFlash.color = new Color(0.9f, 0.05f, 0.05f, 0.32f);
            Tween.Alpha(_damageFlash, 0f, 0.4f, Ease.OutQuad, useUnscaledTime: true);
        }
    }

    private void Update()
    {
        if (_health == null || !_health.IsAlive) return;

        float ratio = _health.CurrentHealth / Mathf.Max(1f, _health.MaxHealth);
        if (ratio > _lowHealthThreshold || Time.unscaledTime < _nextHeartbeat) return;

        // faster heartbeat the lower the health
        _nextHeartbeat = Time.unscaledTime + Mathf.Lerp(0.5f, 1.0f, ratio / _lowHealthThreshold);
        Punch(_healthPanel, 0.1f, 0.22f);
        if (_damageFlash != null)
        {
            Tween.StopAll(_damageFlash);
            _damageFlash.color = new Color(0.9f, 0.05f, 0.05f, 0.14f);
            Tween.Alpha(_damageFlash, 0f, 0.45f, Ease.OutQuad, useUnscaledTime: true);
        }
    }

    private static void Punch(RectTransform target, float strength, float duration)
    {
        if (target == null) return;
        Tween.StopAll(target);
        target.localScale = Vector3.one;
        Tween.PunchScale(target, Vector3.one * strength, duration, frequency: 2f, useUnscaledTime: true);
    }
}

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Abstract base class for all weapons.
/// Manages ammo, reload coroutines, events, and firing state.
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] protected WeaponData _data;

    [Header("References")]
    [SerializeField] protected Transform _muzzlePoint;
    [SerializeField] protected ParticleSystem _muzzleFlash;
    [SerializeField] protected AudioSource _audioSource;

    protected int _currentAmmo;
    protected int _reserveAmmo;
    protected bool _isReloading;
    protected float _nextFireTime;
    protected Coroutine _reloadCoroutine;

    // Weapon Events
    public event Action<int, int> OnAmmoChanged;
    public event Action OnFired;
    public event Action OnReloadStarted;
    public event Action OnReloadCompleted;

    public WeaponData Data => _data;
    public int CurrentAmmo => _currentAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public bool IsReloading => _isReloading;
    public bool CanShoot => CanFire();

    protected virtual void Awake()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f; // 2D weapon audio for player viewmodel
    }

    protected virtual void Start()
    {
        InitializeAmmo();
    }

    public virtual void Initialize(WeaponData data)
    {
        _data = data;
        InitializeAmmo();
    }

    protected virtual void InitializeAmmo()
    {
        if (_data != null)
        {
            _currentAmmo = _data.magazineSize;
            _reserveAmmo = _data.maxReserveAmmo;
            NotifyAmmoChanged();
        }
    }

    public abstract void Fire();

    public virtual void Reload()
    {
        if (!CanReload()) return;

        if (_reloadCoroutine != null) StopCoroutine(_reloadCoroutine);
        _reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    public virtual void CancelReload()
    {
        if (_isReloading && _reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
            _isReloading = false;
        }
    }

    protected virtual IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        OnReloadStarted?.Invoke();

        PlaySound(_data != null && _data.reloadSound != null
            ? _data.reloadSound
            : ProceduralSfx.Get(SfxId.Reload));

        float duration = (_data != null) ? _data.reloadTime : 2.0f;
        yield return new WaitForSeconds(duration);

        if (_data != null)
        {
            int ammoNeeded = _data.magazineSize - _currentAmmo;
            int ammoToAdd = Mathf.Min(ammoNeeded, _reserveAmmo);
            _currentAmmo += ammoToAdd;
            _reserveAmmo -= ammoToAdd;
        }

        _isReloading = false;
        _reloadCoroutine = null;
        NotifyAmmoChanged();
        OnReloadCompleted?.Invoke();
    }

    public abstract void AimDownSights(bool isAiming);

    public virtual bool CanFire()
    {
        return !_isReloading && _currentAmmo > 0 && Time.time >= _nextFireTime;
    }

    public virtual bool CanReload()
    {
        return !_isReloading && _data != null && _currentAmmo < _data.magazineSize && _reserveAmmo > 0;
    }

    public virtual void AddAmmo(int amount)
    {
        if (_data == null) return;
        _reserveAmmo = Mathf.Min(_reserveAmmo + amount, _data.maxReserveAmmo);
        NotifyAmmoChanged();
    }

    protected void NotifyAmmoChanged()
    {
        OnAmmoChanged?.Invoke(_currentAmmo, _reserveAmmo);
    }

    protected void TriggerFiredEvent()
    {
        OnFired?.Invoke();
    }

    protected void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(clip, volume * GameAudio.Sfx);
        }
    }
}

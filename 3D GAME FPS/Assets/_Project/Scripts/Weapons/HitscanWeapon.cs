using UnityEngine;

/// <summary>
/// Raycast-based hitscan weapon (pistol, rifle, shotgun, sniper...).
/// Handles shooting with spread and pellets, damage delivery via IDamageable,
/// muzzle flash, audio, procedural tracer, and recoil.
/// </summary>
public class HitscanWeapon : WeaponBase
{
    [Header("Hitscan Settings")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private LayerMask _hitMask;
    [SerializeField] private WeaponRecoil _recoil;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private GameObject _impactEffectPrefab;
    [SerializeField] private LineRenderer _tracerPrefab;

    [Header("ADS Settings")]
    [SerializeField] private Vector3 _hipPosition;
    [SerializeField] private Vector3 _adsPosition = new Vector3(0f, -0.15f, 0.3f);
    private bool _isAiming;
    private float _nextDryFireTime;
    private Vector3 _blendedPosition;
    private Quaternion _restRotation = Quaternion.identity;

    public bool IsAiming => _isAiming;

    protected override void Awake()
    {
        base.Awake();
        if (_recoil == null) _recoil = GetComponent<WeaponRecoil>();
        if (_playerCamera == null) _playerCamera = Camera.main ?? FindFirstObjectByType<Camera>();

        if (_hitMask == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            int projectileLayer = LayerMask.NameToLayer("Projectile");
            _hitMask = ~((1 << playerLayer) | (1 << projectileLayer));
        }

        _hipPosition = transform.localPosition;
        _blendedPosition = _hipPosition;
        _restRotation = transform.localRotation;
    }

    private Camera GetActiveCamera()
    {
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            return Camera.main;
        if (_playerCamera != null && _playerCamera.isActiveAndEnabled)
            return _playerCamera;
        return Camera.main ?? _playerCamera;
    }

    private void Update()
    {
        HandleADS();
    }

    private void HandleADS()
    {
        if (_data == null) return;
        Vector3 targetPos = _isAiming ? _adsPosition : _hipPosition;
        _blendedPosition = Vector3.Lerp(_blendedPosition, targetPos, Time.deltaTime * _data.adsSpeed);

        // WeaponRecoil owns the weapon transform; hand it the ADS position as its resting pose so the
        // two systems don't fight over transform.localPosition. FOV is handled by FovController.
        if (_recoil != null)
            _recoil.SetBaseTransform(_blendedPosition, _restRotation);
        else
            transform.localPosition = _blendedPosition;
    }

    public override void Fire()
    {
        if (!CanFire())
        {
            // Dry fire click (rate limited so holding the trigger doesn't machine-gun it)
            if (!_isReloading && _currentAmmo <= 0 && Time.time >= _nextDryFireTime)
            {
                _nextDryFireTime = Time.time + 0.3f;
                PlaySound(_data != null && _data.emptySound != null ? _data.emptySound : ProceduralSfx.Get(SfxId.Empty), 0.6f);
            }
            return;
        }

        _currentAmmo--;
        _nextFireTime = Time.time + (1f / Mathf.Max(0.1f, _data.fireRate));

        // Visual / Audio triggers
        SpawnMuzzleEffects();
        if (_recoil != null) _recoil.ApplyRecoil(_data != null ? Mathf.Max(0.5f, _data.recoilForce / 1.2f) : 1f);
        var camCtrl = GetComponentInParent<CameraModeController>();
        if (camCtrl != null) camCtrl.GenerateImpulse(0.25f * (_data != null ? Mathf.Max(0.6f, _data.recoilForce / 1.2f) : 1f));
        PlaySound(GetFireClip());

        TriggerFiredEvent();
        NotifyAmmoChanged();

        ExecuteRaycastShot();
    }

    private void ExecuteRaycastShot()
    {
        Camera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        float range = _data != null && _data.range > 0f ? _data.range : _range;
        int pellets = _data != null ? Mathf.Max(1, _data.pellets) : 1;
        float currentSpread = (_data != null) ? (_isAiming ? _data.spreadMin : _data.spreadMax) : 0.02f;

        // Only the first pellet that hits a target raises the hit marker / sound, so a shotgun blast
        // doesn't play eight overlapping confirmations.
        bool reportedTargetHit = false;
        bool anyKill = false;

        for (int i = 0; i < pellets; i++)
        {
            Vector3 spreadOffset = activeCam.transform.right * Random.Range(-currentSpread, currentSpread)
                                 + activeCam.transform.up * Random.Range(-currentSpread, currentSpread);

            Vector3 rayOrigin = activeCam.transform.position;
            Vector3 rayDir = (activeCam.transform.forward + spreadOffset).normalized;

            Vector3 hitPoint;
            if (Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, range, _hitMask, QueryTriggerInteraction.Ignore))
            {
                hitPoint = hit.point;
                bool hitTarget, killed;
                ProcessHit(hit, rayDir, out hitTarget, out killed);
                anyKill |= killed;

                bool report = hitTarget ? !reportedTargetHit : (pellets == 1 || i == 0);
                if (hitTarget) reportedTargetHit = true;
                if (report)
                {
                    SurfaceType surface = SurfaceMarker.Resolve(hit.collider);
                    float dmg = (_data != null) ? _data.damage : 10f;
                    CombatEvents.RaisePlayerShotLanded(new HitInfo
                    {
                        point = hit.point,
                        normal = hit.normal,
                        damage = dmg * (hitTarget ? 1f : 0f),
                        surface = surface,
                        hitTarget = hitTarget,
                        killed = killed || (hitTarget && anyKill)
                    });
                }
            }
            else
            {
                hitPoint = rayOrigin + rayDir * range;
            }

            SpawnTracer(hitPoint);
        }
    }

    private void ProcessHit(RaycastHit hit, Vector3 rayDir, out bool hitTarget, out bool killed)
    {
        float dmg = (_data != null) ? _data.damage : 10f;
        hitTarget = false;
        killed = false;

        // Tell the target where it was hit (ragdoll impulse) and shove loose physics props.
        float impulse = dmg * 1.2f + 15f;
        var reactive = hit.collider.GetComponentInParent<IHitReactive>();
        if (reactive != null) reactive.OnHit(hit.point, rayDir, impulse);
        else if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
            hit.rigidbody.AddForceAtPosition(rayDir * impulse, hit.point, ForceMode.Impulse);

        // Damage target if damageable
        var damageable = hit.collider.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            bool wasAlive = damageable.IsAlive;
            damageable.TakeDamage(dmg);
            hitTarget = wasAlive;
            killed = wasAlive && !damageable.IsAlive;
        }
        else
        {
            var enemy = hit.collider.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                bool wasAlive = enemy.IsAlive;
                enemy.TakeDamage(dmg);
                hitTarget = wasAlive;
                killed = wasAlive && !enemy.IsAlive;
            }
        }

        SurfaceType surface = SurfaceMarker.Resolve(hit.collider);
        SpawnImpactEffect(hit.point, hit.normal, surface, hitTarget);
    }

    private void SpawnImpactEffect(Vector3 point, Vector3 normal, SurfaceType surface, bool hitTarget)
    {
        if (_impactEffectPrefab != null)
        {
            Instantiate(_impactEffectPrefab, point, Quaternion.LookRotation(normal));
            return;
        }

        VfxPool.Instance.SpawnImpact(point, normal, surface, leaveDecal: !hitTarget);
        PlayImpactSound(point, surface);
    }

    private void PlayImpactSound(Vector3 point, SurfaceType surface)
    {
        SfxId id;
        switch (surface)
        {
            case SurfaceType.Metal: id = SfxId.ImpactMetal; break;
            case SurfaceType.Wood: id = SfxId.ImpactWood; break;
            case SurfaceType.Flesh: id = SfxId.ImpactFlesh; break;
            default: id = SfxId.ImpactConcrete; break;
        }
        VfxPool.Instance.PlaySfxAt(ProceduralSfx.Get(id), point, 0.6f, Random.Range(0.92f, 1.08f));
    }

    private void SpawnTracer(Vector3 targetPoint)
    {
        Vector3 muzzle = _muzzlePoint != null ? _muzzlePoint.position : transform.position;
        VfxPool.Instance.SpawnTracer(muzzle, targetPoint, new Color(1f, 0.92f, 0.6f, 0.9f));
    }

    private void SpawnMuzzleEffects()
    {
        Transform muzzle = _muzzlePoint != null ? _muzzlePoint : transform;
        var pool = VfxPool.Instance;
        pool.SpawnMuzzleFlash(muzzle.position, muzzle.forward);
        pool.FlashLight(muzzle.position);
        // spent casing kicks out of the ejection port
        pool.SpawnCasing(muzzle.position - muzzle.forward * 0.3f + muzzle.up * 0.03f, muzzle.right, muzzle.up);
    }

    private AudioClip GetFireClip()
    {
        if (_data != null && _data.fireSound != null) return _data.fireSound;
        SfxId id = SfxId.RifleShot;
        if (_data != null)
        {
            switch (_data.weaponType)
            {
                case WeaponType.Pistol: id = SfxId.PistolShot; break;
                case WeaponType.Shotgun: id = SfxId.ShotgunShot; break;
                case WeaponType.Sniper: id = SfxId.SniperShot; break;
                case WeaponType.SMG: id = SfxId.PistolShot; break;
            }
        }
        return ProceduralSfx.Get(id);
    }

    public override void AimDownSights(bool isAiming)
    {
        _isAiming = isAiming;
    }
}

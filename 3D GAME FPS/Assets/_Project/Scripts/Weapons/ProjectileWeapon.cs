using UnityEngine;

/// <summary>
/// Physics-based projectile weapon (rocket launcher, grenade launcher).
/// </summary>
public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile")]
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 30f;

    public override void Fire()
    {
        if (!CanFire()) return;

        _currentAmmo--;
        _nextFireTime = Time.time + (1f / _data.fireRate);

        if (_muzzleFlash != null) _muzzleFlash.Play();

        // TODO: Instantiate projectile with velocity
    }

    public override void Reload()
    {
        // TODO: Start reload coroutine
    }

    public override void AimDownSights(bool isAiming)
    {
        // TODO: Lerp to ADS position
    }
}

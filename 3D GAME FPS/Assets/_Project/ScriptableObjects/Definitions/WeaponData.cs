using UnityEngine;

/// <summary>
/// ScriptableObject definition for weapon stats.
/// Create instances via: Create > Game > Weapon Data
/// </summary>
[CreateAssetMenu(fileName = "New WeaponData", menuName = "Game/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;
    public Sprite icon;
    public WeaponType weaponType;

    [Header("Stats")]
    public float damage = 10f;
    public float fireRate = 5f;
    public int magazineSize = 30;
    public int maxReserveAmmo = 90;
    public float reloadTime = 2f;
    public float range = 100f;
    [Tooltip("Bullets fired per trigger pull (shotguns fire several pellets).")]
    public int pellets = 1;
    public bool isFullAuto = true;
    public string fireModeDescription = "FULL AUTO";

    [Header("Audio")]
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;

    [Header("Recoil")]
    public float recoilForce = 1f;
    public float spreadMin = 0.01f;
    public float spreadMax = 0.1f;

    [Header("ADS")]
    public float adsSpeed = 8f;
    public float adsFOV = 40f;
}

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    Sniper,
    RocketLauncher
}
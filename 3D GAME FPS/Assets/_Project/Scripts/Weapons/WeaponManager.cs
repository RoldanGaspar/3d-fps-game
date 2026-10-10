using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages weapon equipping, switching, full-auto / semi-auto firing, and HUD binding.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private List<WeaponBase> _weapons = new List<WeaponBase>();
    [SerializeField] private int _startingWeaponIndex = 0;

    [Header("HUD")]
    [SerializeField] private HUDManager _hudManager;

    private int _currentWeaponIndex = -1;
    private WeaponBase _currentWeapon;

    public event Action<WeaponBase> OnWeaponSwitched;
    public event Action<int, int> OnAmmoChanged;

    public WeaponBase CurrentWeapon => _currentWeapon;
    public int CurrentWeaponIndex => _currentWeaponIndex;
    public IReadOnlyList<WeaponBase> Weapons => _weapons;

    private void Awake()
    {
        if (_weaponHolder == null) _weaponHolder = transform;
        if (_hudManager == null) _hudManager = FindFirstObjectByType<HUDManager>();

        // Auto-discover child weapons if list is empty
        if (_weapons.Count == 0)
        {
            var childWeapons = GetComponentsInChildren<WeaponBase>(true);
            _weapons.AddRange(childWeapons);
        }

        // Pre-initialize all weapons immediately
        for (int i = 0; i < _weapons.Count; i++)
        {
            if (_weapons[i] != null && _weapons[i].Data != null)
            {
                _weapons[i].Initialize(_weapons[i].Data);
            }
        }
    }

    private void Start()
    {
        if (_weapons.Count > 0)
        {
            SwitchWeapon(Mathf.Clamp(_startingWeaponIndex, 0, _weapons.Count - 1));
        }
    }

    private void Update()
    {
        HandleWeaponSwitchInput();
    }

    private void HandleWeaponSwitchInput()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            // 1. Direct slot keys (1, 2, 3...)
            if (kb.digit1Key.wasPressedThisFrame && _weapons.Count > 0) SwitchWeapon(0);
            if (kb.digit2Key.wasPressedThisFrame && _weapons.Count > 1) SwitchWeapon(1);
            if (kb.digit3Key.wasPressedThisFrame && _weapons.Count > 2) SwitchWeapon(2);
        }

        // 2. Scroll wheel cycling
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0.5f) NextWeapon();
            else if (scroll < -0.5f) PreviousWeapon();
        }
    }

    public void TryFire(bool isHeld)
    {
        if (_currentWeapon == null) return;

        // If held down, only fire if full-auto is enabled
        if (isHeld)
        {
            if (_currentWeapon.Data != null && _currentWeapon.Data.isFullAuto)
            {
                _currentWeapon.Fire();
            }
        }
        else
        {
            // Initial click / tap always triggers
            _currentWeapon.Fire();
        }
    }

    public void TryReload()
    {
        if (_currentWeapon != null && _currentWeapon.CanReload())
        {
            _currentWeapon.Reload();
        }
    }

    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Count) return;
        if (index == _currentWeaponIndex && _currentWeapon != null && _currentWeapon.gameObject.activeSelf) return;

        // Unsubscribe from previous weapon
        if (_currentWeapon != null)
        {
            _currentWeapon.CancelReload();
            _currentWeapon.OnAmmoChanged -= HandleAmmoChanged;
            _currentWeapon.gameObject.SetActive(false);
        }

        _currentWeaponIndex = index;
        _currentWeapon = _weapons[_currentWeaponIndex];

        if (_currentWeapon != null)
        {
            _currentWeapon.gameObject.SetActive(true);
            _currentWeapon.OnAmmoChanged += HandleAmmoChanged;

            // Notify HUD & listeners
            HandleAmmoChanged(_currentWeapon.CurrentAmmo, _currentWeapon.ReserveAmmo);
            UpdateHUDWeaponInfo();
            OnWeaponSwitched?.Invoke(_currentWeapon);
        }
    }

    /// <summary>Re-announces the current weapon (HUD ammo, name, listeners) after its data was replaced.</summary>
    public void RefreshCurrent()
    {
        if (_currentWeapon == null) return;
        HandleAmmoChanged(_currentWeapon.CurrentAmmo, _currentWeapon.ReserveAmmo);
        UpdateHUDWeaponInfo();
        OnWeaponSwitched?.Invoke(_currentWeapon);
    }

    public void NextWeapon()
    {
        if (_weapons.Count <= 1) return;
        int nextIndex = (_currentWeaponIndex + 1) % _weapons.Count;
        SwitchWeapon(nextIndex);
    }

    public void PreviousWeapon()
    {
        if (_weapons.Count <= 1) return;
        int prevIndex = (_currentWeaponIndex - 1 + _weapons.Count) % _weapons.Count;
        SwitchWeapon(prevIndex);
    }

    public void AddWeapon(WeaponBase newWeapon)
    {
        if (newWeapon == null || _weapons.Contains(newWeapon)) return;
        _weapons.Add(newWeapon);
        newWeapon.transform.SetParent(_weaponHolder, false);
        newWeapon.gameObject.SetActive(false);

        if (_currentWeapon == null)
        {
            SwitchWeapon(0);
        }
    }

    private void HandleAmmoChanged(int current, int reserve)
    {
        OnAmmoChanged?.Invoke(current, reserve);
        if (_hudManager != null)
        {
            _hudManager.UpdateAmmo(current, reserve);
        }
    }

    private void UpdateHUDWeaponInfo()
    {
        if (_hudManager == null || _currentWeapon == null || _currentWeapon.Data == null) return;
        string wName = _currentWeapon.Data.weaponName;
        string fMode = _currentWeapon.Data.fireModeDescription;
        _hudManager.UpdateWeaponSub(wName, fMode);
    }
}

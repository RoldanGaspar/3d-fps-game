using UnityEngine;
using TMPro;
using Project.UI;

/// <summary>
/// Optimized manager for the in-game procedural vector HUD.
/// Follows game development best practices: zero GC allocations in Update,
/// cached string states, dirty flags, and clean event subscription lifecycle.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Procedural Vector Components")]
    [SerializeField] private VectorSegmentedBar _healthBar;
    [SerializeField] private VectorSegmentedBar _speedBar;
    [SerializeField] private VectorCrosshair _crosshair;

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI _healthText;
    [SerializeField] private TextMeshProUGUI _ammoText;
    [SerializeField] private TextMeshProUGUI _weaponSubText;
    [SerializeField] private TextMeshProUGUI _stateText;

    [Header("Player Tracking")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerMotor _playerMotor;
    [SerializeField] private ParkourStateMachine _parkourStateMachine;

    [Header("Crosshair Settings")]
    [SerializeField] private float _baseGap = 10f;
    [SerializeField] private float _sprintGap = 20f;
    [SerializeField] private float _airborneGap = 28f;

    // Cache state to avoid per-frame GC allocations and UI rebuilds
    private float _currentGap;
    private string _cachedState = "";
    private int _cachedHealth = -1;
    private int _cachedAmmo = -1;
    private int _cachedReserve = -1;
    private float _lastSpeedFill = -1f;

    // Preallocated state strings
    private const string STATE_IDLE = "IDLE";
    private const string STATE_RUNNING = "RUNNING";
    private const string STATE_SPRINTING = "SPRINTING";
    private const string STATE_AIRBORNE = "AIRBORNE";

    private void Awake()
    {
        _currentGap = _baseGap;
    }

    private void Start()
    {
        if (_playerHealth == null) _playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (_playerMotor == null) _playerMotor = FindFirstObjectByType<PlayerMotor>();
        if (_parkourStateMachine == null) _parkourStateMachine = FindFirstObjectByType<ParkourStateMachine>();

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
            OnHealthChanged(_playerHealth.CurrentHealth / Mathf.Max(1f, _playerHealth.MaxHealth));
        }

        var wm = FindFirstObjectByType<WeaponManager>();
        if (wm != null && wm.CurrentWeapon != null)
        {
            UpdateAmmo(wm.CurrentWeapon.CurrentAmmo, wm.CurrentWeapon.ReserveAmmo);
        }
        else
        {
            UpdateAmmo(30, 90);
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
        }
    }

    private void Update()
    {
        UpdateMovementFeedback();
    }

    private void UpdateMovementFeedback()
    {
        if (_playerMotor == null) return;

        float normSpeed = _playerMotor.GetNormalizedSpeed();

        // 1. Update Speed Bar only when fill delta is meaningful
        if (_speedBar != null && Mathf.Abs(_lastSpeedFill - normSpeed) > 0.01f)
        {
            _lastSpeedFill = normSpeed;
            _speedBar.fillPercent = normSpeed;
            _speedBar.SetVerticesDirty();
        }

        // 2. Determine State Label without string allocations
        string stateLabel;
        float targetGap;

        if (_parkourStateMachine != null)
        {
            stateLabel = _parkourStateMachine.CurrentStateName;
            switch (stateLabel)
            {
                case "SLIDING":
                case "WALL RUN":
                case "WALL JUMP":
                case "VAULT":
                case "MANTLE":
                case "DOUBLE JUMP":
                    targetGap = _sprintGap * 1.25f;
                    break;
                case "AIRBORNE":
                    targetGap = _airborneGap;
                    break;
                case "SPRINTING":
                    targetGap = _sprintGap;
                    break;
                case "RUNNING":
                    targetGap = Mathf.Lerp(_baseGap, _sprintGap, normSpeed);
                    break;
                default:
                    targetGap = _baseGap;
                    break;
            }
        }
        else if (!_playerMotor.IsGrounded)
        {
            stateLabel = STATE_AIRBORNE;
            targetGap = _airborneGap;
        }
        else if (_playerMotor.IsSprinting)
        {
            stateLabel = STATE_SPRINTING;
            targetGap = _sprintGap;
        }
        else if (normSpeed > 0.05f)
        {
            stateLabel = STATE_RUNNING;
            targetGap = Mathf.Lerp(_baseGap, _sprintGap, normSpeed);
        }
        else
        {
            stateLabel = STATE_IDLE;
            targetGap = _baseGap;
        }

        // Only update text mesh when state actually changes
        if (_cachedState != stateLabel)
        {
            _cachedState = stateLabel;
            if (_stateText != null)
            {
                _stateText.text = stateLabel;
            }
        }

        // 3. Smooth Crosshair Reticle Bloom
        if (_crosshair != null)
        {
            float newGap = Mathf.Lerp(_currentGap, targetGap, Time.deltaTime * 12f);
            if (Mathf.Abs(newGap - _currentGap) > 0.08f)
            {
                _currentGap = newGap;
                _crosshair.gap = _currentGap;
                _crosshair.SetVerticesDirty();
            }
        }
    }

    private void OnHealthChanged(float normalizedHealth)
    {
        if (_healthBar != null)
        {
            _healthBar.fillPercent = normalizedHealth;
            _healthBar.SetVerticesDirty();
        }

        if (_healthText != null && _playerHealth != null)
        {
            int hpInt = Mathf.CeilToInt(_playerHealth.CurrentHealth);
            if (hpInt != _cachedHealth)
            {
                _cachedHealth = hpInt;
                _healthText.text = hpInt.ToString();
            }
        }
    }

    public void UpdateAmmo(int current, int reserve)
    {
        if (current == _cachedAmmo && reserve == _cachedReserve) return;
        _cachedAmmo = current;
        _cachedReserve = reserve;

        if (_ammoText != null)
        {
            _ammoText.text = $"{current} / {reserve}";
        }
    }

    public void UpdateWeaponSub(string weaponName, string fireMode)
    {
        if (_weaponSubText != null)
        {
            _weaponSubText.text = $"{weaponName} // {fireMode}";
        }
    }
}

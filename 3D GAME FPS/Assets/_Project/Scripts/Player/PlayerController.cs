using UnityEngine;

/// <summary>
/// Main player controller. Reads the unified InputManager and drives locomotion, parkour, weapons and aim.
/// Jump and slide presses are buffered for a moment, so pressing a hair early (just before landing) still
/// works. This is a big part of why mobile shooters feel responsive.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerMotor))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerLook _look;
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private ParkourStateMachine _parkourStateMachine;
    [SerializeField] private WeaponManager _weaponManager;

    [Header("Mobile UI References")]
    [SerializeField] private FloatingJoystick _joystick;

    [Header("Input Buffering")]
    [SerializeField] private float _jumpBufferTime = 0.15f;
    [SerializeField] private float _slideBufferTime = 0.2f;

    [Header("Head Bob")]
    [SerializeField] private bool _enableHeadBob = true;
    [SerializeField] private float _bobFrequency = 8f;
    [SerializeField] private float _bobAmplitude = 0.05f;
    [SerializeField] private float _sprintBobMultiplier = 1.4f;

    private float _bobTimer;
    private Vector3 _cameraOriginalLocalPos;
    private float _jumpBuffer;
    private float _slideBuffer;
    private bool _aiming;
    private float _cameraDrop, _cameraDropTarget;
    private float _landBump;
    private PlayerAudio _audio;

    private void Start()
    {
        if (_motor == null) _motor = GetComponent<PlayerMotor>();
        if (_look == null) _look = GetComponent<PlayerLook>();
        if (_health == null) _health = GetComponent<PlayerHealth>();
        if (_parkourStateMachine == null) _parkourStateMachine = GetComponent<ParkourStateMachine>();
        if (_weaponManager == null) _weaponManager = GetComponentInChildren<WeaponManager>();

        if (_joystick == null)
            _joystick = FindFirstObjectByType<FloatingJoystick>();

        if (_cameraHolder != null)
            _cameraOriginalLocalPos = _cameraHolder.localPosition;

        // landing makes the view dip, more for a longer fall
        _audio = GetComponent<PlayerAudio>();
        if (_audio != null) _audio.OnLanded += HandleLanded;
    }

    private void OnDestroy()
    {
        if (_audio != null) _audio.OnLanded -= HandleLanded;
    }

    private void HandleLanded(Vector3 at, float intensity)
    {
        _landBump = Mathf.Max(_landBump, Mathf.Clamp01(intensity));
        if (_look != null) _look.KickRoll(Random.Range(-1f, 1f) * 1.5f * intensity);
    }

    private void Update()
    {
        HandleInput();
        HandleHeadBob();
    }

    /// <summary>Lowers the camera smoothly (slide, crouch). Pass 0 to return to standing height.</summary>
    public void SetCameraDrop(float metres) => _cameraDropTarget = metres;

    private void HandleInput()
    {
        var input = InputManager.Instance;
        if (input == null || _motor == null) return;

        Vector2 inputVec = input.MoveInput;
        bool sprintRequested = input.IsSprintRequested;

        _motor.SetJumpHeld(input.JumpHeld);

        if (input.JumpTriggered) _jumpBuffer = _jumpBufferTime;
        if (input.SlideTriggered) _slideBuffer = _slideBufferTime;

        // Slide first, then jump: a jump pressed in the same frame cancels the slide into a slide-jump.
        if (_slideBuffer > 0f)
        {
            if (TrySlide()) _slideBuffer = 0f;
            else _slideBuffer -= Time.deltaTime;
        }

        if (_jumpBuffer > 0f)
        {
            if (TryJump()) _jumpBuffer = 0f;
            else _jumpBuffer -= Time.deltaTime;
        }

        if (input.FireTriggered) ExecuteFire(false);
        else if (input.FireHeld) ExecuteFire(true);

        if (input.ReloadTriggered) ExecuteReload();
        if (input.CameraToggleTriggered) ToggleCamera();

        UpdateAim(input.AimHeld && !_motor.IsSprinting);

        Vector3 moveDir = transform.right * inputVec.x + transform.forward * inputVec.y;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        _motor.SetMoveDirection(moveDir);
        _motor.SetSprint(sprintRequested && moveDir.sqrMagnitude > 0.05f);
    }

    private void UpdateAim(bool aim)
    {
        if (aim == _aiming) return;
        _aiming = aim;
        var weapon = _weaponManager != null ? _weaponManager.CurrentWeapon : null;
        if (weapon != null) weapon.AimDownSights(aim);
    }

    private void HandleHeadBob()
    {
        if (_cameraHolder == null) return;

        _cameraDrop = Mathf.Lerp(_cameraDrop, _cameraDropTarget, 1f - Mathf.Exp(-14f * Time.deltaTime));
        _landBump = Mathf.MoveTowards(_landBump, 0f, Time.deltaTime * 4.5f);
        Vector3 basePos = _cameraOriginalLocalPos + Vector3.down * (_cameraDrop + _landBump * 0.16f);

        float speed = _motor.GetNormalizedSpeed();
        bool bob = _enableHeadBob && _motor.IsGrounded && speed > 0.1f && _cameraDropTarget < 0.01f;

        if (bob)
        {
            float bobMultiplier = _motor.IsSprinting ? _sprintBobMultiplier : 1f;
            _bobTimer += Time.deltaTime * _bobFrequency * bobMultiplier;

            float bobY = Mathf.Sin(_bobTimer) * _bobAmplitude * bobMultiplier;
            float bobX = Mathf.Cos(_bobTimer * 0.5f) * _bobAmplitude * 0.5f * bobMultiplier;

            _cameraHolder.localPosition = Vector3.Lerp(_cameraHolder.localPosition, basePos + new Vector3(bobX, bobY, 0f), Time.deltaTime * 10f);
        }
        else
        {
            _bobTimer = 0f;
            _cameraHolder.localPosition = Vector3.Lerp(_cameraHolder.localPosition, basePos, Time.deltaTime * 10f);
        }
    }

    // --- Locomotion ---

    /// <summary>Returns true when the jump (or a vault, mantle, wall jump, double jump) was carried out.</summary>
    private bool TryJump()
    {
        if (_parkourStateMachine != null && _parkourStateMachine.TryJump()) return true;

        // Falling right above the ground: keep the press buffered and jump on landing instead of
        // burning the double jump a moment too early.
        if (!_motor.IsGrounded && !_motor.CanGroundJump && _motor.Velocity.y < 0f && _motor.DistanceToGround(0.8f) < 0.7f)
            return false;

        return _motor.Jump();
    }

    private bool TrySlide()
    {
        return _parkourStateMachine != null && _parkourStateMachine.TrySlide();
    }

    public void ExecuteJump()
    {
        if (!TryJump()) _jumpBuffer = _jumpBufferTime;
    }

    public void ExecuteSlide()
    {
        if (!TrySlide()) _slideBuffer = _slideBufferTime;
    }

    public void ExecuteFire(bool isHeld = false)
    {
        if (_weaponManager != null)
        {
            _weaponManager.TryFire(isHeld);
        }
    }

    public void ExecuteReload()
    {
        if (_weaponManager != null)
        {
            _weaponManager.TryReload();
        }
    }

    private void ToggleCamera()
    {
        var cam = GetComponent<CameraModeController>();
        if (cam != null) cam.ToggleMode();
    }

    // Direct UI action callbacks (invoked by TouchButtons / HUD)
    public void OnJumpPressed()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportJumpDown();
        else ExecuteJump();
    }

    public void OnJumpReleased()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportJumpUp();
    }

    public void OnSprintToggle(bool sprint)
    {
        _motor?.SetSprint(sprint);
    }

    public void OnSlidePressed()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportSlideDown();
        else ExecuteSlide();
    }

    public void OnSlideReleased()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportSlideUp();
    }

    public void OnFirePressed()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportFireDown();
        else ExecuteFire(false);
    }

    public void OnFireReleased()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportFireUp();
    }

    public void OnAimPressed()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportAimDown();
    }

    public void OnAimReleased()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportAimUp();
    }

    public void OnReloadPressed()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportReload();
        else ExecuteReload();
    }

    public void OnReloadReleased()
    {
    }

    public void OnCameraTogglePressed()
    {
        if (InputManager.Instance != null) InputManager.Instance.ReportCameraToggle();
        else ToggleCamera();
    }
}

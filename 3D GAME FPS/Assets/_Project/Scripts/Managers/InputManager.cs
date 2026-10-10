using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum InputMode
{
    AutoDetect,         // Automatically switches when keyboard/mouse vs touch is used
    ForceKeyboardMouse, // Always PC / Desktop mode
    ForceTouch          // Always Mobile Touch mode
}

public enum ActiveDevice
{
    KeyboardMouse,
    Touch
}

/// <summary>
/// One place that turns keyboard, mouse, gamepad and the on-screen touch controls into the same set of
/// properties. Reads the Input System package directly, so it works in the editor, on desktop and on phones.
///
/// Keyboard and mouse:  W A S D move, Shift sprint, Space jump, C or Ctrl slide, left mouse fire,
///                      right mouse aim, R reload, V camera, 1 2 3 weapons, Esc or P pause, F1 input mode.
/// Gamepad:             left stick move, right stick look, A jump, B slide, X reload, RT fire, LT aim,
///                      left stick click sprint, Y camera.
///
/// Runs before every other script (execution order) so that a press is seen in the very frame it happens.
/// </summary>
[DefaultExecutionOrder(-200)]
public class InputManager : Singleton<InputManager>
{
    [Header("Input Configuration")]
    [SerializeField] private InputMode _mode = InputMode.AutoDetect;
    [SerializeField] private ActiveDevice _currentDevice = ActiveDevice.Touch;

    [Header("Look Tuning")]
    [Tooltip("Degrees of turn per pixel of mouse movement (before the Settings sensitivity scale).")]
    [SerializeField] private float _degreesPerPixel = 0.1f;
    [Tooltip("Degrees per second at full right-stick deflection.")]
    [SerializeField] private float _gamepadLookSpeed = 180f;

    [Header("Touch References")]
    [SerializeField] private FloatingJoystick _virtualJoystick;

    public event Action<ActiveDevice> OnActiveDeviceChanged;

    // --- Unified state read by the gameplay scripts ---
    public ActiveDevice CurrentDevice => _currentDevice;
    public InputMode Mode { get => _mode; set => SetMode(value); }

    public Vector2 MoveInput { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public bool IsSprintRequested { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool FireTriggered { get; private set; }
    public bool FireHeld { get; private set; }
    public bool SlideTriggered { get; private set; }
    public bool ReloadTriggered { get; private set; }
    public bool CameraToggleTriggered { get; private set; }
    public bool AimHeld { get; private set; }

    /// <summary>True when game input (move, look, fire) should be processed.</summary>
    public bool GameplayEnabled { get; private set; } = true;

    // --- Touch buffers, filled by the on-screen controls ---
    private Vector2 _touchLookDelta;
    private bool _touchJumpTriggered, _touchJumpHeld;
    private bool _touchFireTriggered, _touchFireHeld;
    private bool _touchSlideTriggered;
    private bool _touchReloadTriggered;
    private bool _touchCameraToggleTriggered;
    private bool _touchAimHeld;
    private bool _touchActivityThisFrame;

    private bool _cursorLocked;
    private bool _hadRealInput;    // true once the player actually used a device, after that we follow their lead

    private void Start()
    {
        if (_virtualJoystick == null) _virtualJoystick = FindFirstObjectByType<FloatingJoystick>();

        // Phones start in touch mode, everything else (editor, PC) starts with keyboard and mouse.
        // UnityEngine.Device.Application also answers "mobile" inside the editor's Device Simulator, so the
        // on-screen joystick and buttons show up there too (mouse clicks act as finger touches).
        _currentDevice = UnityEngine.Device.Application.isMobilePlatform ? ActiveDevice.Touch : ActiveDevice.KeyboardMouse;
        OnActiveDeviceChanged?.Invoke(_currentDevice);
        ApplyCursorState(true);
    }

    private void Update()
    {
        Keyboard kb = Keyboard.current;
        Mouse mouse = Mouse.current;
        Gamepad pad = Gamepad.current;

        if (kb != null && kb.f1Key.wasPressedThisFrame)
        {
            SetMode((InputMode)(((int)_mode + 1) % 3));
            Debug.Log($"[InputManager] Input mode: {_mode}");
        }

        GameplayEnabled = IsGameplayState();

        DetectActiveDevice(kb, mouse, pad);
        Aggregate(kb, mouse, pad);
        ApplyCursorState(false);
    }

    private void LateUpdate()
    {
        _touchLookDelta = Vector2.zero;
        _touchJumpTriggered = false;
        _touchFireTriggered = false;
        _touchSlideTriggered = false;
        _touchReloadTriggered = false;
        _touchCameraToggleTriggered = false;
        _touchActivityThisFrame = false;

        JumpTriggered = false;
        FireTriggered = false;
        SlideTriggered = false;
        ReloadTriggered = false;
        CameraToggleTriggered = false;
    }

    // ---------------------------------------------------------------------------------------------
    // Game state and cursor
    // ---------------------------------------------------------------------------------------------

    private static bool IsGameplayState()
    {
        var gm = GameManager.Instance;
        // Scenes opened directly in the editor have no state change yet, so only block when we know it is a menu.
        if (gm == null) return true;
        var s = gm.CurrentState;
        return s == GameManager.GameState.Playing && !PerkPickerUI.IsOpen;
    }

    /// <summary>The mouse is captured only while actually playing with keyboard and mouse.</summary>
    private void ApplyCursorState(bool force)
    {
        bool wantLock = _currentDevice == ActiveDevice.KeyboardMouse && GameplayEnabled && Application.isFocused;
        if (!force && wantLock == _cursorLocked) return;
        SetCursorLock(wantLock);
    }

    public void SetCursorLock(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    // ---------------------------------------------------------------------------------------------
    // Device detection
    // ---------------------------------------------------------------------------------------------

    private void DetectActiveDevice(Keyboard kb, Mouse mouse, Gamepad pad)
    {
        if (_mode == InputMode.ForceKeyboardMouse) { SetActiveDevice(ActiveDevice.KeyboardMouse); return; }
        if (_mode == InputMode.ForceTouch) { SetActiveDevice(ActiveDevice.Touch); return; }

        if (_touchActivityThisFrame)
        {
            _hadRealInput = true;
            SetActiveDevice(ActiveDevice.Touch);
            return;
        }

        // Until the player touches something, use the default for this platform. This is evaluated every
        // frame because the Device Simulator may only report "mobile" a moment after start-up.
        if (!_hadRealInput)
            SetActiveDevice(UnityEngine.Device.Application.isMobilePlatform ? ActiveDevice.Touch : ActiveDevice.KeyboardMouse);

        // A real key press or a decent mouse move switches back to keyboard and mouse.
        // In the Device Simulator the mouse is the finger, so only the keyboard can switch.
        bool simulatingPhone = UnityEngine.Device.Application.isMobilePlatform;
        bool keyboardActivity = kb != null && kb.anyKey.wasPressedThisFrame && !kb.escapeKey.wasPressedThisFrame;
        bool mouseActivity = !simulatingPhone && mouse != null && (mouse.delta.ReadValue().sqrMagnitude > 4f || mouse.leftButton.wasPressedThisFrame);
        if (keyboardActivity || mouseActivity)
        {
            _hadRealInput = true;
            SetActiveDevice(ActiveDevice.KeyboardMouse);
        }
    }

    public void SetMode(InputMode newMode)
    {
        _mode = newMode;
        if (_mode == InputMode.ForceKeyboardMouse) SetActiveDevice(ActiveDevice.KeyboardMouse);
        else if (_mode == InputMode.ForceTouch) SetActiveDevice(ActiveDevice.Touch);
    }

    public void SetActiveDevice(ActiveDevice newDevice)
    {
        if (_currentDevice == newDevice) return;
        _currentDevice = newDevice;
        ApplyCursorState(true);
        OnActiveDeviceChanged?.Invoke(_currentDevice);
    }

    // ---------------------------------------------------------------------------------------------
    // Aggregation
    // ---------------------------------------------------------------------------------------------

    private void Aggregate(Keyboard kb, Mouse mouse, Gamepad pad)
    {
        if (!GameplayEnabled)
        {
            MoveInput = Vector2.zero;
            LookDelta = Vector2.zero;
            IsSprintRequested = false;
            JumpHeld = false;
            FireHeld = false;
            AimHeld = false;
            return;
        }

        // 1. Move. Raw, with no smoothing: the motor does the acceleration, so the response is immediate.
        Vector2 kbMove = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) kbMove.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) kbMove.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) kbMove.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) kbMove.x -= 1f;
        }
        Vector2 padMove = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
        Vector2 joyMove = _virtualJoystick != null ? _virtualJoystick.Direction : Vector2.zero;

        if (joyMove.sqrMagnitude > 0.001f)
        {
            MoveInput = Vector2.ClampMagnitude(joyMove, 1f);
            ReportTouchActivity();
        }
        else if (kbMove.sqrMagnitude > 0.001f)
        {
            MoveInput = kbMove.normalized;
        }
        else if (padMove.sqrMagnitude > 0.04f)
        {
            MoveInput = Vector2.ClampMagnitude(padMove, 1f);
        }
        else
        {
            MoveInput = Vector2.zero;
        }

        // 2. Sprint (hold Shift while moving forward, or the joystick sprint zone)
        bool kbSprint = kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed) && MoveInput.y > 0.1f;
        bool padSprint = pad != null && pad.leftStickButton.isPressed && MoveInput.y > 0.1f;
        bool joySprint = _virtualJoystick != null && _virtualJoystick.IsSprinting;
        IsSprintRequested = kbSprint || padSprint || joySprint;

        // 3. Jump
        bool kbJumpDown = kb != null && kb.spaceKey.wasPressedThisFrame;
        bool padJumpDown = pad != null && pad.buttonSouth.wasPressedThisFrame;
        JumpTriggered = kbJumpDown || padJumpDown || _touchJumpTriggered;
        JumpHeld = (kb != null && kb.spaceKey.isPressed) || (pad != null && pad.buttonSouth.isPressed) || _touchJumpHeld;

        // 4. Slide
        bool kbSlide = kb != null && (kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame);
        bool padSlide = pad != null && pad.buttonEast.wasPressedThisFrame;
        SlideTriggered = kbSlide || padSlide || _touchSlideTriggered;

        // 5. Fire and aim
        bool mouseOn = mouse != null && _currentDevice == ActiveDevice.KeyboardMouse;
        bool mouseDown = mouseOn && mouse.leftButton.wasPressedThisFrame;
        bool mouseHeld = mouseOn && mouse.leftButton.isPressed;
        bool padFireDown = pad != null && pad.rightTrigger.wasPressedThisFrame;
        bool padFireHeld = pad != null && pad.rightTrigger.isPressed;
        FireTriggered = mouseDown || padFireDown || _touchFireTriggered;
        FireHeld = mouseHeld || padFireHeld || _touchFireHeld;
        AimHeld = (mouseOn && mouse.rightButton.isPressed) || (pad != null && pad.leftTrigger.isPressed) || _touchAimHeld;

        // 6. Reload and camera
        ReloadTriggered = (kb != null && kb.rKey.wasPressedThisFrame) || (pad != null && pad.buttonWest.wasPressedThisFrame) || _touchReloadTriggered;
        CameraToggleTriggered = (kb != null && kb.vKey.wasPressedThisFrame) || (pad != null && pad.buttonNorth.wasPressedThisFrame) || _touchCameraToggleTriggered;

        // 7. Look: raw mouse pixels times degrees per pixel, so there is no smoothing delay and no frame-rate dependence.
        if (_touchLookDelta.sqrMagnitude > 0.0001f)
        {
            LookDelta = _touchLookDelta;
            ReportTouchActivity();
        }
        else if (mouseOn)
        {
            LookDelta = mouse.delta.ReadValue() * _degreesPerPixel;
        }
        else
        {
            LookDelta = Vector2.zero;
        }

        if (pad != null)
        {
            Vector2 stick = pad.rightStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f)
            {
                // squared response curve: fine aim near the centre, fast turn at the edge
                LookDelta += stick * stick.magnitude * _gamepadLookSpeed * Time.unscaledDeltaTime;
            }
        }
    }

    // ---------------------------------------------------------------------------------------------
    // Touch callbacks, called by the on-screen controls
    // ---------------------------------------------------------------------------------------------

    public void ReportTouchActivity() => _touchActivityThisFrame = true;

    public void ReportTouchLook(Vector2 delta)
    {
        _touchLookDelta += delta;
        ReportTouchActivity();
    }

    public void ReportJump() => ReportJumpDown();

    public void ReportJumpDown()
    {
        _touchJumpTriggered = true;
        _touchJumpHeld = true;
        ReportTouchActivity();
    }

    public void ReportJumpUp()
    {
        _touchJumpHeld = false;
        ReportTouchActivity();
    }

    public void ReportFireDown()
    {
        _touchFireTriggered = true;
        _touchFireHeld = true;
        ReportTouchActivity();
    }

    public void ReportFireUp()
    {
        _touchFireHeld = false;
        ReportTouchActivity();
    }

    public void ReportAimDown()
    {
        _touchAimHeld = true;
        ReportTouchActivity();
    }

    public void ReportAimUp()
    {
        _touchAimHeld = false;
        ReportTouchActivity();
    }

    public void ReportSlide() => ReportSlideDown();

    public void ReportSlideDown()
    {
        _touchSlideTriggered = true;
        ReportTouchActivity();
    }

    public void ReportSlideUp() => ReportTouchActivity();

    public void ReportReload()
    {
        _touchReloadTriggered = true;
        ReportTouchActivity();
    }

    public void ReportCameraToggle()
    {
        _touchCameraToggleTriggered = true;
        ReportTouchActivity();
    }

    public void SetVirtualJoystick(FloatingJoystick joystick) => _virtualJoystick = joystick;
}

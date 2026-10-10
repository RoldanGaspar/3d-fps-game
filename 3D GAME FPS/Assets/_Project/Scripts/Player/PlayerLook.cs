using UnityEngine;

/// <summary>
/// First-person camera controller with unified mouse, touch and gamepad support via InputManager.
/// Look input is applied raw and straight away (no smoothing) so aiming reacts the moment the player moves.
/// Also owns the camera roll: wall runs lean the view into the wall, strafing leans it slightly.
/// </summary>
public class PlayerLook : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float _mouseSensitivity = 2f;
    [SerializeField] private float _touchSensitivity = 0.15f;

    [Header("Clamp")]
    [SerializeField] private float _minVerticalAngle = -85f;
    [SerializeField] private float _maxVerticalAngle = 85f;

    [Header("Camera Roll")]
    [Tooltip("Degrees of lean when strafing at full speed. 0 turns it off.")]
    [SerializeField] private float _strafeRoll = 1.2f;

    [Header("References")]
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private Transform _playerBody;

    private float _xRotation;
    private float _yRotation;

    private float _roll;
    private float _rollTarget;
    private float _rollSpeed = 10f;
    private float _kick;               // short extra roll from impacts, decays on its own
    private PlayerMotor _motor;

    public float Pitch => _xRotation;
    public float Yaw => _yRotation;

    private void Start()
    {
        _motor = GetComponent<PlayerMotor>();

        // If no InputManager is present in the scene, default to locked cursor for desktop play
        if (InputManager.Instance == null)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        if (InputManager.Instance != null)
        {
            ApplyLookDelta(InputManager.Instance.LookDelta);
        }
    }

    public void ApplyLookDelta(Vector2 delta)
    {
        delta *= GameSettings.LookScale;

        _yRotation += delta.x;
        _xRotation -= delta.y;
        _xRotation = Mathf.Clamp(_xRotation, _minVerticalAngle, _maxVerticalAngle);

        // roll: the parkour target, plus a little lean into a strafe, plus any impact kick
        float strafe = 0f;
        if (_motor != null && _strafeRoll > 0f && _motor.IsGrounded)
        {
            float side = Vector3.Dot(_motor.HorizontalVelocity, transform.right);
            strafe = -Mathf.Clamp(side / Mathf.Max(0.1f, _motor.SprintSpeed), -1f, 1f) * _strafeRoll;
        }

        float dt = Time.unscaledDeltaTime;
        float target = _rollTarget + strafe;
        _roll = Mathf.Lerp(_roll, target, 1f - Mathf.Exp(-_rollSpeed * dt));
        _kick = Mathf.Lerp(_kick, 0f, 1f - Mathf.Exp(-9f * dt));

        if (_cameraHolder != null)
            _cameraHolder.localRotation = Quaternion.Euler(_xRotation, 0f, _roll + _kick);

        if (_playerBody != null)
            _playerBody.localRotation = Quaternion.Euler(0f, _yRotation, 0f);
    }

    public void ApplyTouchLook(Vector2 touchDelta)
    {
        ApplyLookDelta(touchDelta * _touchSensitivity);
    }

    public void SetSensitivity(float sensitivity)
    {
        _mouseSensitivity = sensitivity;
    }

    /// <summary>Sets the roll the view eases toward (degrees, positive leans right). Call with 0 to level out.</summary>
    public void SetCameraTilt(float tiltAngle, float speed)
    {
        _rollTarget = tiltAngle;
        _rollSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>A quick roll jolt that fades away, for landings and hits.</summary>
    public void KickRoll(float degrees)
    {
        _kick += degrees;
    }
}

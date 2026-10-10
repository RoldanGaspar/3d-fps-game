using UnityEngine;

/// <summary>
/// [DEPRECATED] Legacy root controller. Unified input is now managed by InputManager.
/// </summary>
[System.Obsolete("Deprecated in favor of unified InputManager.")]
public class MobileInputUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatingJoystick _moveJoystick;
    [SerializeField] private TouchButton _fireButton;
    [SerializeField] private TouchButton _jumpButton;
    [SerializeField] private TouchButton _slideButton;
    [SerializeField] private TouchButton _reloadButton;
    [SerializeField] private TouchButton _adsButton;

    public Vector2 MoveInput => _moveJoystick != null ? _moveJoystick.Direction : Vector2.zero;

    // TODO: Wire button events to player systems
}

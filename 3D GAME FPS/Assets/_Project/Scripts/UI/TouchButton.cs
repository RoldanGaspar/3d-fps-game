using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

/// <summary>
/// Reusable touch button component with press/release events.
/// Reports touch activity to InputManager to ensure instant device detection.
///
/// With "Drag To Look" on (the fire button), keeping the finger on the button and sliding it also turns the
/// camera, so the player can shoot and aim with the same thumb, like in Call of Duty Mobile. Every finger is
/// tracked on its own, so this works together with the joystick and the look pad.
/// </summary>
public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IInitializePotentialDragHandler, IDragHandler
{
    public UnityEvent OnPressed = new UnityEvent();
    public UnityEvent OnReleased = new UnityEvent();

    [Header("Drag To Look")]
    [SerializeField] private bool _dragToLook;
    [Tooltip("Multiplier on top of the look pad sensitivity. 1 = same speed as the look pad.")]
    [SerializeField] private float _lookMultiplier = 1f;

    public bool IsPressed { get; private set; }
    public bool DragToLook { get => _dragToLook; set => _dragToLook = value; }

    private int _pointerId = int.MinValue;
    private Project.UI.TouchLookZone _lookZone;

    public void OnPointerDown(PointerEventData eventData)
    {
        // Only the first finger on a button counts; a second finger on the same button is ignored.
        if (IsPressed) return;

        IsPressed = true;
        _pointerId = eventData.pointerId;
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ReportTouchActivity();
        }
        OnPressed?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IsPressed || eventData.pointerId != _pointerId) return;

        IsPressed = false;
        _pointerId = int.MinValue;
        OnReleased?.Invoke();
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // start turning the camera at once, not after the usual drag distance
        if (_dragToLook) eventData.useDragThreshold = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragToLook || !IsPressed || eventData.pointerId != _pointerId) return;
        var input = InputManager.Instance;
        if (input == null) return;

        if (_lookZone == null) _lookZone = FindFirstObjectByType<Project.UI.TouchLookZone>();
        float sensitivity = _lookZone != null ? _lookZone.Sensitivity : 0.15f;
        input.ReportTouchLook(eventData.delta * (sensitivity * _lookMultiplier));
    }

    private void OnDisable()
    {
        // never leave a button stuck down if the HUD is hidden while a finger is on it
        if (IsPressed)
        {
            IsPressed = false;
            _pointerId = int.MinValue;
            OnReleased?.Invoke();
        }
    }
}

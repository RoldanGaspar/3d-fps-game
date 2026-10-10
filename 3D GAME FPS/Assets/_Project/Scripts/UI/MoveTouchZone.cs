using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Invisible touch sector covering the left region of the screen.
    /// Captures touches anywhere in the sector to dynamically anchor and drive FloatingJoystick.
    /// Reports touch activity to InputManager for seamless adaptive switching.
    /// </summary>
    public class MoveTouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private FloatingJoystick _joystick;

        // The first finger in the zone owns the joystick; other fingers are ignored until it lifts.
        private int _activePointerId = int.MinValue;

        private void Start()
        {
            if (_joystick == null)
                _joystick = FindFirstObjectByType<FloatingJoystick>();
        }

        public void SetJoystick(FloatingJoystick joystick)
        {
            _joystick = joystick;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId != int.MinValue) return;
            _activePointerId = eventData.pointerId;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.ReportTouchActivity();
            }

            if (_joystick != null)
            {
                _joystick.OnSectorPointerDown(eventData);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.ReportTouchActivity();
            }

            if (_joystick != null)
            {
                _joystick.OnSectorDrag(eventData);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;
            _activePointerId = int.MinValue;

            if (_joystick != null)
            {
                _joystick.OnSectorPointerUp(eventData);
            }
        }
    }
}

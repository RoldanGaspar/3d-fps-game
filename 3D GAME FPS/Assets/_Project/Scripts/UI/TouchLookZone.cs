using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Right-side touch drag zone for mobile camera aiming.
    /// Feeds swipe delta into InputManager (or directly to PlayerLook as fallback).
    /// Tracks active pointerId to prevent multi-touch camera snapping/jitter.
    /// </summary>
    public class TouchLookZone : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float _touchSensitivity = 0.15f;
        [SerializeField] private PlayerLook _playerLook;

        private int _activePointerId = int.MinValue;

        /// <summary>Degrees of camera turn per pixel of finger movement (also used by the fire button's drag-to-look).</summary>
        public float Sensitivity => _touchSensitivity;

        private void Start()
        {
            if (_playerLook == null) _playerLook = FindFirstObjectByType<PlayerLook>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_activePointerId == int.MinValue)
            {
                _activePointerId = eventData.pointerId;
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.ReportTouchActivity();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _activePointerId) return;

            if (InputManager.Instance != null)
            {
                InputManager.Instance.ReportTouchLook(eventData.delta * _touchSensitivity);
            }
            else if (_playerLook != null)
            {
                _playerLook.ApplyTouchLook(eventData.delta);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _activePointerId)
            {
                _activePointerId = int.MinValue;
            }
        }
    }
}

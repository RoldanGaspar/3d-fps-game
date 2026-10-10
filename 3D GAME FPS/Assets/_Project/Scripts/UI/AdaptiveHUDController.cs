using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Smoothly adapts the HUD presentation between Desktop (Keyboard/Mouse)
    /// and Mobile (Touch) control schemes.
    /// In Desktop mode, smoothly fades mobile touch controls to invisible (alpha = 0).
    /// In Touch mode, smoothly fades mobile touch controls back to fully visible (alpha = 1).
    /// </summary>
    public class AdaptiveHUDController : MonoBehaviour
    {
        [Header("Target Groups")]
        [SerializeField] private CanvasGroup _mobileControlsGroup;

        [Header("Animation Settings")]
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _desktopAlpha = 0.0f; // Clean Auto-Fade per user selection
        [SerializeField] private float _touchAlpha = 1.0f;

        private float _targetAlpha = 1.0f;

        private void Start()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActiveDeviceChanged += HandleActiveDeviceChanged;
                // Initialize to current device
                HandleActiveDeviceChanged(InputManager.Instance.CurrentDevice);
            }
        }

        private void OnDestroy()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.OnActiveDeviceChanged -= HandleActiveDeviceChanged;
            }
        }

        private void Update()
        {
            if (_mobileControlsGroup == null) return;

            if (Mathf.Abs(_mobileControlsGroup.alpha - _targetAlpha) > 0.001f)
            {
                float speed = 1f / Mathf.Max(0.01f, _fadeDuration);
                _mobileControlsGroup.alpha = Mathf.MoveTowards(_mobileControlsGroup.alpha, _targetAlpha, Time.unscaledDeltaTime * speed);

                bool isInteractable = _mobileControlsGroup.alpha > 0.05f;
                _mobileControlsGroup.interactable = isInteractable;
                _mobileControlsGroup.blocksRaycasts = isInteractable;
            }
        }

        public void HandleActiveDeviceChanged(ActiveDevice device)
        {
            _targetAlpha = (device == ActiveDevice.KeyboardMouse) ? _desktopAlpha : _touchAlpha;
            if (_mobileControlsGroup != null && _fadeDuration <= 0f)
            {
                _mobileControlsGroup.alpha = _targetAlpha;
                _mobileControlsGroup.interactable = _targetAlpha > 0.05f;
                _mobileControlsGroup.blocksRaycasts = _targetAlpha > 0.05f;
            }
        }

        public void SetMobileControlsGroup(CanvasGroup group)
        {
            _mobileControlsGroup = group;
        }
    }
}

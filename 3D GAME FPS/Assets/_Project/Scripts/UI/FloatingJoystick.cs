using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Industry-grade enhanced virtual joystick for mobile FPS.
/// Features:
/// 1. Dynamic floating screen-sector anchoring with thumb-follow base drift.
/// 2. Radial deadzone (0.15) and polynomial response curve (x^1.5).
/// 3. Dynamic "Pull-to-Sprint" zone with Auto-Sprint Lock node.
/// 4. Haptic feedback and smooth home-position return.
/// </summary>
public class FloatingJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Core References")]
    [SerializeField] private RectTransform _background;
    [SerializeField] private RectTransform _handle;
    [SerializeField] private RectTransform _sprintLockNode;

    [Header("Input Tuning")]
    [SerializeField] private float _handleRange = 70f;
    [SerializeField] private float _deadzone = 0.15f;
    [SerializeField] private float _curveExponent = 1.5f;
    [SerializeField] private float _dampingSpeed = 20f;

    [Header("Auto-Sprint & Lock")]
    [SerializeField] private float _sprintThreshold = 0.82f;
    [SerializeField] private float _sprintLockDistance = 1.35f;
    [SerializeField] private bool _enableHaptics = true;

    [Header("Dynamic Floating & Base Drift")]
    [SerializeField] private bool _enableFloating = true;
    [SerializeField] private bool _enableBaseDrift = true;
    [SerializeField] private float _driftThresholdMultiplier = 1.25f;
    [SerializeField] private float _driftFollowSpeed = 10f;
    [SerializeField] private float _returnHomeSpeed = 12f;

    private Vector2 _rawInput;
    private Vector2 _curvedInput;
    private Vector2 _smoothedInput;
    private Vector2 _defaultAnchoredPosition;
    private RectTransform _parentCanvasRect;
    private Camera _canvasCamera;
    private bool _isDragging;
    private bool _isSprinting;
    private bool _inLockZone;
    private bool _isSprintLocked;
    private bool _hapticTriggered;

    // Public properties
    public Vector2 Direction => _isSprintLocked ? Vector2.up : _smoothedInput;
    public Vector2 RawInput => _rawInput;
    public Vector2 CurvedInput => _curvedInput;
    public bool IsSprinting => _isSprintLocked || _isSprinting;
    public bool IsSprintLocked => _isSprintLocked;
    public bool IsDragging => _isDragging;

    private Canvas _parentCanvas;

    private Camera CanvasCamera
    {
        get
        {
            if (_parentCanvas == null) _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas != null && _parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                return _parentCanvas.worldCamera != null ? _parentCanvas.worldCamera : Camera.main;
            }
            return null;
        }
    }

    private void Awake()
    {
        _parentCanvas = GetComponentInParent<Canvas>();
        if (_parentCanvas != null)
        {
            _parentCanvasRect = _parentCanvas.transform as RectTransform;
            _canvasCamera = _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_parentCanvas.worldCamera != null ? _parentCanvas.worldCamera : Camera.main);
        }

        if (_handle != null)
        {
            _handle.anchoredPosition = Vector2.zero;
        }

        if (_background != null)
        {
            _defaultAnchoredPosition = _background.anchoredPosition;
            if (_defaultAnchoredPosition == Vector2.zero)
            {
                _defaultAnchoredPosition = new Vector2(175f, 175f);
            }
        }
        else
        {
            _defaultAnchoredPosition = new Vector2(175f, 175f);
        }
    }

    private void Update()
    {
        UpdateInputSmoothing();
        UpdateHomeReturn();
        UpdateSprintNodeVisuals();
    }

    private void UpdateInputSmoothing()
    {
        if (_isSprintLocked)
        {
            _smoothedInput = Vector2.Lerp(_smoothedInput, Vector2.up, Time.deltaTime * _dampingSpeed);
            return;
        }

        if (_isDragging)
        {
            _smoothedInput = Vector2.Lerp(_smoothedInput, _curvedInput, Time.deltaTime * _dampingSpeed);
        }
        else
        {
            _smoothedInput = Vector2.Lerp(_smoothedInput, Vector2.zero, Time.deltaTime * _dampingSpeed);
        }
    }

    private void UpdateHomeReturn()
    {
        if (!_isDragging && _background != null)
        {
            _background.anchoredPosition = Vector2.Lerp(
                _background.anchoredPosition, 
                _defaultAnchoredPosition, 
                Time.deltaTime * _returnHomeSpeed
            );
        }
    }

    private void UpdateSprintNodeVisuals()
    {
        if (_sprintLockNode == null) return;

        float targetScale = _isSprintLocked ? 1.15f : (_inLockZone ? 1.1f : (_isSprinting ? 1.0f : 0.9f));
        _sprintLockNode.localScale = Vector3.Lerp(_sprintLockNode.localScale, Vector3.one * targetScale, Time.deltaTime * 15f);
    }

    // --- Sector Touch Handlers (Invoked by MoveTouchZone or direct) ---

    public void OnSectorPointerDown(PointerEventData eventData)
    {
        // Tapping while sprint locked immediately cancels the lock
        if (_isSprintLocked)
        {
            CancelSprintLock();
            return;
        }

        if (_enableFloating && _background != null && _parentCanvasRect != null)
        {
            Vector3 worldPoint;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_parentCanvasRect, eventData.position, CanvasCamera, out worldPoint))
            {
                _background.position = worldPoint;
            }
        }

        OnPointerDown(eventData);
    }

    public void OnSectorDrag(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnSectorPointerUp(PointerEventData eventData)
    {
        OnPointerUp(eventData);
    }

    // --- Core IDragHandler / IPointer Interfaces ---

    public void OnPointerDown(PointerEventData eventData)
    {
        // Direct tap on joystick while sprint locked cancels lock
        if (_isSprintLocked)
        {
            CancelSprintLock();
            return;
        }

        _isDragging = true;
        _hapticTriggered = false;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.ReportTouchActivity();
        }

        ProcessDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        _isDragging = true;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.ReportTouchActivity();
        }

        ProcessDrag(eventData);
    }

    private void ProcessDrag(PointerEventData eventData)
    {
        if (_background == null || _handle == null) return;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_background, eventData.position, CanvasCamera, out localPoint))
            return;

        // Pulling back/down while sprint locked releases lock
        if (_isSprintLocked && localPoint.y < -_handleRange * 0.35f)
        {
            CancelSprintLock();
            return;
        }

        float rawDistance = localPoint.magnitude;
        Vector2 normalizedDir = rawDistance > 0.001f ? localPoint / rawDistance : Vector2.zero;

        // 1. Base Follow / Drift
        if (_enableBaseDrift && _parentCanvasRect != null)
        {
            float driftThreshold = _handleRange * _driftThresholdMultiplier;
            if (rawDistance > driftThreshold)
            {
                float excess = rawDistance - driftThreshold;
                Vector2 shift = normalizedDir * (excess * Time.deltaTime * _driftFollowSpeed);
                _background.anchoredPosition += shift;
            }
        }

        // 2. Sprint Lock Zone Check (Vertical extension above range)
        float maxExtension = _handleRange * _sprintLockDistance;
        bool inUpperSector = localPoint.y > (_handleRange * 0.7f);
        _inLockZone = inUpperSector && (rawDistance >= _handleRange * 1.15f);

        // Clamp handle visual position
        float handleClampDist = _inLockZone ? Mathf.Min(rawDistance, maxExtension) : Mathf.Min(rawDistance, _handleRange);
        _handle.anchoredPosition = normalizedDir * handleClampDist;

        // 3. Normalized Magnitude & Deadzone Curve
        float normalizedMag = Mathf.Clamp01(rawDistance / _handleRange);

        if (normalizedMag < _deadzone)
        {
            _rawInput = Vector2.zero;
            _curvedInput = Vector2.zero;
            _isSprinting = false;
        }
        else
        {
            // Remap [deadzone -> 1.0] to [0.0 -> 1.0]
            float remapped = Mathf.Clamp01((normalizedMag - _deadzone) / (1f - _deadzone));
            // Apply polynomial power curve
            float curvedMag = Mathf.Pow(remapped, _curveExponent);

            _rawInput = normalizedDir * normalizedMag;
            _curvedInput = normalizedDir * curvedMag;

            // Sprint threshold evaluation
            bool sprintCandidate = (normalizedMag >= _sprintThreshold && normalizedDir.y > 0.65f) || _inLockZone;
            if (sprintCandidate && !_isSprinting)
            {
                TriggerHaptic();
            }
            _isSprinting = sprintCandidate;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;

        // If thumb was released inside the sprint lock zone -> ENGAGE SPRINT LOCK
        if (_inLockZone)
        {
            _isSprintLocked = true;
            _inLockZone = false;
            TriggerHaptic();
            if (_handle != null)
            {
                _handle.anchoredPosition = Vector2.up * _handleRange;
            }
            return;
        }

        // Normal release
        _rawInput = Vector2.zero;
        _curvedInput = Vector2.zero;
        _isSprinting = false;
        _inLockZone = false;

        if (_handle != null)
        {
            _handle.anchoredPosition = Vector2.zero;
        }
    }

    public void CancelSprintLock()
    {
        _isSprintLocked = false;
        _inLockZone = false;
        _isSprinting = false;
        _rawInput = Vector2.zero;
        _curvedInput = Vector2.zero;
        _smoothedInput = Vector2.zero;

        if (_handle != null)
        {
            _handle.anchoredPosition = Vector2.zero;
        }
    }

    private void TriggerHaptic()
    {
        if (!_enableHaptics) return;

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}

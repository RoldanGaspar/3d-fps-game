using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Cinemachine 3.x-powered Camera Mode Controller.
/// Provides smooth AAA camera blends between First-Person and Third-Person views,
/// smart wall deocclusion / collision, character model shadow culling,
/// and screen impulse recoil.
/// </summary>
public class CameraModeController : MonoBehaviour
{
    public enum CameraMode { FirstPerson, ThirdPerson }

    [Header("Cinemachine References")]
    [SerializeField] private CinemachineBrain _brain;
    [SerializeField] private CinemachineCamera _fpsVcam;
    [SerializeField] private CinemachineCamera _tpsVcam;

    [Header("Player & Model References")]
    [SerializeField] private Transform _cameraHolder;
    [SerializeField] private Transform _characterModel;
    [SerializeField] private Transform _weaponHolder;
    [SerializeField] private Transform _thirdPersonWeaponHolder;

    [Header("Blend Settings")]
    [SerializeField] private float _blendDuration = 0.35f;

    [Header("Impulse (Recoil & Shake)")]
    [SerializeField] private CinemachineImpulseSource _impulseSource;

    // --- Runtime ---
    private CameraMode _currentMode = CameraMode.FirstPerson;
    public CameraMode CurrentMode => _currentMode;
    public CinemachineCamera FpsVcam => _fpsVcam;
    public CinemachineCamera TpsVcam => _tpsVcam;

    private Coroutine _blendCoroutine;

    // Events
    public event System.Action<CameraMode> OnCameraModeChanged;

    private void Awake()
    {
        if (_brain == null && Camera.main != null)
        {
            _brain = Camera.main.GetComponent<CinemachineBrain>();
        }

        if (_brain != null)
        {
            _brain.DefaultBlend.Time = _blendDuration;
            _brain.DefaultBlend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
        }

        // Initialize in FPS mode
        ApplyMode(CameraMode.FirstPerson, instant: true);
    }

    [Header("Debounce Tuning")]
    [SerializeField] private float _toggleCooldown = 0.35f;
    private float _lastToggleTime = -1f;

    // The V key / gamepad Y / touch button all arrive through InputManager and PlayerController.ToggleCamera.

    /// <summary>
    /// Toggle between First-Person and Third-Person view with debounce protection.
    /// </summary>
    public void ToggleMode()
    {
        if (Time.unscaledTime - _lastToggleTime < _toggleCooldown) return;
        _lastToggleTime = Time.unscaledTime;

        var newMode = _currentMode == CameraMode.FirstPerson
            ? CameraMode.ThirdPerson
            : CameraMode.FirstPerson;
        ApplyMode(newMode, instant: false);
    }

    public void ApplyMode(CameraMode mode, bool instant)
    {
        _currentMode = mode;
        bool isFPS = mode == CameraMode.FirstPerson;

        if (_blendCoroutine != null)
        {
            StopCoroutine(_blendCoroutine);
            _blendCoroutine = null;
        }

        if (isFPS)
        {
            // Switch priority to FPS virtual camera
            if (_fpsVcam != null) _fpsVcam.Priority.Value = 20;
            if (_tpsVcam != null) _tpsVcam.Priority.Value = 0;

            if (instant)
            {
                SetWeaponViewModelVisibility(true);
                SetCharacterVisibility(false);
                if (_thirdPersonWeaponHolder != null) _thirdPersonWeaponHolder.gameObject.SetActive(false);
            }
            else
            {
                _blendCoroutine = StartCoroutine(TransitionToFPSRoutine());
            }
        }
        else
        {
            // Switch priority to TPS virtual camera
            if (_tpsVcam != null) _tpsVcam.Priority.Value = 20;
            if (_fpsVcam != null) _fpsVcam.Priority.Value = 0;

            // In TPS mode: immediately reveal character and 3P weapon, hide FPS viewmodel
            SetWeaponViewModelVisibility(false);
            SetCharacterVisibility(true);
            if (_thirdPersonWeaponHolder != null) _thirdPersonWeaponHolder.gameObject.SetActive(true);
        }

        OnCameraModeChanged?.Invoke(mode);
    }

    private IEnumerator TransitionToFPSRoutine()
    {
        // Wait for the Cinemachine blend to approach completion before hiding character head/body
        yield return new WaitForSeconds(_blendDuration * 0.75f);

        SetWeaponViewModelVisibility(true);
        SetCharacterVisibility(false);
        if (_thirdPersonWeaponHolder != null) _thirdPersonWeaponHolder.gameObject.SetActive(false);
        _blendCoroutine = null;
    }

    private void SetWeaponViewModelVisibility(bool visible)
    {
        if (_weaponHolder == null) return;
        var renderers = _weaponHolder.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = visible;
        }
    }

    private void SetCharacterVisibility(bool fullyVisible)
    {
        if (_characterModel == null) return;

        var renderers = _characterModel.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            r.enabled = true;
            r.shadowCastingMode = fullyVisible
                ? UnityEngine.Rendering.ShadowCastingMode.On
                : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }

    /// <summary>
    /// Triggers a screen shake impulse (for weapon recoil, landing impact, explosion).
    /// </summary>
    public void GenerateImpulse(float force = 1f)
    {
        if (_impulseSource != null)
        {
            _impulseSource.GenerateImpulse(Vector3.down * force);
        }
    }

    /// <summary>
    /// In Cinemachine setup, Main Camera is always the active rendering camera.
    /// </summary>
    public Camera ActiveCamera => Camera.main;

    /// <summary>
    /// Returns true if currently in third-person mode.
    /// </summary>
    public bool IsThirdPerson => _currentMode == CameraMode.ThirdPerson;

    // --- Editor Wire Helper ---
    public void ConfigureReferences(CinemachineBrain brain, CinemachineCamera fps, CinemachineCamera tps,
                                    Transform camHolder, Transform charModel, Transform weaponHolder,
                                    Transform tpWeaponHolder, CinemachineImpulseSource impulse)
    {
        _brain = brain;
        _fpsVcam = fps;
        _tpsVcam = tps;
        _cameraHolder = camHolder;
        _characterModel = charModel;
        _weaponHolder = weaponHolder;
        _thirdPersonWeaponHolder = tpWeaponHolder;
        _impulseSource = impulse;
    }
}

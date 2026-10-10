using System.Collections;
using UnityEngine;

/// <summary>
/// What happens when the player dies: controls lock, the character collapses as a ragdoll while the camera
/// pulls out to third person in slow motion, then the game over screen appears. Also handles reviving.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerDeathController : MonoBehaviour
{
    [SerializeField] private float _slowMotionScale = 0.35f;
    [SerializeField] private float _slowMotionTime = 1.2f;
    [SerializeField] private float _gameOverDelay = 2.2f;

    private PlayerHealth _health;
    private CharacterController _controller;
    private Animator _animator;
    private RagdollController _ragdoll;
    private CameraModeController _camera;
    private MonoBehaviour[] _controlScripts;
    private GameObject _mobileControls;
    private Transform _originalFollow;
    private Transform _originalLookAt;
    private CameraModeController.CameraMode _modeBeforeDeath;
    private bool _dead;
    private Coroutine _routine;
    private Transform _deathCamTarget;
    private Transform _deathCamHips;
    private float _deathCamYaw;

    public bool IsDead => _dead;

    private void LateUpdate()
    {
        if (!_dead || _deathCamTarget == null || _deathCamHips == null) return;

        float dt = Time.unscaledDeltaTime;
        _deathCamYaw += 14f * dt;
        _deathCamTarget.position = Vector3.Lerp(_deathCamTarget.position, _deathCamHips.position + Vector3.up * 0.6f, 1f - Mathf.Exp(-6f * dt));
        _deathCamTarget.rotation = Quaternion.Euler(22f, _deathCamYaw, 0f);
    }

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _controller = GetComponent<CharacterController>();
        _camera = GetComponent<CameraModeController>();
        _animator = GetComponentInChildren<Animator>();
        _ragdoll = GetComponent<RagdollController>();

        _controlScripts = new MonoBehaviour[]
        {
            GetComponent<PlayerController>(),
            GetComponent<PlayerLook>(),
            GetComponent<PlayerMotor>(),
            GetComponent<ParkourStateMachine>(),
            GetComponent<PlayerAudio>(),
            GetComponentInChildren<WeaponManager>(true)
        };
    }

    private void OnEnable()
    {
        _health.OnDeath.AddListener(HandleDeath);
    }

    private void OnDisable()
    {
        _health.OnDeath.RemoveListener(HandleDeath);
    }

    private void HandleDeath()
    {
        if (_dead) return;
        _dead = true;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        SetControl(false);

        Transform hips = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.Hips) : transform;

        // Camera: pull out to third person and look at the body.
        if (_camera != null)
        {
            _modeBeforeDeath = _camera.CurrentMode;
            _camera.ApplyMode(CameraModeController.CameraMode.ThirdPerson, true);
            if (_camera.TpsVcam != null)
            {
                _originalFollow = _camera.TpsVcam.Follow;
                _originalLookAt = _camera.TpsVcam.LookAt;

                // An upright helper that slowly orbits the body: the lying ragdoll's own rotation would
                // put the third person camera under the floor.
                if (_deathCamTarget == null) _deathCamTarget = new GameObject("DeathCamTarget").transform;
                _deathCamTarget.position = hips.position + Vector3.up * 0.6f;
                _deathCamTarget.rotation = Quaternion.Euler(22f, transform.eulerAngles.y + 160f, 0f);
                _deathCamHips = hips;
                _deathCamYaw = _deathCamTarget.eulerAngles.y;
                _camera.TpsVcam.Follow = _deathCamTarget;
                _camera.TpsVcam.LookAt = _deathCamTarget;
            }
        }

        // Ragdoll collapse, pushed back from where the player was facing.
        if (_ragdoll != null && _ragdoll.IsBuilt)
        {
            Vector3 push = -transform.forward * 35f + Vector3.up * 6f;
            _ragdoll.Activate(hips.position + Vector3.up * 0.3f, push);
        }

        SetTimeScale(_slowMotionScale);
        yield return new WaitForSecondsRealtime(_slowMotionTime);
        SetTimeScale(1f);

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, _gameOverDelay - _slowMotionTime));
        if (GameManager.Instance != null) GameManager.Instance.GameOver();
        _routine = null;
    }

    /// <summary>Stands the player back up (called from the game over screen) with half health and a grace period.</summary>
    public void Revive()
    {
        if (!_dead) return;
        if (_routine != null) { StopCoroutine(_routine); _routine = null; }
        SetTimeScale(1f);

        Transform hips = _animator != null ? _animator.GetBoneTransform(HumanBodyBones.Hips) : transform;
        Vector3 standAt = hips.position + Vector3.up * 1.1f;

        if (_ragdoll != null) _ragdoll.Deactivate();

        // The ragdoll slid away from the capsule; move the capsule to where the body lies.
        _controller.enabled = false;
        transform.position = standAt;
        _controller.enabled = true;

        if (_camera != null)
        {
            if (_camera.TpsVcam != null)
            {
                _camera.TpsVcam.Follow = _originalFollow;
                _camera.TpsVcam.LookAt = _originalLookAt;
            }
            _camera.ApplyMode(_modeBeforeDeath, true);
        }

        if (_deathCamTarget != null) Destroy(_deathCamTarget.gameObject);
        _deathCamTarget = null;

        _health.Revive(0.5f, 3f);
        SetControl(true);
        _dead = false;
    }

    private void SetControl(bool on)
    {
        foreach (var s in _controlScripts)
            if (s != null) s.enabled = on;

        if (_controller != null) _controller.enabled = on;

        if (_mobileControls == null)
        {
            var t = GameObject.Find("MobileControlsGroup");
            if (t != null) _mobileControls = t;
        }
        if (_mobileControls != null) _mobileControls.SetActive(on);
    }

    private static void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * Mathf.Max(0.05f, scale);
    }
}

using UnityEngine;

/// <summary>
/// Parkour and movement sounds for the player: surface-aware footsteps, jump, landing, slide,
/// wall-run scuffs and the hurt grunt. Everything is driven by existing player components.
/// </summary>
[RequireComponent(typeof(PlayerMotor))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    [SerializeField] private float _walkStride = 2.0f;
    [SerializeField] private float _sprintStride = 2.7f;
    [SerializeField] private float _footstepVolume = 0.35f;
    [SerializeField] private float _wallRunInterval = 0.28f;

    [Header("Landing")]
    [SerializeField] private float _minLandAirTime = 0.25f;
    [SerializeField] private float _landVolume = 0.6f;

    /// <summary>Raised on every footstep: world position at the feet, surface, and whether the player is sprinting.</summary>
    public event System.Action<Vector3, SurfaceType, bool> OnFootstep;
    /// <summary>Raised when landing after a real fall: world position at the feet and intensity 0..1.</summary>
    public event System.Action<Vector3, float> OnLanded;

    private PlayerMotor _motor;
    private CharacterController _controller;
    private ParkourStateMachine _parkour;
    private PlayerHealth _health;
    private AudioSource _source;

    private bool _wasGrounded = true;
    private float _airTime;
    private float _peakFall;
    private float _stepDistance;
    private float _nextWallRunStep;
    private string _lastState = "";

    private void Awake()
    {
        _motor = GetComponent<PlayerMotor>();
        _controller = GetComponent<CharacterController>();
        _parkour = GetComponent<ParkourStateMachine>();
        _health = GetComponent<PlayerHealth>();

        _source = gameObject.AddComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDamaged.AddListener(OnDamaged);
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged.RemoveListener(OnDamaged);
    }

    private void Update()
    {
        bool grounded = _motor.IsGrounded;
        string state = _parkour != null ? _parkour.CurrentStateName : "";

        UpdateAirEvents(grounded);
        UpdateStateEvents(state);
        UpdateFootsteps(grounded, state);

        _wasGrounded = grounded;
        _lastState = state;
    }

    private void UpdateAirEvents(bool grounded)
    {
        float vy = _controller != null ? _controller.velocity.y : _motor.Velocity.y;

        if (_wasGrounded && !grounded && vy > 1.5f)
            Play(SfxId.Jump, 0.45f);

        if (!grounded)
        {
            _airTime += Time.deltaTime;
            _peakFall = Mathf.Max(_peakFall, -vy);
        }
        else if (!_wasGrounded)
        {
            if (_airTime >= _minLandAirTime)
            {
                float intensity = Mathf.InverseLerp(2f, 14f, _peakFall);
                Play(SfxId.Land, Mathf.Lerp(0.3f, 1f, intensity) * _landVolume, Random.Range(0.9f, 1.05f));
                OnLanded?.Invoke(FeetPosition(), intensity);
            }
            _airTime = 0f;
            _peakFall = 0f;
            _stepDistance = 0f;
        }
        else
        {
            _airTime = 0f;
            _peakFall = 0f;
        }
    }

    private void UpdateStateEvents(string state)
    {
        if (state == _lastState) return;

        switch (state)
        {
            case "SLIDING":
                Play(SfxId.Slide, 0.55f);
                break;
            case "VAULT":
            case "MANTLE":
            case "DOUBLE JUMP":
            case "WALL JUMP":
                Play(SfxId.Jump, 0.5f, Random.Range(0.85f, 1f));
                break;
        }
    }

    private void UpdateFootsteps(bool grounded, string state)
    {
        if (state == "WALL RUN")
        {
            if (Time.time >= _nextWallRunStep)
            {
                _nextWallRunStep = Time.time + _wallRunInterval;
                Play(SfxId.FootstepMetal, _footstepVolume * 0.8f, 0.8f);
            }
            return;
        }

        if (!grounded || state == "SLIDING" || state == "VAULT" || state == "MANTLE") return;

        Vector3 v = _controller != null ? _controller.velocity : _motor.Velocity;
        v.y = 0f;
        float speed = v.magnitude;
        if (speed < 0.5f) return;

        _stepDistance += speed * Time.deltaTime;
        float stride = _motor.IsSprinting ? _sprintStride : _walkStride;
        if (_stepDistance < stride) return;

        _stepDistance = 0f;
        SurfaceType surface = SurfaceUnderfoot();
        float volume = _footstepVolume * (state == "CROUCH" ? 0.5f : 1f) * (_motor.IsSprinting ? 1.25f : 1f);
        Play(ProceduralSfx.FootstepFor(surface), volume, Random.Range(0.9f, 1.1f));
        OnFootstep?.Invoke(FeetPosition(), surface, _motor.IsSprinting);
    }

    private Vector3 FeetPosition()
    {
        float down = _controller != null ? _controller.height * 0.5f - _controller.center.y : 1f;
        return transform.position + Vector3.down * down;
    }

    private SurfaceType SurfaceUnderfoot()
    {
        float height = _controller != null ? _controller.height * 0.5f + 0.3f : 1.3f;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, height, ~0, QueryTriggerInteraction.Ignore))
            return SurfaceMarker.Resolve(hit.collider);
        return SurfaceType.Concrete;
    }

    private void OnDamaged()
    {
        Play(SfxId.PlayerHurt, 0.7f, Random.Range(0.92f, 1.05f));
    }

    private void Play(SfxId id, float volume = 1f, float pitch = 1f)
    {
        _source.pitch = pitch;
        _source.PlayOneShot(ProceduralSfx.Get(id), volume * GameAudio.Sfx);
    }
}

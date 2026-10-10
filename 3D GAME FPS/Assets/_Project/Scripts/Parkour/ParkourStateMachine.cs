using UnityEngine;

/// <summary>
/// Controls parkour state transitions and manages locomotion coordination.
/// Supports sliding, wall running, wall jumping, vaulting, mantling, and double jumping.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ParkourStateMachine : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParkourDetector _detector;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerLook _look;
    [SerializeField] private ParkourSettings _settings;

    private ParkourState _currentState;
    private CharacterController _controller;
    private float _nextSlideTime;
    private Collider _blockedWall;
    private float _wallBlockUntil;

    public ParkourDetector Detector => _detector;
    public PlayerMotor Motor => _motor;
    public PlayerLook Look => _look;
    public CharacterController Controller => _controller;
    public ParkourSettings Settings => _settings;
    public ParkourState CurrentState => _currentState;

    /// <summary>Raised the moment an air jump happens (the state itself ends in the same frame).</summary>
    public event System.Action DoubleJumped;
    public void RaiseDoubleJump() => DoubleJumped?.Invoke();

    private PlayerAnimator _animation;
    /// <summary>Character animation driver (may be null in test scenes without a character).</summary>
    public PlayerAnimator Animation
    {
        get
        {
            if (_animation == null) _animation = GetComponent<PlayerAnimator>();
            return _animation;
        }
    }

    private PlayerController _player;
    public PlayerController Player
    {
        get
        {
            if (_player == null) _player = GetComponent<PlayerController>();
            return _player;
        }
    }

    public string CurrentStateName
    {
        get
        {
            if (_currentState != null) return _currentState.StateName;
            if (_motor != null)
            {
                if (!_motor.IsGrounded) return "AIRBORNE";
                if (_motor.IsSprinting) return "SPRINTING";
                if (_motor.GetNormalizedSpeed() > 0.05f) return "RUNNING";
            }
            return "IDLE";
        }
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_detector == null) _detector = GetComponent<ParkourDetector>();
        if (_motor == null) _motor = GetComponent<PlayerMotor>();
        if (_look == null) _look = GetComponent<PlayerLook>();

        if (_settings == null)
        {
            _settings = ScriptableObject.CreateInstance<ParkourSettings>();
        }
    }

    private void Start()
    {
        ChangeState(new RunningState(this, _controller));
    }

    private void Update()
    {
        _currentState?.Update();
    }

    private void FixedUpdate()
    {
        _currentState?.FixedUpdate();
    }

    public void ChangeState(ParkourState newState)
    {
        if (newState == null) return;
        _currentState?.Exit();
        _currentState = newState;
        _currentState?.Enter();
    }

    /// <summary>The player holds jump, or is still rising: grabbing a ledge in the air is intended.</summary>
    public bool WantsToGrab()
    {
        var input = InputManager.Instance;
        return (input != null && input.JumpHeld) || (_motor != null && _motor.Velocity.y > 0.5f);
    }

    // --- Wall run bookkeeping: do not re-attach to the wall we just left ---

    public void BlockWall(Collider wall, float seconds)
    {
        _blockedWall = wall;
        _wallBlockUntil = Time.time + seconds;
    }

    public bool IsWallBlocked(Collider wall) => wall == _blockedWall && Time.time < _wallBlockUntil;

    // --- Actions ---

    public bool TrySlide()
    {
        if (_currentState is SlidingState || _currentState is VaultState || _currentState is MantleState) return false;
        if (Time.time < _nextSlideTime) return false;
        if (_motor == null || !_motor.IsGrounded) return false;

        // sliding needs some speed; standing still does nothing
        bool moving = _motor.CurrentSpeed > 2.5f || _motor.MoveDirection.sqrMagnitude > 0.05f;
        if (!moving) return false;

        float cooldown = _settings != null ? Mathf.Min(_settings.slideCooldown, 0.5f) : 0.5f;
        _nextSlideTime = Time.time + cooldown;
        ChangeState(new SlidingState(this, _controller));
        return true;
    }

    /// <summary>
    /// Handles the jump button for parkour. Returns true when it did something (wall jump, slide jump, vault,
    /// mantle, double jump); false means the normal motor jump should run.
    /// </summary>
    public bool TryJump()
    {
        if (_currentState is VaultState || _currentState is MantleState) return true;   // already busy: swallow the press

        if (_currentState is WallRunState wrs)
        {
            BlockWall(wrs.WallHit.collider, 0.6f);
            ChangeState(new WallJumpState(this, _controller, wrs.WallHit.normal));
            return true;
        }

        if (_currentState is SlidingState)
        {
            // slide jump: leave the slide and keep the speed
            ChangeState(new AirborneState(this, _controller));
            _motor?.Jump();
            return true;
        }

        // Jumping at something you can climb: vault or mantle instead of a plain hop.
        if (_detector != null && _currentState != null)
        {
            if (_detector.CheckObstacle(out var frontHit, out var topHit, out _, out bool isVault, out bool isMantle))
            {
                if (isVault) { ChangeState(new VaultState(this, _controller, frontHit, topHit)); return true; }
                if (isMantle) { ChangeState(new MantleState(this, _controller, frontHit, topHit)); return true; }
            }
        }

        if (_currentState is AirborneState)
        {
            // still inside the coyote window after a ledge: this is a normal jump, not the air jump
            if (_motor != null && _motor.CanGroundJump) return false;

            if (_motor != null && _motor.AirJumpsRemaining > 0)
            {
                _motor.AirJumpsRemaining--;
                ChangeState(new DoubleJumpState(this, _controller));
                return true;
            }
        }

        return false;
    }
}

using UnityEngine;

/// <summary>
/// Handles player movement physics: velocity, gravity, ground check, sprint, jump.
/// Tuned for a responsive "arcade shooter" feel (think Call of Duty Mobile):
///  - constant acceleration, so a key press reaches full speed in about 0.1 s and a release stops just as fast
///  - momentum is kept in the air (a slide jump or wall jump carries its speed)
///  - coyote time: a jump still works for a moment after walking off a ledge
///  - hold jump for a high jump, tap for a short hop; falling is a bit faster than rising
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 5f;
    [SerializeField] private float _sprintSpeed = 8.5f;
    [Tooltip("Metres per second gained per second on the ground. Higher is snappier.")]
    [SerializeField] private float _groundAcceleration = 60f;
    [SerializeField] private float _groundDeceleration = 70f;
    [SerializeField] private float _airAcceleration = 18f;
    [Tooltip("How fast momentum fades in the air when no key is held. Low keeps the jump distance.")]
    [SerializeField] private float _airDeceleration = 2.5f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float _jumpForce = 7.5f;
    [SerializeField] private float _gravity = -22f;
    [SerializeField] private float _fallGravityMultiplier = 1.35f;
    [SerializeField] private float _lowJumpGravityMultiplier = 2.4f;
    [SerializeField] private float _terminalVelocity = -45f;
    [SerializeField] private float _coyoteTime = 0.12f;
    [SerializeField] private int _maxAirJumps = 1;

    [Header("Ground Check")]
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundDistance = 0.35f;
    [SerializeField] private LayerMask _groundMask;

    private CharacterController _controller;
    private Vector3 _velocity;            // vertical part (y) is used for gravity, xz is ignored
    private Vector3 _horizontalVelocity;  // world space xz momentum
    private Vector3 _moveDirection;
    private bool _isGrounded;
    private bool _isSprinting;
    private bool _jumpHeld = true;
    private bool _jumpedSinceGrounded;
    private float _lastGroundedTime = -10f;
    private int _airJumpsRemaining;

    public bool IsGrounded => _isGrounded;
    public bool IsSprinting => _isSprinting;
    public Vector3 Velocity => new Vector3(_horizontalVelocity.x, _velocity.y, _horizontalVelocity.z);
    public Vector3 HorizontalVelocity => _horizontalVelocity;
    public float CurrentSpeed => _horizontalVelocity.magnitude;
    public CharacterController Controller => _controller;

    /// <summary>Multiplies walk and sprint speed (skills and perks).</summary>
    public float SpeedMultiplier { get; set; } = 1f;

    public bool EnableGravity { get; set; } = true;
    public bool EnableMovement { get; set; } = true;
    public float WalkSpeed => _walkSpeed;
    public float SprintSpeed => _sprintSpeed;
    public float JumpForce => _jumpForce;
    public Vector3 MoveDirection => _moveDirection;
    public int AirJumpsRemaining { get => _airJumpsRemaining; set => _airJumpsRemaining = value; }

    /// <summary>True while a normal ground jump is still allowed, including the short coyote window after a ledge.</summary>
    public bool CanGroundJump =>
        _isGrounded || (!_jumpedSinceGrounded && Time.time - _lastGroundedTime <= _coyoteTime);

    public void SetVerticalVelocity(float y) => _velocity.y = y;

    /// <summary>Sets the whole velocity: x and z become the horizontal momentum, y the vertical speed.</summary>
    public void SetVelocity(Vector3 vel)
    {
        _horizontalVelocity = new Vector3(vel.x, 0f, vel.z);
        _velocity.y = vel.y;
    }

    public void AddVelocity(Vector3 vel)
    {
        _horizontalVelocity += new Vector3(vel.x, 0f, vel.z);
        _velocity.y += vel.y;
    }

    public void SetHorizontalVelocity(Vector3 vel) => _horizontalVelocity = new Vector3(vel.x, 0f, vel.z);

    /// <summary>The jump button state, used to cut a jump short when it is released early.</summary>
    public void SetJumpHeld(bool held) => _jumpHeld = held;

    public void SetColliderHeight(float height, float centerY = 0f)
    {
        if (_controller != null)
        {
            _controller.height = height;
            _controller.center = new Vector3(_controller.center.x, centerY, _controller.center.z);
        }
    }

    /// <summary>Distance from the feet to the ground below, or maxDistance when there is none.</summary>
    public float DistanceToGround(float maxDistance = 3f)
    {
        float feetY = transform.position.y + _controller.center.y - _controller.height * 0.5f;
        var origin = new Vector3(transform.position.x, feetY + 0.1f, transform.position.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, maxDistance + 0.1f, _groundMask, QueryTriggerInteraction.Ignore))
            return Mathf.Max(0f, hit.distance - 0.1f);
        return maxDistance;
    }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        if (_groundCheck == null)
        {
            var gc = transform.Find("GroundCheck");
            if (gc != null) _groundCheck = gc;
        }

        if (_groundMask == 0)
        {
            _groundMask = LayerMask.GetMask("Ground", "Obstacle");
        }
        else
        {
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0) _groundMask &= ~(1 << wallLayer);
        }
    }

    private void Update()
    {
        CheckGround();
        ApplyGravity();
        ApplyMovement();
    }

    private void CheckGround()
    {
        bool wasGrounded = _isGrounded;
        Vector3 checkPos = _groundCheck != null
            ? _groundCheck.position
            : transform.position + Vector3.down * (_controller.height * 0.5f);

        bool rayGrounded = false;
        if (Physics.Raycast(checkPos + Vector3.up * 0.15f, Vector3.down, out RaycastHit hit, _groundDistance + 0.2f, _groundMask, QueryTriggerInteraction.Ignore))
        {
            float maxSlope = (_controller != null) ? _controller.slopeLimit : 45f;
            if (Vector3.Angle(hit.normal, Vector3.up) <= maxSlope)
            {
                rayGrounded = true;
            }
        }

        // Not grounded while still moving up fast (just jumped), otherwise the ray would re-ground us for a frame.
        _isGrounded = (_controller.isGrounded || rayGrounded) && _velocity.y <= 0.5f;

        if (_isGrounded)
        {
            _lastGroundedTime = Time.time;
            _jumpedSinceGrounded = false;
            if (!wasGrounded) _airJumpsRemaining = _maxAirJumps;
            if (_velocity.y < 0f) _velocity.y = -2f;
        }
    }

    private void ApplyGravity()
    {
        if (_isGrounded || !EnableGravity) return;

        float g = _gravity;
        if (_velocity.y < 0f) g *= _fallGravityMultiplier;
        else if (!_jumpHeld) g *= _lowJumpGravityMultiplier;   // button released while rising: short hop

        _velocity.y = Mathf.Max(_velocity.y + g * Time.deltaTime, _terminalVelocity);
    }

    private void ApplyMovement()
    {
        if (!EnableMovement) return;

        float dt = Time.deltaTime;
        bool hasInput = _moveDirection.sqrMagnitude > 0.001f;
        float top = (_isSprinting ? _sprintSpeed : _walkSpeed) * SpeedMultiplier;

        if (_isGrounded)
        {
            Vector3 target = hasInput ? _moveDirection * top : Vector3.zero;
            float rate = hasInput ? _groundAcceleration : _groundDeceleration;
            // turning back against the current motion gets extra grip so reversing does not feel icy
            if (hasInput && Vector3.Dot(_horizontalVelocity, target) < 0f) rate *= 1.4f;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, target, rate * dt);
        }
        else if (hasInput)
        {
            // In the air the keys add speed along the pressed direction (never taking away speed you already
            // have there), while sideways momentum such as a wall-jump kick fades slowly.
            Vector3 dir = _moveDirection.normalized;
            float wishSpeed = top * _moveDirection.magnitude;
            float along = Vector3.Dot(_horizontalVelocity, dir);
            Vector3 sideways = _horizontalVelocity - dir * along;

            if (along < wishSpeed) along = Mathf.Min(wishSpeed, along + _airAcceleration * dt);
            sideways = Vector3.MoveTowards(sideways, Vector3.zero, _airDeceleration * dt);
            _horizontalVelocity = dir * along + sideways;
        }
        else
        {
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, Vector3.zero, _airDeceleration * dt);
        }

        Vector3 move = _horizontalVelocity;
        move.y = _velocity.y;

        _controller.Move(move * dt);

        // A wall stops us: do not keep "pushing" into it after the controller has blocked the movement.
        if (_controller.velocity.sqrMagnitude < 0.01f && _horizontalVelocity.sqrMagnitude > 0.01f && _isGrounded)
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, Vector3.zero, _groundDeceleration * dt);
    }

    public void SetMoveDirection(Vector3 direction)
    {
        _moveDirection = direction;
    }

    public void SetSprint(bool sprint)
    {
        _isSprinting = sprint;
    }

    public bool Jump()
    {
        if (CanGroundJump)
        {
            _velocity.y = _jumpForce;
            _isGrounded = false;
            _jumpedSinceGrounded = true;
            _jumpHeld = true;
            return true;
        }
        if (_airJumpsRemaining > 0)
        {
            _velocity.y = _jumpForce * 0.9f;
            _airJumpsRemaining--;
            _jumpHeld = true;
            return true;
        }
        return false;
    }

    public float GetNormalizedSpeed()
    {
        Vector3 horizontalVel = new Vector3(_controller.velocity.x, 0, _controller.velocity.z);
        return Mathf.Clamp01(horizontalVel.magnitude / _sprintSpeed);
    }
}

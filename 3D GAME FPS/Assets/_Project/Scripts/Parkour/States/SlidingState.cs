using UnityEngine;

/// <summary>
/// Slide: a burst of speed that fades out, a small collider so you fit under low gaps, and a lowered camera.
/// The motor's own movement is switched off while sliding, and its momentum is kept up to date, so a
/// slide-jump or the run that follows carries the speed instead of stopping dead.
/// </summary>
public class SlidingState : ParkourState
{
    public override string StateName => "SLIDING";

    private const float EndSpeed = 3.2f;
    private const float SteerDegreesPerSecond = 45f;
    private const float CameraDrop = 0.7f;

    private Vector3 _direction;
    private float _speed;
    private float _timer;
    private float _originalHeight;
    private float _originalCenterY;
    private float _slideHeight;

    public float Speed => _speed;
    public float Progress { get; private set; }

    public SlidingState(ParkourStateMachine stateMachine, CharacterController controller)
        : base(stateMachine, controller) { }

    public override void Enter()
    {
        var motor = StateMachine.Motor;
        _originalHeight = Controller.height;
        _originalCenterY = Controller.center.y;

        // Slide along the way we were already travelling, or the way the player is pushing.
        Vector3 vel = motor != null ? motor.HorizontalVelocity : Vector3.zero;
        if (vel.sqrMagnitude > 2f) _direction = vel.normalized;
        else if (motor != null && motor.MoveDirection.sqrMagnitude > 0.05f) _direction = motor.MoveDirection.normalized;
        else _direction = PlayerTransform.forward;

        var s = StateMachine.Settings;
        float slideSpeed = s != null ? s.slideSpeed : 10f;
        float carried = motor != null ? motor.CurrentSpeed * 1.15f : 0f;
        _speed = Mathf.Min(Mathf.Max(slideSpeed, carried), slideSpeed * 1.3f);
        _timer = s != null ? s.slideDuration : 0.8f;
        _slideHeight = Mathf.Max(1f, s != null ? s.slideColliderHeight : 1f);

        if (motor != null)
        {
            motor.EnableMovement = false;
            motor.SetColliderHeight(_slideHeight, _originalCenterY - (_originalHeight - _slideHeight) * 0.5f);
        }

        StateMachine.Player?.SetCameraDrop(CameraDrop);
    }

    public override void Update()
    {
        var motor = StateMachine.Motor;
        float dt = Time.deltaTime;
        _timer -= dt;

        float duration = StateMachine.Settings != null ? StateMachine.Settings.slideDuration : 0.8f;
        Progress = 1f - Mathf.Clamp01(_timer / Mathf.Max(0.1f, duration));

        // Friction: fast at first, then easing into a crawl.
        _speed = Mathf.Lerp(_speed, EndSpeed, 1f - Mathf.Exp(-3.2f * dt));

        // A little steering with A and D (or the joystick), like a real slide.
        var input = InputManager.Instance;
        if (input != null && Mathf.Abs(input.MoveInput.x) > 0.1f)
            _direction = Quaternion.AngleAxis(input.MoveInput.x * SteerDegreesPerSecond * dt, Vector3.up) * _direction;

        Vector3 motion = _direction * _speed;
        motion.y = motor != null ? motor.Velocity.y : -2f;
        Controller.Move(motion * dt);

        if (motor != null) motor.SetHorizontalVelocity(_direction * _speed);

        if (motor != null && !motor.IsGrounded)
        {
            StateMachine.ChangeState(new AirborneState(StateMachine, Controller));
            return;
        }

        // Stay down while something low is overhead, otherwise stand up when the slide is over.
        if (_timer <= 0f && CanStand())
        {
            if (motor != null && motor.IsSprinting) StateMachine.ChangeState(new SprintingState(StateMachine, Controller));
            else StateMachine.ChangeState(new RunningState(StateMachine, Controller));
        }
    }

    public override void Exit()
    {
        var motor = StateMachine.Motor;
        if (motor != null)
        {
            motor.SetColliderHeight(_originalHeight, _originalCenterY);
            motor.EnableMovement = true;
        }
        StateMachine.Player?.SetCameraDrop(0f);
    }

    private bool CanStand()
    {
        float extra = _originalHeight - _slideHeight;
        if (extra <= 0.01f) return true;
        float r = Controller.radius * 0.9f;
        Vector3 top = PlayerTransform.position + Controller.center + Vector3.up * (_slideHeight * 0.5f - r);
        // level geometry only: the character's own ragdoll colliders must not count as a ceiling
        int mask = LayerMask.GetMask("Ground", "Obstacle", "Wall");
        return !Physics.SphereCast(top, r, Vector3.up, out _, extra + 0.05f, mask, QueryTriggerInteraction.Ignore);
    }
}

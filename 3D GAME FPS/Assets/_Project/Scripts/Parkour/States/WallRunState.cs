using UnityEngine;

/// <summary>
/// Wall run along a vertical wall. The player keeps the speed they arrived with, sinks slowly and the camera
/// leans away from the wall. Jump to kick off the wall (<see cref="WallJumpState"/>). The state ends when the
/// wall ends, the timer runs out, forward input is released or the ground is reached.
/// </summary>
public class WallRunState : ParkourState
{
    public override string StateName => "WALL RUN";

    private RaycastHit _wallHit;
    private readonly bool _isLeft;
    private float _timer;
    private float _verticalSpeed;
    private float _noInputTime;
    private Vector3 _wallForward;

    public RaycastHit WallHit => _wallHit;
    public bool IsLeft => _isLeft;
    public Vector3 WallForward => _wallForward;

    public WallRunState(ParkourStateMachine stateMachine, CharacterController controller, RaycastHit wallHit, bool isLeft)
        : base(stateMachine, controller)
    {
        _wallHit = wallHit;
        _isLeft = isLeft;
    }

    public override void Enter()
    {
        var s = StateMachine.Settings;
        _timer = s != null ? s.wallRunDuration : 1.5f;

        var motor = StateMachine.Motor;
        if (motor != null)
        {
            // start with the speed we came in with, and a little of the upward motion so the entry flows
            _verticalSpeed = Mathf.Clamp(motor.Velocity.y, 0f, 3f);
            motor.EnableGravity = false;
            motor.EnableMovement = false;
            motor.SetVerticalVelocity(0f);
        }

        UpdateWallForward();
        float tilt = s != null ? s.wallRunCameraTilt : 15f;
        StateMachine.Look?.SetCameraTilt(_isLeft ? -tilt : tilt, 9f);
    }

    private void UpdateWallForward()
    {
        Vector3 f = Vector3.Cross(_wallHit.normal, Vector3.up);
        if (Vector3.Dot(f, PlayerTransform.forward) < 0f) f = -f;
        _wallForward = f.normalized;
    }

    public override void Update()
    {
        float dt = Time.deltaTime;
        _timer -= dt;
        var s = StateMachine.Settings;
        var motor = StateMachine.Motor;

        UpdateWallForward();

        float speed = s != null ? s.wallRunSpeed : 8f;
        if (motor != null) speed = Mathf.Max(speed, motor.CurrentSpeed * 0.95f);
        float sink = s != null ? -Mathf.Abs(s.wallRunGravity) : -2f;

        // upward carry fades quickly, then the player settles into a slow slide down the wall
        _verticalSpeed = Mathf.MoveTowards(_verticalSpeed, sink, 9f * dt);

        // a gentle push into the wall keeps the capsule touching it on bends
        Vector3 motion = _wallForward * speed + Vector3.up * _verticalSpeed - _wallHit.normal * 1.5f;
        Controller.Move(motion * dt);
        if (motor != null) motor.SetHorizontalVelocity(_wallForward * speed);

        // Is the wall still there?
        bool hasWall = false;
        if (StateMachine.Detector != null)
            hasWall = _isLeft ? StateMachine.Detector.CheckWallLeft(out _wallHit) : StateMachine.Detector.CheckWallRight(out _wallHit);

        var input = InputManager.Instance;
        bool pushing = input == null || input.MoveInput.y > 0.1f;
        _noInputTime = pushing ? 0f : _noInputTime + dt;

        if (!hasWall || _timer <= 0f || _noInputTime > 0.2f)
        {
            StateMachine.BlockWall(_wallHit.collider, 0.5f);
            StateMachine.ChangeState(new AirborneState(StateMachine, Controller));
            return;
        }

        if (motor != null && motor.IsGrounded && _verticalSpeed <= 0f)
        {
            StateMachine.ChangeState(new RunningState(StateMachine, Controller));
        }
    }

    public override void Exit()
    {
        var motor = StateMachine.Motor;
        if (motor != null)
        {
            motor.EnableGravity = true;
            motor.EnableMovement = true;
            motor.SetVerticalVelocity(Mathf.Min(_verticalSpeed, 0f));
        }

        StateMachine.Look?.SetCameraTilt(0f, 8f);
    }
}

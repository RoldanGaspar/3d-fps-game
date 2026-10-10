using UnityEngine;

/// <summary>
/// Procedural vault. The capsule runs along a path over the obstacle (height and depth measured from the
/// real collider), the vault clip is scrubbed in sync and the hands are planted on the obstacle top.
/// </summary>
public class VaultState : ParkourState
{
    public override string StateName => "VAULT";

    private readonly Collider _obstacle;
    private readonly Vector3 _forward;
    private readonly Vector3 _right;
    private readonly Vector3 _startPos;
    private readonly Vector3 _landPos;
    private readonly float _footOffset;
    private readonly float _startRootY;
    private readonly float _landRootY;
    private readonly float _topY;
    private readonly float _totalDistance;
    private readonly float _frontDistance;
    private readonly float _farDistance;
    private readonly Vector3 _handBase;
    private readonly float _duration;
    private readonly float _entrySpeed;

    private float _elapsed;

    public VaultState(ParkourStateMachine stateMachine, CharacterController controller, RaycastHit frontHit, RaycastHit topHit)
        : base(stateMachine, controller)
    {
        _obstacle = frontHit.collider;
        _startPos = controller.transform.position;

        Vector3 fwd = Vector3.ProjectOnPlane(-frontHit.normal, Vector3.up);
        if (fwd.sqrMagnitude < 0.01f) fwd = controller.transform.forward;
        _forward = fwd.normalized;
        _right = Vector3.Cross(Vector3.up, _forward);

        _footOffset = controller.height * 0.5f - controller.center.y + 0.05f;
        _startRootY = _startPos.y - _footOffset;
        _topY = topHit.point.y;

        // Depth of the obstacle along the vault direction (front face to far face).
        float depth = 0.7f;
        if (_obstacle != null)
        {
            var ray = new Ray(new Vector3(frontHit.point.x, _topY - 0.05f, frontHit.point.z) + _forward * 8f, -_forward);
            if (_obstacle.Raycast(ray, out RaycastHit far, 8f))
                depth = Mathf.Clamp(Vector3.Distance(far.point, new Vector3(frontHit.point.x, far.point.y, frontHit.point.z)), 0.2f, 6f);
        }

        Vector3 startXZ = new Vector3(_startPos.x, 0f, _startPos.z);
        Vector3 frontXZ = new Vector3(frontHit.point.x, 0f, frontHit.point.z);
        _frontDistance = Vector3.Dot(frontXZ - startXZ, _forward);
        _farDistance = _frontDistance + depth;

        Vector3 landXZ = frontXZ + _forward * (depth + 0.9f);
        _landRootY = _startRootY;
        if (Physics.Raycast(new Vector3(landXZ.x, _topY + 3f, landXZ.z), Vector3.down, out RaycastHit ground, _topY + 8f, ~0, QueryTriggerInteraction.Ignore)
            && ground.collider != _obstacle)
            _landRootY = ground.point.y;

        _landPos = new Vector3(landXZ.x, _landRootY + _footOffset, landXZ.z);
        _totalDistance = Mathf.Max(0.5f, Vector3.Dot(landXZ - startXZ, _forward));

        // The hands go on top of the obstacle, a little behind its front edge.
        float handOffset = Mathf.Min(0.3f, depth * 0.5f);
        _handBase = new Vector3(frontHit.point.x, _topY, frontHit.point.z) + _forward * handOffset;

        // Faster entry speed means a quicker vault, so the body never slows to a crawl over the obstacle.
        _entrySpeed = StateMachine.Motor != null ? StateMachine.Motor.CurrentSpeed : 5f;
        _duration = Mathf.Clamp(_totalDistance / Mathf.Max(5f, _entrySpeed * 0.9f) * 1.25f, 0.5f, 0.8f);
    }

    public override void Enter()
    {
        _elapsed = 0f;

        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.EnableGravity = false;
            StateMachine.Motor.EnableMovement = false;
        }

        if (_obstacle != null) Physics.IgnoreCollision(Controller, _obstacle, true);
        StateMachine.Animation?.BeginParkourMove(ParkourClipData.VaultStart);
    }

    public override void Update()
    {
        _elapsed += Time.deltaTime;
        float u = Mathf.Clamp01(_elapsed / _duration);
        float clipTime = Mathf.Lerp(ParkourClipData.VaultStart, ParkourClipData.VaultEnd, u);

        // Distance travelled follows the clip so the legs and the body stay in sync.
        float s = _totalDistance * ParkourClipData.VaultForwardProgress(clipTime);

        Vector3 along = _startPos + _forward * s;
        along.y = 0f;

        float baseRoot = Mathf.Lerp(_startRootY, _landRootY, Mathf.SmoothStep(0f, 1f, s / _totalDistance));
        float over = ParkourClipData.Bump(s, _frontDistance - 0.9f, _frontDistance - 0.35f, _farDistance - 0.1f, _farDistance + 0.55f);
        float rootY = Mathf.Max(baseRoot, Mathf.Lerp(baseRoot, _topY + 0.12f, over));

        Vector3 target = new Vector3(along.x, rootY + _footOffset, along.z);
        Controller.Move(target - PlayerTransform.position);

        var anim = StateMachine.Animation;
        if (anim != null)
        {
            anim.SetParkourClipTime(clipTime);
            float hands = ParkourClipData.Bump(u, 0.08f, 0.22f, 0.5f, 0.7f);
            anim.SetParkourHands(_handBase - _right * 0.2f, _handBase + _right * 0.2f, hands * 0.85f);
        }

        // The view dips while the body folds over the obstacle instead of rising like a lift.
        StateMachine.Player?.SetCameraDrop(0.55f * over);

        if (u >= 1f)
        {
            if (StateMachine.Motor != null && StateMachine.Motor.IsSprinting)
                StateMachine.ChangeState(new SprintingState(StateMachine, Controller));
            else
                StateMachine.ChangeState(new RunningState(StateMachine, Controller));
        }
    }

    public override void Exit()
    {
        if (_obstacle != null) Physics.IgnoreCollision(Controller, _obstacle, false);
        StateMachine.Animation?.EndParkourMove();
        StateMachine.Player?.SetCameraDrop(0f);

        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.EnableGravity = true;
            StateMachine.Motor.EnableMovement = true;
            // keep running through the vault
            StateMachine.Motor.SetHorizontalVelocity(_forward * Mathf.Max(StateMachine.Motor.WalkSpeed, _entrySpeed * 0.85f));
        }
    }
}

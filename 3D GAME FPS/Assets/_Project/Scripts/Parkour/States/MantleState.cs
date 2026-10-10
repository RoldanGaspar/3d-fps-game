using UnityEngine;

/// <summary>
/// Procedural ledge climb. The capsule follows a path computed from the real ledge (hang below the
/// edge, pull up, step onto the top) while the mantle clip is scrubbed in sync and both hands are
/// pinned to the ledge edge with IK. Works for any ledge height the detector accepts.
/// </summary>
public class MantleState : ParkourState
{
    public override string StateName => "MANTLE";

    private readonly Collider _ledge;
    private readonly Vector3 _forward;
    private readonly Vector3 _right;
    private readonly Vector3 _edgeTop;      // point on the ledge's front edge at top height
    private readonly Vector3 _startPos;
    private readonly Vector3 _hangPos;
    private readonly Vector3 _endPos;
    private readonly float _clipStart;
    private readonly float _approachTime;
    private readonly float _climbTime;

    private const float ClipEnd = 0.96f;
    private float _elapsed;

    public MantleState(ParkourStateMachine stateMachine, CharacterController controller, RaycastHit frontHit, RaycastHit topHit)
        : base(stateMachine, controller)
    {
        _ledge = frontHit.collider;
        _startPos = controller.transform.position;

        Vector3 fwd = Vector3.ProjectOnPlane(-frontHit.normal, Vector3.up);
        if (fwd.sqrMagnitude < 0.01f) fwd = controller.transform.forward;
        _forward = fwd.normalized;
        _right = Vector3.Cross(Vector3.up, _forward);

        float footOffset = controller.height * 0.5f - controller.center.y + 0.05f;
        float topY = topHit.point.y;
        float startRootY = _startPos.y - footOffset;

        _edgeTop = new Vector3(frontHit.point.x, topY, frontHit.point.z);

        // Where in the clip the hands are at the right height for this ledge.
        float handsNeeded = Mathf.Max(0.8f, topY - startRootY);
        _clipStart = ParkourClipData.MantleStartForHandHeight(handsNeeded);
        float handsUp = ParkourClipData.MantleHandsHeight(_clipStart);

        float hangRootY = Mathf.Max(startRootY, topY - handsUp);
        Vector3 hangXZ = new Vector3(frontHit.point.x, 0f, frontHit.point.z) - _forward * 0.42f;
        _hangPos = new Vector3(hangXZ.x, hangRootY + footOffset, hangXZ.z);

        Vector3 endXZ = new Vector3(topHit.point.x, 0f, topHit.point.z) + _forward * 0.45f;
        _endPos = new Vector3(endXZ.x, topY + footOffset + 0.02f, endXZ.z);

        _approachTime = Mathf.Clamp(Vector3.Distance(_startPos, _hangPos) / 6f, 0.12f, 0.35f);
        float settingsDuration = StateMachine.Settings != null ? StateMachine.Settings.mantleDuration : 0.6f;
        _climbTime = Mathf.Max(0.45f, (ClipEnd - _clipStart) * ParkourClipData.MantleClipLength * 0.9f + (settingsDuration - 0.6f));
    }

    public override void Enter()
    {
        _elapsed = 0f;

        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.EnableGravity = false;
            StateMachine.Motor.EnableMovement = false;
            StateMachine.Motor.SetVerticalVelocity(0f);
        }

        if (_ledge != null) Physics.IgnoreCollision(Controller, _ledge, true);
        StateMachine.Animation?.BeginParkourMove(_clipStart);
    }

    public override void Update()
    {
        _elapsed += Time.deltaTime;

        Vector3 target;
        float clipTime;
        float handWeight;

        if (_elapsed < _approachTime)
        {
            // Jump / step up to the hanging position
            float a = Mathf.SmoothStep(0f, 1f, _elapsed / _approachTime);
            target = Vector3.Lerp(_startPos, _hangPos, a);
            clipTime = _clipStart;
            handWeight = a;
        }
        else
        {
            float u = Mathf.Clamp01((_elapsed - _approachTime) / _climbTime);
            clipTime = Mathf.Lerp(_clipStart, ClipEnd, u);

            float rise = ParkourClipData.MantleRiseProgress(clipTime, _clipStart, ClipEnd);
            float forward = ParkourClipData.MantleForwardProgress(clipTime, _clipStart, ClipEnd);
            target = new Vector3(
                Mathf.Lerp(_hangPos.x, _endPos.x, forward),
                Mathf.Lerp(_hangPos.y, _endPos.y, rise),
                Mathf.Lerp(_hangPos.z, _endPos.z, forward));

            // Hands hold the ledge until the body is over it, then let go.
            handWeight = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.82f, u));
        }

        Controller.Move(target - PlayerTransform.position);

        var anim = StateMachine.Animation;
        if (anim != null)
        {
            anim.SetParkourClipTime(clipTime);
            Vector3 handsAt = _edgeTop + _forward * 0.04f;
            anim.SetParkourHands(handsAt - _right * 0.22f, handsAt + _right * 0.22f, handWeight);
        }

        if (_elapsed >= _approachTime + _climbTime)
        {
            StateMachine.ChangeState(new RunningState(StateMachine, Controller));
        }
    }

    public override void Exit()
    {
        if (_ledge != null) Physics.IgnoreCollision(Controller, _ledge, false);
        StateMachine.Animation?.EndParkourMove();

        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.EnableGravity = true;
            StateMachine.Motor.EnableMovement = true;
            StateMachine.Motor.SetHorizontalVelocity(Vector3.zero);   // arrive standing on the ledge, not still running
        }
    }
}

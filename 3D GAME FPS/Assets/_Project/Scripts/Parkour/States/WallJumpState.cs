using UnityEngine;

/// <summary>
/// Kick off a wall: away from the wall, up, and a share of the speed along the wall.
/// Gives back an air jump so wall runs can chain into double jumps.
/// </summary>
public class WallJumpState : ParkourState
{
    public override string StateName => "WALL JUMP";

    private readonly Vector3 _wallNormal;

    public WallJumpState(ParkourStateMachine stateMachine, CharacterController controller, Vector3 wallNormal)
        : base(stateMachine, controller)
    {
        _wallNormal = wallNormal;
    }

    public override void Enter()
    {
        var s = StateMachine.Settings;
        float up = s != null ? s.wallJumpForce : 7.5f;
        float side = s != null ? s.wallJumpSideForce : 5.5f;

        var motor = StateMachine.Motor;
        if (motor != null)
        {
            Vector3 away = Vector3.ProjectOnPlane(_wallNormal, Vector3.up).normalized;
            Vector3 along = Vector3.ProjectOnPlane(motor.HorizontalVelocity, away);   // speed that runs parallel to the wall
            motor.EnableMovement = true;
            motor.EnableGravity = true;
            motor.SetVelocity(away * side + along * 0.6f + Vector3.up * up);
            motor.AirJumpsRemaining = Mathf.Max(motor.AirJumpsRemaining, 1);
            motor.SetJumpHeld(true);
        }

        StateMachine.Look?.SetCameraTilt(0f, 12f);
        StateMachine.Look?.KickRoll(Vector3.Dot(_wallNormal, PlayerTransform.right) > 0f ? -3f : 3f);

        StateMachine.ChangeState(new AirborneState(StateMachine, Controller));
    }
}

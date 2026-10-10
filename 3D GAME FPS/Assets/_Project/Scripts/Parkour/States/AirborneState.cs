using UnityEngine;

public class AirborneState : ParkourState
{
    public override string StateName => "AIRBORNE";

    private const float MinWallRunSpeed = 3.5f;
    private const float MinHeightForWallRun = 0.6f;

    public AirborneState(ParkourStateMachine stateMachine, CharacterController controller)
        : base(stateMachine, controller) { }

    public override void Update()
    {
        var motor = StateMachine.Motor;

        // 1. Landing transition
        if (motor != null && motor.IsGrounded)
        {
            if (motor.IsSprinting) StateMachine.ChangeState(new SprintingState(StateMachine, Controller));
            else StateMachine.ChangeState(new RunningState(StateMachine, Controller));
            return;
        }

        var detector = StateMachine.Detector;
        if (detector == null) return;

        // 2. Wall run: needs speed, forward input, some height and a wall that runs roughly the way we are facing
        if (CanStartWallRun(motor))
        {
            if (detector.CheckWallLeft(out var leftHit) && IsRunnable(leftHit))
            {
                StateMachine.ChangeState(new WallRunState(StateMachine, Controller, leftHit, true));
                return;
            }
            if (detector.CheckWallRight(out var rightHit) && IsRunnable(rightHit))
            {
                StateMachine.ChangeState(new WallRunState(StateMachine, Controller, rightHit, false));
                return;
            }
        }

        // 3. Grab a ledge or vault an obstacle in the air, only when the player is reaching for it (rising or holding jump)
        if (StateMachine.WantsToGrab() && detector.CheckObstacle(out var frontHit, out var topHit, out _, out bool isVault, out bool isMantle))
        {
            if (isVault)
            {
                StateMachine.ChangeState(new VaultState(StateMachine, Controller, frontHit, topHit));
                return;
            }
            if (isMantle)
            {
                StateMachine.ChangeState(new MantleState(StateMachine, Controller, frontHit, topHit));
            }
        }
    }

    private bool CanStartWallRun(PlayerMotor motor)
    {
        if (motor == null) return false;
        var input = InputManager.Instance;
        if (input != null && input.MoveInput.y < 0.1f) return false;
        if (motor.CurrentSpeed < MinWallRunSpeed) return false;
        return motor.DistanceToGround(MinHeightForWallRun + 0.1f) > MinHeightForWallRun;
    }

    private bool IsRunnable(RaycastHit hit)
    {
        if (StateMachine.IsWallBlocked(hit.collider)) return false;
        if (Mathf.Abs(hit.normal.y) > 0.3f) return false;   // floors and ramps are not walls

        Vector3 along = Vector3.Cross(hit.normal, Vector3.up);
        if (Vector3.Dot(along, PlayerTransform.forward) < 0f) along = -along;
        return Vector3.Dot(along.normalized, PlayerTransform.forward) > 0.35f;
    }
}

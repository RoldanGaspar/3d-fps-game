using UnityEngine;

public class SprintingState : ParkourState
{
    public override string StateName => "SPRINTING";

    public SprintingState(ParkourStateMachine stateMachine, CharacterController controller)
        : base(stateMachine, controller) { }

    public override void Update()
    {
        if (StateMachine.Motor != null && !StateMachine.Motor.IsGrounded)
        {
            StateMachine.ChangeState(new AirborneState(StateMachine, Controller));
            return;
        }

        if (StateMachine.Motor != null && !StateMachine.Motor.IsSprinting)
        {
            StateMachine.ChangeState(new RunningState(StateMachine, Controller));
            return;
        }

        // Running into a low obstacle at a sprint vaults it on its own. Everything else (walking into it,
        // or a ledge to climb) needs the jump button, which is handled in ParkourStateMachine.TryJump.
        if (StateMachine.Detector != null && StateMachine.Motor != null && StateMachine.Motor.MoveDirection.sqrMagnitude > 0.1f)
        {
            if (StateMachine.Detector.CheckObstacle(out var frontHit, out var topHit, out _, out bool isVault, out _) && isVault)
            {
                StateMachine.ChangeState(new VaultState(StateMachine, Controller, frontHit, topHit));
            }
        }
    }
}
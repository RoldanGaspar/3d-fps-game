using UnityEngine;

public class RunningState : ParkourState
{
    public override string StateName => "RUNNING";

    public RunningState(ParkourStateMachine stateMachine, CharacterController controller)
        : base(stateMachine, controller) { }

    public override void Update()
    {
        if (StateMachine.Motor != null && !StateMachine.Motor.IsGrounded)
        {
            StateMachine.ChangeState(new AirborneState(StateMachine, Controller));
            return;
        }

        if (StateMachine.Motor != null && StateMachine.Motor.IsSprinting)
        {
            StateMachine.ChangeState(new SprintingState(StateMachine, Controller));
            return;
        }

        // Walking into an obstacle does nothing; press jump to vault or climb it (see ParkourStateMachine.TryJump).
    }
}
using UnityEngine;

public class CrouchState : ParkourState
{
    public override string StateName => "CROUCH";

    public CrouchState(ParkourStateMachine stateMachine, CharacterController controller)
        : base(stateMachine, controller) { }

    public override void Enter()
    {
        float crouchHeight = StateMachine.Settings != null ? StateMachine.Settings.crouchHeight : 1.0f;
        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.SetColliderHeight(crouchHeight, -0.5f);
        }
    }

    public override void Exit()
    {
        float standHeight = StateMachine.Settings != null ? StateMachine.Settings.standHeight : 2.0f;
        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.SetColliderHeight(standHeight, 0f);
        }
    }
}
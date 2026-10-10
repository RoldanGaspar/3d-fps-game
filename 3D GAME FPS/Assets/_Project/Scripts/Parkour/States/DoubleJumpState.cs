using UnityEngine;

public class DoubleJumpState : ParkourState
{
    public override string StateName => "DOUBLE JUMP";

    public DoubleJumpState(ParkourStateMachine stateMachine, CharacterController controller)
        : base(stateMachine, controller) { }

    public override void Enter()
    {
        // never weaker than a short hop, whatever the settings asset says
        float force = Mathf.Max(6.2f, StateMachine.Settings != null ? StateMachine.Settings.doubleJumpForce : 7.0f);
        if (StateMachine.Motor != null)
        {
            StateMachine.Motor.SetVerticalVelocity(force);
            StateMachine.Motor.SetJumpHeld(true);
        }

        StateMachine.RaiseDoubleJump();
        StateMachine.ChangeState(new AirborneState(StateMachine, Controller));
    }
}
using UnityEngine;

/// <summary>
/// Abstract base class for all parkour states.
/// </summary>
public abstract class ParkourState
{
    protected ParkourStateMachine StateMachine;
    protected CharacterController Controller;
    protected Transform PlayerTransform;

    public ParkourState(ParkourStateMachine stateMachine, CharacterController controller)
    {
        StateMachine = stateMachine;
        Controller = controller;
        PlayerTransform = controller.transform;
    }

    public virtual string StateName => "IDLE";
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
}
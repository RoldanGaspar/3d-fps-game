using UnityEngine;

/// <summary>
/// Enemy dead behaviour: stop moving, play the death animation, then clean up.
/// </summary>
public class EnemyDeadState : EnemyState
{
    private const float RemoveDelay = 6f;
    private float _removeAt;

    public EnemyDeadState(EnemyAI ai) : base(ai) { }

    public override void Enter()
    {
        ai.DisableForDeath();
        _removeAt = Time.time + RemoveDelay;
    }

    public override void Tick(float deltaTime)
    {
        if (Time.time >= _removeAt)
            Object.Destroy(ai.gameObject);
    }
}

using UnityEngine;

/// <summary>
/// Enemy chase behaviour: run at the player until they are in firing range with a clear line of sight.
/// </summary>
public class EnemyChaseState : EnemyState
{
    private float _nextRepath;

    public EnemyChaseState(EnemyAI ai) : base(ai) { }

    public override void Enter()
    {
        ai.Agent.isStopped = false;
        ai.Agent.updateRotation = true;
        ai.Agent.speed = ai.ChaseSpeed;
        ai.SetAiming(false);
        _nextRepath = 0f;
    }

    public override void Tick(float deltaTime)
    {
        if (!ai.HasPlayer)
        {
            ai.GoPatrol();
            return;
        }

        if (ai.CanSeePlayer && ai.DistanceToPlayer <= ai.AttackRange)
        {
            ai.GoAttack();
            return;
        }

        if (ai.TimeSinceSeen > ai.LoseTargetTime)
        {
            ai.GoAlert();
            return;
        }

        if (Time.time >= _nextRepath)
        {
            _nextRepath = Time.time + 0.2f;
            Vector3 dest = ai.CanSeePlayer ? ai.Player.position : ai.LastKnownPlayerPos;
            ai.Agent.SetDestination(dest);
        }
    }
}

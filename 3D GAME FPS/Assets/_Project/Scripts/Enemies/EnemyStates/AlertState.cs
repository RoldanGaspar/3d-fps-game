using UnityEngine;

/// <summary>
/// The enemy lost sight of the player: walk to the last known position, look around,
/// then give up and go back to patrolling.
/// </summary>
public class EnemyAlertState : EnemyState
{
    private float _lookAroundUntil;
    private bool _arrived;

    public EnemyAlertState(EnemyAI ai) : base(ai) { }

    public override void Enter()
    {
        _arrived = false;
        ai.Agent.isStopped = false;
        ai.Agent.updateRotation = true;
        ai.Agent.speed = ai.PatrolSpeed * 1.4f;
        ai.SetAiming(false);
        ai.Agent.SetDestination(ai.LastKnownPlayerPos);
    }

    public override void Tick(float deltaTime)
    {
        if (ai.HasPlayer && ai.CanSeePlayer)
        {
            if (ai.DistanceToPlayer <= ai.AttackRange) ai.GoAttack();
            else ai.GoChase();
            return;
        }

        if (!_arrived)
        {
            if (!ai.Agent.pathPending && ai.Agent.remainingDistance <= ai.Agent.stoppingDistance + 0.6f)
            {
                _arrived = true;
                _lookAroundUntil = Time.time + 3f;
                ai.Agent.isStopped = true;
            }
            return;
        }

        // Scan left and right while waiting
        ai.transform.Rotate(0f, Mathf.Sin(Time.time * 2f) * 90f * deltaTime, 0f);
        if (Time.time >= _lookAroundUntil)
            ai.GoPatrol();
    }
}

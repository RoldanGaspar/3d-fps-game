using UnityEngine;

/// <summary>
/// Enemy patrol behaviour: wander between waypoints (or random points around the spawn) and
/// switch to chasing as soon as the player is spotted.
/// </summary>
public class EnemyPatrolState : EnemyState
{
    private int _pointIndex;
    private float _waitUntil;
    private bool _waiting;

    public EnemyPatrolState(EnemyAI ai) : base(ai) { }

    public override void Enter()
    {
        ai.Agent.isStopped = false;
        ai.Agent.updateRotation = true;
        ai.Agent.speed = ai.PatrolSpeed;
        ai.SetAiming(false);
        PickDestination();
    }

    public override void Tick(float deltaTime)
    {
        if (ai.HasPlayer && ai.CanSeePlayer)
        {
            if (ai.DistanceToPlayer <= ai.AttackRange) ai.GoAttack();
            else ai.GoChase();
            return;
        }

        if (_waiting)
        {
            if (Time.time >= _waitUntil)
            {
                _waiting = false;
                PickDestination();
            }
            return;
        }

        if (!ai.Agent.pathPending && ai.Agent.remainingDistance <= ai.Agent.stoppingDistance + 0.4f)
        {
            _waiting = true;
            _waitUntil = Time.time + Random.Range(1f, 2.5f);
        }
    }

    private void PickDestination()
    {
        var points = ai.PatrolPoints;
        if (points != null && points.Length > 0)
        {
            _pointIndex = (_pointIndex + 1) % points.Length;
            ai.Agent.SetDestination(points[_pointIndex].position);
            return;
        }

        if (ai.TryGetRandomNavPoint(ai.SpawnPosition, ai.PatrolRadius, out var p))
            ai.Agent.SetDestination(p);
    }
}

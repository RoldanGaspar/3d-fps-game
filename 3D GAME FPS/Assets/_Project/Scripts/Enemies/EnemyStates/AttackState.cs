using UnityEngine;

/// <summary>
/// Enemy ranged attack: face the player, strafe to stay awkward to hit, keep a preferred distance
/// and fire in short bursts.
/// </summary>
public class EnemyAttackState : EnemyState
{
    private float _nextStrafeTime;
    private float _strafeSign = 1f;
    private float _nextBurstTime;
    private int _shotsLeft;
    private float _nextShotTime;
    private bool _firstBurst;
    private float _lostSightTimer;

    public EnemyAttackState(EnemyAI ai) : base(ai) { }

    public override void Enter()
    {
        ai.Agent.isStopped = false;
        ai.Agent.updateRotation = false;
        ai.Agent.speed = ai.StrafeSpeed;
        ai.SetAiming(true);

        _strafeSign = Random.value < 0.5f ? -1f : 1f;
        _nextStrafeTime = 0f;
        _shotsLeft = 0;
        _firstBurst = true;
        _lostSightTimer = 0f;
        _nextBurstTime = Time.time + ai.ReactionTime;
    }

    public override void Exit()
    {
        ai.Agent.updateRotation = true;
        ai.SetAiming(false);
    }

    public override void Tick(float deltaTime)
    {
        if (!ai.HasPlayer)
        {
            ai.GoPatrol();
            return;
        }

        float distance = ai.DistanceToPlayer;
        if (ai.CanSeePlayer) _lostSightTimer = 0f; else _lostSightTimer += deltaTime;

        if (_lostSightTimer > 1.2f || distance > ai.AttackRange + 4f)
        {
            ai.GoChase();
            return;
        }

        ai.FaceTowards(ai.Player.position, 9f, deltaTime);
        UpdateMovement(distance);
        UpdateShooting();
    }

    private void UpdateMovement(float distance)
    {
        if (Time.time < _nextStrafeTime && !ai.Agent.pathPending && ai.Agent.remainingDistance > 0.4f) return;

        _nextStrafeTime = Time.time + Random.Range(1.2f, 2.6f);
        if (Random.value < 0.35f) _strafeSign = -_strafeSign;

        Vector3 toPlayer = ai.Player.position - ai.transform.position;
        toPlayer.y = 0f;
        Vector3 forward = toPlayer.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float rangeAdjust = 0f;
        if (distance < ai.MinRange) rangeAdjust = -3f;
        else if (distance > ai.AttackRange * 0.8f) rangeAdjust = 3f;

        Vector3 desired = ai.transform.position + right * (_strafeSign * Random.Range(2.5f, 4.5f)) + forward * rangeAdjust;
        if (UnityEngine.AI.NavMesh.SamplePosition(desired, out var hit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            ai.Agent.SetDestination(hit.position);
    }

    private void UpdateShooting()
    {
        if (!ai.CanSeePlayer) return;

        if (_shotsLeft <= 0)
        {
            if (Time.time < _nextBurstTime) return;
            _shotsLeft = ai.BurstSize;
            _nextShotTime = Time.time;
        }

        if (Time.time < _nextShotTime) return;

        ai.FireAtPlayer(accurate: _firstBurst && _shotsLeft == ai.BurstSize);
        _shotsLeft--;
        _nextShotTime = Time.time + ai.ShotInterval;

        if (_shotsLeft <= 0)
        {
            _firstBurst = false;
            _nextBurstTime = Time.time + Random.Range(ai.BurstCooldown.x, ai.BurstCooldown.y);
        }
    }
}

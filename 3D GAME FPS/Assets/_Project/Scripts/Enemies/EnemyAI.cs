using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Base class for the small state classes that drive <see cref="EnemyAI"/>.
/// </summary>
public abstract class EnemyState
{
    protected readonly EnemyAI ai;

    protected EnemyState(EnemyAI ai)
    {
        this.ai = ai;
    }

    public virtual void Enter() { }
    public virtual void Tick(float deltaTime) { }
    public virtual void Exit() { }
}

/// <summary>
/// Ranged enemy built on NavMeshAgent + a small state machine:
/// Patrol -> (Alert) -> Chase -> Attack -> Dead.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyBase))]
public class EnemyAI : MonoBehaviour, IAnimatorIKHandler
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _eye;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Transform _leftHandGrip;
    [SerializeField] private Collider[] _colliders;

    [Header("Perception")]
    [SerializeField] private float _detectionRange = 28f;
    [SerializeField] private float _fieldOfView = 140f;
    [SerializeField] private float _loseTargetTime = 5f;
    [SerializeField] private float _hearingRange = 8f;
    [SerializeField] private LayerMask _sightBlockMask;

    [Header("Movement")]
    [SerializeField] private float _patrolSpeed = 2.2f;
    [SerializeField] private float _chaseSpeed = 4.8f;
    [SerializeField] private float _strafeSpeed = 2.2f;
    [SerializeField] private float _patrolRadius = 12f;
    [SerializeField] private Transform[] _patrolPoints;

    [Header("Combat")]
    [SerializeField] private float _attackRange = 15f;
    [SerializeField] private float _minRange = 5f;
    [SerializeField] private float _damage = 8f;
    [SerializeField] private int _burstSize = 3;
    [SerializeField] private float _shotInterval = 0.17f;
    [SerializeField] private Vector2 _burstCooldown = new Vector2(0.9f, 1.8f);
    [SerializeField] private float _spreadPerMeter = 0.035f;
    [SerializeField] private float _shotRange = 60f;
    [SerializeField] private float _reactionTime = 0.45f;

    private static readonly int Hash_Speed = Animator.StringToHash("Speed");
    private static readonly int Hash_IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int Hash_IsAirborne = Animator.StringToHash("IsAirborne");
    private static readonly int Hash_WeaponType = Animator.StringToHash("WeaponType");
    private static readonly int Hash_IsAiming = Animator.StringToHash("IsAiming");
    private static readonly int Hash_Fire = Animator.StringToHash("Fire");
    private static readonly int Hash_Hit = Animator.StringToHash("Hit");
    private static readonly int Hash_IsDead = Animator.StringToHash("IsDead");

    private NavMeshAgent _agent;
    private EnemyBase _health;
    private RagdollController _ragdoll;
    private Transform _player;
    private PlayerHealth _playerHealth;
    private CameraModeController _playerCamera;
    private LayerMask _shootMask;

    private EnemyState _state;
    private EnemyPatrolState _patrol;
    private EnemyAlertState _alert;
    private EnemyChaseState _chase;
    private EnemyAttackState _attack;
    private EnemyDeadState _dead;

    private bool _canSeePlayer;
    private float _nextPerceptionTime;
    private float _nextHitReaction;
    private Vector3 _lastKnownPlayerPos;
    private float _timeSinceSeen = 999f;
    private float _leftHandIK;
    private float _stepDistance;

    // --- Accessors for the state classes ---
    public NavMeshAgent Agent => _agent;
    public Transform Player => _player;
    public bool HasPlayer => _player != null && _playerHealth != null && _playerHealth.IsAlive;
    public bool CanSeePlayer => _canSeePlayer;
    public float TimeSinceSeen => _timeSinceSeen;
    public float LoseTargetTime => _loseTargetTime;
    public Vector3 LastKnownPlayerPos => _lastKnownPlayerPos;
    public float PatrolSpeed => _patrolSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float StrafeSpeed => _strafeSpeed;
    public float PatrolRadius => _patrolRadius;
    public Transform[] PatrolPoints => _patrolPoints;
    public float AttackRange => _attackRange;
    public float MinRange => _minRange;
    public int BurstSize => _burstSize;
    public float ShotInterval => _shotInterval;
    public Vector2 BurstCooldown => _burstCooldown;
    public float ReactionTime => _reactionTime * _reactionMultiplier;
    public Vector3 SpawnPosition { get; private set; }
    public EnemyState CurrentState => _state;

    public float DistanceToPlayer => _player != null ? Vector3.Distance(transform.position, _player.position) : float.MaxValue;
    private Vector3 PlayerAimPoint => _player.position + Vector3.up * 0.35f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _health = GetComponent<EnemyBase>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();
        if (_eye == null) _eye = transform;
        // Only the root colliders: bone colliders belong to the ragdoll and must stay usable after death.
        if (_colliders == null || _colliders.Length == 0) _colliders = GetComponents<Collider>();
        _ragdoll = GetComponent<RagdollController>();

        ApplyEnemyData();

        int enemy = LayerMask.NameToLayer("Enemy");
        int projectile = LayerMask.NameToLayer("Projectile");
        _shootMask = ~((1 << enemy) | (1 << projectile));

        if (_sightBlockMask == 0)
            _sightBlockMask = LayerMask.GetMask("Ground", "Wall", "Obstacle");

        _patrol = new EnemyPatrolState(this);
        _alert = new EnemyAlertState(this);
        _chase = new EnemyChaseState(this);
        _attack = new EnemyAttackState(this);
        _dead = new EnemyDeadState(this);

        if (_animator != null)
        {
            _animator.applyRootMotion = false;
            _animator.SetBool(Hash_IsGrounded, true);
            _animator.SetBool(Hash_IsAirborne, false);
            _animator.SetInteger(Hash_WeaponType, 1);
            var relay = _animator.GetComponent<AnimatorIKRelay>();
            if (relay == null) relay = _animator.gameObject.AddComponent<AnimatorIKRelay>();
            relay.SetTarget(this);
        }
    }

    private void ApplyEnemyData()
    {
        var data = _health != null ? _health.Data : null;
        if (data == null) return;

        _detectionRange = data.detectionRange;
        _fieldOfView = data.fieldOfView;
        _loseTargetTime = data.loseTargetTime;
        _attackRange = Mathf.Max(data.attackRange, _minRange + 2f);
        _damage = data.attackDamage;
        _chaseSpeed = data.moveSpeed + 1.3f;
        _patrolSpeed = Mathf.Min(_patrolSpeed, data.moveSpeed);
    }

    private float _damageMultiplier = 1f;
    private float _spreadMultiplier = 1f;
    private float _reactionMultiplier = 1f;

    /// <summary>Level-based tuning: damage, aim error and reaction time multipliers.</summary>
    public void ApplyDifficulty(float damage, float spread, float reaction)
    {
        _damageMultiplier = damage;
        _spreadMultiplier = spread;
        _reactionMultiplier = reaction;
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.Damaged += OnDamaged;
            _health.OnDeath.AddListener(OnDeath);
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.Damaged -= OnDamaged;
            _health.OnDeath.RemoveListener(OnDeath);
        }
    }

    private void Start()
    {
        SpawnPosition = transform.position;

        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<PlayerHealth>();
            _playerCamera = playerObj.GetComponent<CameraModeController>();
        }

        if (!_agent.isOnNavMesh && NavMesh.SamplePosition(transform.position, out var hit, 3f, NavMesh.AllAreas))
            _agent.Warp(hit.position);

        ChangeState(_patrol);
    }

    private void Update()
    {
        if (!_health.IsAlive && _state != _dead) return;

        UpdatePerception();
        _state?.Tick(Time.deltaTime);
        UpdateAnimatorLocomotion();
    }

    // --- State machine ---

    public void GoPatrol() => ChangeState(_patrol);
    public void GoAlert() => ChangeState(_alert);
    public void GoChase() => ChangeState(_chase);
    public void GoAttack() => ChangeState(_attack);

    private void ChangeState(EnemyState next)
    {
        if (next == _state) return;
        _state?.Exit();
        _state = next;
        _state?.Enter();
    }

    // --- Perception ---

    private void UpdatePerception()
    {
        if (!HasPlayer)
        {
            _canSeePlayer = false;
            return;
        }

        if (Time.time >= _nextPerceptionTime)
        {
            _nextPerceptionTime = Time.time + 0.1f;
            _canSeePlayer = EvaluateSight();
        }

        if (_canSeePlayer)
        {
            _lastKnownPlayerPos = _player.position;
            _timeSinceSeen = 0f;
        }
        else
        {
            _timeSinceSeen += Time.deltaTime;
        }
    }

    private bool EvaluateSight()
    {
        Vector3 eye = _eye.position;
        Vector3 target = PlayerAimPoint;
        Vector3 toPlayer = target - eye;
        float distance = toPlayer.magnitude;
        if (distance > _detectionRange) return false;

        // Very close enemies "hear" the player even from behind.
        bool heard = distance <= _hearingRange;
        if (!heard)
        {
            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
            if (angle > _fieldOfView * 0.5f) return false;
        }

        return !Physics.Linecast(eye, target, _sightBlockMask, QueryTriggerInteraction.Ignore);
    }

    private void OnDamaged(float amount)
    {
        if (_state == _dead || !HasPlayer) return;

        _lastKnownPlayerPos = _player.position;
        _timeSinceSeen = 0f;

        if (_animator != null && Time.time >= _nextHitReaction)
        {
            _nextHitReaction = Time.time + 1.2f;
            _animator.SetTrigger(Hash_Hit);
        }

        // Getting shot is enough to know where the player is.
        if (_state == _patrol || _state == _alert)
            GoChase();
    }

    private void OnDeath()
    {
        ChangeState(_dead);
    }

    // --- Movement helpers ---

    public bool TryGetRandomNavPoint(Vector3 around, float radius, out Vector3 point)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 candidate = around + Random.insideUnitSphere * radius;
            candidate.y = around.y;
            if (NavMesh.SamplePosition(candidate, out var hit, 2.5f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }
        point = around;
        return false;
    }

    public void FaceTowards(Vector3 worldPoint, float turnSpeed, float deltaTime)
    {
        Vector3 flat = worldPoint - transform.position;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(flat);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * deltaTime));
    }

    private void UpdateAnimatorLocomotion()
    {
        if (_animator == null) return;
        float speed = _agent.enabled ? _agent.velocity.magnitude : 0f;
        _animator.SetFloat(Hash_Speed, speed, 0.1f, Time.deltaTime);

        // Footsteps: one step per stride walked, audible in 3D.
        if (speed > 0.5f)
        {
            _stepDistance += speed * Time.deltaTime;
            if (_stepDistance >= 1.9f)
            {
                _stepDistance = 0f;
                VfxPool.Instance.PlaySfxAt(ProceduralSfx.Get(SfxId.Footstep), transform.position, 0.45f, Random.Range(0.8f, 0.95f));
            }
        }
    }

    public void SetAiming(bool aiming)
    {
        if (_animator != null) _animator.SetBool(Hash_IsAiming, aiming);
    }

    // --- Shooting ---

    public void FireAtPlayer(bool accurate)
    {
        if (!HasPlayer) return;

        Vector3 origin = _muzzle != null ? _muzzle.position : _eye.position;
        Vector3 target = PlayerAimPoint;
        float distance = Vector3.Distance(origin, target);
        float spread = _spreadPerMeter * _spreadMultiplier * distance * (accurate ? 0.4f : 1f);
        target += Random.insideUnitSphere * spread;

        Vector3 dir = (target - origin).normalized;
        Vector3 end = origin + dir * _shotRange;
        var pool = VfxPool.Instance;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, _shotRange, _shootMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;
            var victim = hit.collider.GetComponentInParent<PlayerHealth>();
            if (victim != null)
            {
                if (victim.IsAlive)
                {
                    victim.TakeDamage(_damage * _damageMultiplier);
                    if (_playerCamera != null) _playerCamera.GenerateImpulse(0.35f);
                }
            }
            else
            {
                pool.SpawnImpact(hit.point, hit.normal, SurfaceMarker.Resolve(hit.collider), true);
            }
        }

        pool.SpawnTracer(origin, end, new Color(1f, 0.45f, 0.2f, 0.95f));
        pool.SpawnMuzzleFlash(origin, dir, 0.8f);
        pool.PlaySfxAt(ProceduralSfx.Get(SfxId.EnemyShot), origin, 0.9f, Random.Range(0.93f, 1.07f));

        if (_animator != null) _animator.SetTrigger(Hash_Fire);
    }

    // --- Death ---

    public void DisableForDeath()
    {
        if (_agent.enabled)
        {
            _agent.isStopped = true;
            _agent.enabled = false;
        }
        foreach (var c in _colliders)
            if (c != null) c.enabled = false;

        if (_animator != null)
            _animator.SetBool(Hash_IsAiming, false);

        // Prefer a physics ragdoll; fall back to the death clip when too many ragdolls are active.
        if (_ragdoll != null && _ragdoll.IsBuilt && RagdollController.HasCapacity)
        {
            Vector3 point = _health.LastHitImpulse > 0f ? _health.LastHitPoint : transform.position + Vector3.up;
            Vector3 dir = _health.LastHitImpulse > 0f ? _health.LastHitDirection : -transform.forward;
            float impulse = _health.LastHitImpulse > 0f ? _health.LastHitImpulse : 20f;

            Collider[] ignore = _player != null ? _player.GetComponentsInChildren<Collider>() : null;
            _ragdoll.Activate(point, dir.normalized * impulse, ignore);
            return;
        }

        if (_animator != null)
            _animator.SetBool(Hash_IsDead, true);
    }

    // --- IK (left hand on the foregrip) ---

    public void HandleAnimatorIK(int layerIndex)
    {
        if (_animator == null || layerIndex != 0) return;

        bool use = _leftHandGrip != null && _state != _dead;
        _leftHandIK = Mathf.MoveTowards(_leftHandIK, use ? 1f : 0f, Time.deltaTime * 6f);
        if (_leftHandIK <= 0.001f)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            return;
        }

        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _leftHandIK);
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandGrip.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
    }
}

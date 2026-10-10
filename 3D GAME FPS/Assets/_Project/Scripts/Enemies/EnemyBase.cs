using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Optional: lets a target know where and how hard it was hit (used for ragdoll impulses).
/// </summary>
public interface IHitReactive
{
    void OnHit(Vector3 point, Vector3 direction, float impulse);
}

/// <summary>
/// Base enemy class with health and death handling.
/// </summary>
public class EnemyBase : MonoBehaviour, IDamageable, IHitReactive
{
    [Header("Stats")]
    [SerializeField] protected EnemyData _data;
    [SerializeField] protected float _currentHealth;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    /// <summary>Raised after damage is applied. Argument is the damage amount.</summary>
    public event Action<float> Damaged;

    /// <summary>Raised for every enemy that dies (used for run rewards).</summary>
    public static event Action<EnemyBase> AnyEnemyDied;

    private float _healthMultiplier = 1f;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => ((_data != null) ? _data.maxHealth : 100f) * _healthMultiplier;

    /// <summary>Scales this enemy's health for the current level and refills it.</summary>
    public void ApplyHealthMultiplier(float multiplier)
    {
        _healthMultiplier = Mathf.Max(0.1f, multiplier);
        _currentHealth = MaxHealth;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AnyEnemyDied = null;
    }
    public bool IsAlive => _currentHealth > 0f;
    public EnemyData Data => _data;

    public Vector3 LastHitPoint { get; private set; }
    public Vector3 LastHitDirection { get; private set; } = Vector3.forward;
    public float LastHitImpulse { get; private set; }

    public void OnHit(Vector3 point, Vector3 direction, float impulse)
    {
        LastHitPoint = point;
        LastHitDirection = direction;
        LastHitImpulse = impulse;
    }

    protected virtual void Awake()
    {
        _currentHealth = MaxHealth;
    }

    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth / MaxHealth);
        Damaged?.Invoke(amount);
        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        OnDeath?.Invoke();
        AnyEnemyDied?.Invoke(this);
    }
}

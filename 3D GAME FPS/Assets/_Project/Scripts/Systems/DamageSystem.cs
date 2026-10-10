using UnityEngine;

/// <summary>
/// IDamageable interface for anything that can take damage.
/// </summary>
public interface IDamageable
{
    void TakeDamage(float amount);
    bool IsAlive { get; }
}

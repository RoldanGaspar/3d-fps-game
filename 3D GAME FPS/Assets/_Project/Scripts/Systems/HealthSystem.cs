using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Shared health component usable on any GameObject.
/// </summary>
public class HealthSystem : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 100f;
    private float _currentHealth;

    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDied;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public bool IsAlive => _currentHealth > 0f;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    public void TakeDamage(float amount)
    {
        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
        if (!IsAlive) OnDied?.Invoke();
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
    }
}

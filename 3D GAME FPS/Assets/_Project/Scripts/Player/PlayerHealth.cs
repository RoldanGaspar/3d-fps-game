using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Player health, armor, damage, regeneration and death handling.
/// Armor soaks a share of every hit until it runs out.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _currentHealth = 100f;

    [Header("Armor")]
    [SerializeField] private float _maxArmor;
    [SerializeField] private float _armor;
    [SerializeField] private float _armorAbsorb = 0.6f;

    [Header("Regeneration")]
    [SerializeField] private float _regenPerSecond;
    [SerializeField] private float _regenDelay = 4f;

    public UnityEvent<float> OnHealthChanged = new UnityEvent<float>();
    public UnityEvent<float> OnArmorChanged = new UnityEvent<float>();
    public UnityEvent OnDamaged = new UnityEvent();
    public UnityEvent OnDeath = new UnityEvent();

    private bool _dead;
    private float _invulnerableUntil;
    private float _lastDamageTime = -999f;

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => _maxHealth;
    public float Armor => _armor;
    public float MaxArmor => _maxArmor;
    public bool IsAlive => _currentHealth > 0f;
    public bool Invulnerable => Time.time < _invulnerableUntil;
    public float RegenPerSecond { get => _regenPerSecond; set => _regenPerSecond = Mathf.Max(0f, value); }

    private void Awake()
    {
        _currentHealth = _maxHealth;
        _armor = _maxArmor;
    }

    private void Update()
    {
        if (_dead || _regenPerSecond <= 0f || _currentHealth >= _maxHealth) return;
        if (Time.time - _lastDamageTime < _regenDelay) return;
        Heal(_regenPerSecond * Time.deltaTime);
    }

    public void TakeDamage(float amount)
    {
        if (_dead || Invulnerable) return;

        _lastDamageTime = Time.time;

        if (_armor > 0f)
        {
            float absorbed = Mathf.Min(_armor, amount * _armorAbsorb);
            _armor -= absorbed;
            amount -= absorbed;
            OnArmorChanged?.Invoke(_maxArmor > 0f ? _armor / _maxArmor : 0f);
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
        OnDamaged?.Invoke();
        if (_currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (_dead) return;
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
    }

    public void AddArmor(float amount)
    {
        if (_dead || _maxArmor <= 0f) return;
        _armor = Mathf.Min(_maxArmor, _armor + amount);
        OnArmorChanged?.Invoke(_armor / _maxArmor);
    }

    /// <summary>Raises the maximum health (skills) and optionally fills the new space.</summary>
    public void SetMaxHealth(float max, bool refill)
    {
        _maxHealth = Mathf.Max(1f, max);
        _currentHealth = refill ? _maxHealth : Mathf.Min(_currentHealth, _maxHealth);
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
    }

    public void SetMaxArmor(float max, bool refill)
    {
        _maxArmor = Mathf.Max(0f, max);
        _armor = refill ? _maxArmor : Mathf.Min(_armor, _maxArmor);
        OnArmorChanged?.Invoke(_maxArmor > 0f ? _armor / _maxArmor : 0f);
    }

    /// <summary>Brings the player back to life with a fraction of their health and a short grace period.</summary>
    public void Revive(float healthFraction, float invulnerableSeconds)
    {
        _dead = false;
        _currentHealth = Mathf.Max(1f, _maxHealth * Mathf.Clamp01(healthFraction));
        _armor = _maxArmor * 0.5f;
        _invulnerableUntil = Time.time + invulnerableSeconds;
        _lastDamageTime = Time.time;
        OnHealthChanged?.Invoke(_currentHealth / _maxHealth);
        OnArmorChanged?.Invoke(_maxArmor > 0f ? _armor / _maxArmor : 0f);
    }

    private void Die()
    {
        _dead = true;
        OnDeath?.Invoke();
    }
}

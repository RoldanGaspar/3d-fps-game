using UnityEngine;

/// <summary>
/// ScriptableObject definition for enemy stats.
/// Create instances via: Create > Game > Enemy Data
/// </summary>
[CreateAssetMenu(fileName = "New EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyName;
    public EnemyType enemyType;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float moveSpeed = 3.5f;
    public float attackDamage = 10f;
    public float attackRate = 1f;
    public float attackRange = 2f;

    [Header("Detection")]
    public float detectionRange = 15f;
    public float fieldOfView = 120f;
    public float loseTargetTime = 5f;

    [Header("Rewards")]
    public int scoreValue = 100;
}

public enum EnemyType
{
    Grunt,
    Soldier,
    Heavy,
    Sniper,
    Boss
}
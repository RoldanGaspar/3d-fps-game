using UnityEngine;

/// <summary>Everything a level needs to know to spawn its enemies and pay out rewards.</summary>
public struct LevelConfig
{
    public int level;
    public int waves;
    public int firstWaveSize;
    public int enemiesAddedPerWave;
    public int maxAlive;
    public float healthMultiplier;
    public float damageMultiplier;
    /// <summary>Multiplier on enemy aim error. Lower is more accurate.</summary>
    public float spreadMultiplier;
    public float reactionMultiplier;
    public int coinsPerKill;
    public int xpPerKill;
    public int clearBonusCoins;
    public bool hasBoss;
}

/// <summary>
/// Defines how the game gets harder level by level. Every number lives here so it is easy to tune.
/// </summary>
public static class DifficultyCurve
{
    public const int MaxAliveCap = 8;      // keeps mobile frame rates stable

    public static LevelConfig Get(int level)
    {
        level = Mathf.Max(1, level);
        int n = level - 1;

        var cfg = new LevelConfig
        {
            level = level,
            waves = Mathf.Min(3 + level / 4, 6),
            firstWaveSize = Mathf.RoundToInt(2 + level * 0.5f),
            enemiesAddedPerWave = 1 + level / 6,
            maxAlive = Mathf.Min(5 + level / 2, MaxAliveCap),
            healthMultiplier = 1f + 0.08f * n,
            damageMultiplier = Mathf.Min(1f + 0.05f * n, 2f),
            spreadMultiplier = Mathf.Max(0.55f, 1f - 0.03f * n),
            reactionMultiplier = Mathf.Max(0.5f, 1f - 0.04f * n),
            coinsPerKill = 8 + level * 2,
            xpPerKill = 10 + level * 2,
            clearBonusCoins = 50 + level * 25,
            hasBoss = level % 5 == 0
        };
        return cfg;
    }

    /// <summary>Total enemies in a level, handy for UI and balancing.</summary>
    public static int TotalEnemies(LevelConfig cfg)
    {
        int total = 0;
        for (int w = 0; w < cfg.waves; w++)
            total += cfg.firstWaveSize + w * cfg.enemiesAddedPerWave;
        return total;
    }

    /// <summary>XP needed to go from <paramref name="level"/> to the next player level.</summary>
    public static int XpForNextLevel(int level)
    {
        return 100 + (level - 1) * 60;
    }
}

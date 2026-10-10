using System;
using UnityEngine;

/// <summary>
/// State of the current run (one attempt at a level). Lives across scene loads so the menu can
/// start a level and the arena can read what was chosen. Also pays out rewards into the profile.
/// </summary>
public static class GameSession
{
    public static int CurrentLevel { get; private set; } = 1;
    public static LevelConfig Config { get; private set; } = DifficultyCurve.Get(1);

    public static int Kills { get; private set; }
    public static int CoinsEarned { get; private set; }
    public static int XpEarned { get; private set; }
    public static int WaveReached { get; private set; }
    public static float StartTime { get; private set; }
    public static int RevivesUsed { get; private set; }
    public static bool Active { get; private set; }
    public static bool LevelUpHappened { get; private set; }

    public static event Action OnStatsChanged;
    public static event Action<int> OnPlayerLevelUp;

    public const int MaxRevivesPerRun = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        CurrentLevel = 1;
        Config = DifficultyCurve.Get(1);
        Active = false;
        OnStatsChanged = null;
        OnPlayerLevelUp = null;
    }

    public static float Elapsed => Active ? Time.unscaledTime - StartTime : 0f;
    public static int ReviveCost => 100 + (CurrentLevel - 1) * 25;
    public static bool CanRevive => RevivesUsed < MaxRevivesPerRun && SaveSystem.Profile.coins >= ReviveCost;

    public static void Begin(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
        Config = DifficultyCurve.Get(CurrentLevel);
        Kills = 0;
        CoinsEarned = 0;
        XpEarned = 0;
        WaveReached = 0;
        RevivesUsed = 0;
        LevelUpHappened = false;
        RunModifiers.Reset();
        StartTime = Time.unscaledTime;
        Active = true;
        OnStatsChanged?.Invoke();
    }

    /// <summary>Used when the arena scene is opened directly (editor testing): starts level 1.</summary>
    public static void EnsureStarted()
    {
        if (!Active) Begin(CurrentLevel);
    }

    public static void RegisterWave(int wave)
    {
        WaveReached = Mathf.Max(WaveReached, wave);
        OnStatsChanged?.Invoke();
    }

    /// <param name="rewardScale">1 for a normal enemy; tougher enemies and bosses pay more.</param>
    public static void RegisterKill(float rewardScale = 1f)
    {
        Kills++;
        CoinsEarned += Mathf.RoundToInt(Config.coinsPerKill * rewardScale * PlayerStats.CoinMultiplier);
        XpEarned += Mathf.RoundToInt(Config.xpPerKill * rewardScale);
        OnStatsChanged?.Invoke();
    }

    public static bool TryRevive()
    {
        if (!CanRevive) return false;
        var profile = SaveSystem.Profile;
        profile.coins -= ReviveCost;
        RevivesUsed++;
        SaveSystem.Save();
        return true;
    }

    /// <summary>Banks the run's coins and XP into the saved profile. Called once when the run ends.</summary>
    public static void EndRun(bool victory)
    {
        if (!Active) return;
        Active = false;

        var p = SaveSystem.Profile;
        int bonus = victory ? Config.clearBonusCoins : 0;
        CoinsEarned += bonus;

        p.coins += CoinsEarned;
        p.totalKills += Kills;
        p.xp += XpEarned;

        // Level ups grant skill points
        int levelsGained = 0;
        while (p.xp >= DifficultyCurve.XpForNextLevel(p.level))
        {
            p.xp -= DifficultyCurve.XpForNextLevel(p.level);
            p.level++;
            p.skillPoints++;
            levelsGained++;
        }
        LevelUpHappened = levelsGained > 0;

        if (victory)
        {
            p.highestLevelCleared = Mathf.Max(p.highestLevelCleared, CurrentLevel);
            p.highestLevelUnlocked = Mathf.Max(p.highestLevelUnlocked, CurrentLevel + 1);
        }

        SaveSystem.Save();
        if (levelsGained > 0) OnPlayerLevelUp?.Invoke(p.level);
        OnStatsChanged?.Invoke();
    }
}

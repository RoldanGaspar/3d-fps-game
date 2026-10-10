using UnityEngine;

/// <summary>
/// Combines the saved skills with the current run's perks into the numbers gameplay code uses.
/// </summary>
public static class PlayerStats
{
    public const float BaseHealth = 100f;
    public const float ArmorAbsorb = 0.6f;

    private static PlayerProfile P => SaveSystem.Profile;

    public static float MaxHealth => BaseHealth + 20f * P.SkillRank("health") + RunModifiers.ExtraMaxHealth;
    public static float MaxArmor => 25f * P.SkillRank("armor") + RunModifiers.ExtraMaxArmor;
    public static float SpeedMultiplier => (1f + 0.04f * P.SkillRank("speed")) * RunModifiers.SpeedMultiplier;
    public static float RegenPerSecond => 1f * P.SkillRank("regen");
    public static float CoinMultiplier => (1f + 0.1f * P.SkillRank("scavenger")) * RunModifiers.CoinMultiplier;
}

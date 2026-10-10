using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temporary perks the player picks between waves. They only last for the current run
/// (permanent upgrades live in the profile's skills and weapon upgrades).
/// </summary>
public static class RunModifiers
{
    public class PerkDef
    {
        public string id;
        public string name;
        public string description;
    }

    public static float DamageMultiplier { get; private set; } = 1f;
    public static float SpeedMultiplier { get; private set; } = 1f;
    public static float ExtraMaxHealth { get; private set; }
    public static float ExtraMaxArmor { get; private set; }
    public static float CoinMultiplier { get; private set; } = 1f;
    public static List<string> Picked { get; } = new List<string>();

    public static readonly List<PerkDef> All = new List<PerkDef>
    {
        new PerkDef { id = "hp",      name = "VITALITY",      description = "+30 max health and heal 30" },
        new PerkDef { id = "armor",   name = "PLATING",       description = "+30 max armor and restore it" },
        new PerkDef { id = "damage",  name = "HOLLOW POINTS", description = "+12% weapon damage this run" },
        new PerkDef { id = "speed",   name = "ADRENALINE",    description = "+8% movement speed this run" },
        new PerkDef { id = "ammo",    name = "RESUPPLY",      description = "Refill all ammo now" },
        new PerkDef { id = "heal",    name = "FIELD MEDIC",   description = "Heal 60% of your health now" },
        new PerkDef { id = "coins",   name = "LOOTER",        description = "+20% coins from kills this run" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Reset();

    public static void Reset()
    {
        DamageMultiplier = 1f;
        SpeedMultiplier = 1f;
        ExtraMaxHealth = 0f;
        ExtraMaxArmor = 0f;
        CoinMultiplier = 1f;
        Picked.Clear();
    }

    /// <summary>Picks up to <paramref name="count"/> different random perks.</summary>
    public static List<PerkDef> Roll(int count)
    {
        var pool = new List<PerkDef>(All);
        var result = new List<PerkDef>();
        while (result.Count < count && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            result.Add(pool[i]);
            pool.RemoveAt(i);
        }
        return result;
    }

    /// <summary>Applies a perk to the player's current run. Returns false when no player is present.</summary>
    public static bool Apply(PerkDef perk)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;
        var health = player.GetComponent<PlayerHealth>();
        var weapons = player.GetComponentInChildren<WeaponManager>(true);

        Picked.Add(perk.id);
        switch (perk.id)
        {
            case "hp":
                ExtraMaxHealth += 30f;
                health.SetMaxHealth(health.MaxHealth + 30f, false);
                health.Heal(30f);
                break;
            case "armor":
                ExtraMaxArmor += 30f;
                health.SetMaxArmor(health.MaxArmor + 30f, true);
                break;
            case "damage":
                DamageMultiplier += 0.12f;
                if (weapons != null)
                    foreach (var w in weapons.Weapons)
                        if (w != null && w.Data != null) w.Data.damage *= 1.12f / 1f;
                break;
            case "speed":
                SpeedMultiplier += 0.08f;
                var motor = player.GetComponent<PlayerMotor>();
                if (motor != null) motor.SpeedMultiplier = PlayerStats.SpeedMultiplier;
                break;
            case "ammo":
                if (weapons != null)
                    foreach (var w in weapons.Weapons) if (w != null && w.Data != null) w.AddAmmo(w.Data.maxReserveAmmo);
                break;
            case "heal":
                health.Heal(health.MaxHealth * 0.6f);
                break;
            case "coins":
                CoinMultiplier += 0.2f;
                break;
        }
        return true;
    }
}

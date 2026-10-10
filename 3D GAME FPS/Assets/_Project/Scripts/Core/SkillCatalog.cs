using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The permanent skills the player can level up with skill points (earned by levelling up).
/// All numbers live here so they are easy to balance. Add a new entry to add a new skill.
/// </summary>
public static class SkillCatalog
{
    public class SkillDef
    {
        public string id;
        public string name;
        public string description;     // what one rank gives
        public int maxRank;
        public float perRank;          // meaning depends on the skill (see PlayerStats)
        public string unit;            // shown after the value
    }

    public static readonly List<SkillDef> All = new List<SkillDef>
    {
        new SkillDef { id = "health",    name = "EXPAND HEALTH", description = "Maximum health",         maxRank = 10, perRank = 20f, unit = " HP" },
        new SkillDef { id = "armor",     name = "EXPAND ARMOR",  description = "Armor plating (absorbs 60% of damage)", maxRank = 10, perRank = 25f, unit = " armor" },
        new SkillDef { id = "ammo",      name = "EXPAND AMMO",   description = "Extra spare magazines",  maxRank = 10, perRank = 1f,  unit = " mag" },
        new SkillDef { id = "reload",    name = "QUICK HANDS",   description = "Faster reloading",       maxRank = 5,  perRank = 8f,  unit = "%" },
        new SkillDef { id = "damage",    name = "LETHAL",        description = "Weapon damage",          maxRank = 10, perRank = 5f,  unit = "%" },
        new SkillDef { id = "speed",     name = "SPRINTER",      description = "Movement speed",         maxRank = 5,  perRank = 4f,  unit = "%" },
        new SkillDef { id = "regen",     name = "REGENERATION",  description = "Health regained per second out of combat", maxRank = 5, perRank = 1f, unit = " HP/s" },
        new SkillDef { id = "scavenger", name = "SCAVENGER",     description = "Bonus coins from kills", maxRank = 5,  perRank = 10f, unit = "%" },
    };

    public static SkillDef Get(string id)
    {
        foreach (var s in All) if (s.id == id) return s;
        return null;
    }

    /// <summary>Skill points needed for the next rank (1 for the first ranks, then more).</summary>
    public static int CostForNextRank(int currentRank)
    {
        return 1 + currentRank / 4;
    }

    public static bool CanUpgrade(PlayerProfile profile, SkillDef skill)
    {
        int rank = profile.SkillRank(skill.id);
        return rank < skill.maxRank && profile.skillPoints >= CostForNextRank(rank);
    }

    public static bool TryUpgrade(PlayerProfile profile, SkillDef skill)
    {
        if (!CanUpgrade(profile, skill)) return false;
        int rank = profile.SkillRank(skill.id);
        profile.skillPoints -= CostForNextRank(rank);
        profile.SetSkillRank(skill.id, rank + 1);
        SaveSystem.Save();
        return true;
    }

    /// <summary>Refunds all skill points (costs coins) so the player can re-spec.</summary>
    public static int RespecCost(PlayerProfile profile)
    {
        int ranks = 0;
        foreach (var s in profile.skills) ranks += s.rank;
        return ranks * 25;
    }

    public static bool TryRespec(PlayerProfile profile)
    {
        int cost = RespecCost(profile);
        if (cost <= 0 || profile.coins < cost) return false;

        int refunded = 0;
        foreach (var s in All)
        {
            int rank = profile.SkillRank(s.id);
            for (int r = 0; r < rank; r++) refunded += CostForNextRank(r);
            profile.SetSkillRank(s.id, 0);
        }
        profile.skillPoints += refunded;
        profile.coins -= cost;
        SaveSystem.Save();
        return true;
    }
}

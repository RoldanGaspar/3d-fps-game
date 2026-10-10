using System;
using System.Collections.Generic;

/// <summary>
/// Everything that is saved between play sessions: currency, progression, owned items and settings.
/// Plain serializable data (JsonUtility), versioned so older saves can be migrated.
/// </summary>
[Serializable]
public class PlayerProfile
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;

    // Currency & progression
    public int coins = 150;
    public int xp;
    public int level = 1;
    public int skillPoints;

    // Campaign
    public int highestLevelUnlocked = 1;
    public int highestLevelCleared;
    public int totalKills;

    // Inventory
    public List<string> ownedWeapons = new List<string> { "Rifle", "Pistol" };
    public string primaryWeapon = "Rifle";
    public string secondaryWeapon = "Pistol";
    public List<UpgradeEntry> weaponUpgrades = new List<UpgradeEntry>();
    public List<CountEntry> consumables = new List<CountEntry>();

    // Skills (permanent)
    public List<UpgradeEntry> skills = new List<UpgradeEntry>();

    // Settings
    public int targetFps = 60;          // 30 / 60 / 120, 0 = auto
    public int graphicsQuality = 1;     // 0 low, 1 medium, 2 high
    public float sensitivity = 1f;
    public bool showFps;
    public bool batterySaver;

    [Serializable]
    public struct UpgradeEntry
    {
        public string id;
        public int rank;
    }

    [Serializable]
    public struct CountEntry
    {
        public string id;
        public int count;
    }

    // --- helpers ---

    public int GetRank(List<UpgradeEntry> list, string id)
    {
        foreach (var e in list) if (e.id == id) return e.rank;
        return 0;
    }

    public void SetRank(List<UpgradeEntry> list, string id, int rank)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].id != id) continue;
            list[i] = new UpgradeEntry { id = id, rank = rank };
            return;
        }
        list.Add(new UpgradeEntry { id = id, rank = rank });
    }

    public int SkillRank(string id) => GetRank(skills, id);
    public void SetSkillRank(string id, int rank) => SetRank(skills, id, rank);
    public int WeaponUpgradeRank(string key) => GetRank(weaponUpgrades, key);
    public void SetWeaponUpgradeRank(string key, int rank) => SetRank(weaponUpgrades, key, rank);

    public int ConsumableCount(string id)
    {
        foreach (var e in consumables) if (e.id == id) return e.count;
        return 0;
    }

    public void SetConsumableCount(string id, int count)
    {
        for (int i = 0; i < consumables.Count; i++)
        {
            if (consumables[i].id != id) continue;
            consumables[i] = new CountEntry { id = id, count = Math.Max(0, count) };
            return;
        }
        consumables.Add(new CountEntry { id = id, count = Math.Max(0, count) });
    }

    public bool OwnsWeapon(string id) => ownedWeapons.Contains(id);
}

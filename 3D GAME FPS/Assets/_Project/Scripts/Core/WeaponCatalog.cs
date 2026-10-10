using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All weapons in the game, their prices and upgrade rules. Stats are defined here and turned into a runtime
/// <see cref="WeaponData"/> (with the player's upgrades and skills applied) when a run starts.
/// </summary>
public static class WeaponCatalog
{
    public enum Slot { Primary, Secondary }

    public enum UpgradeStat { Damage, FireRate, Magazine, Reload, Accuracy }

    public class WeaponDef
    {
        public string id;
        public string name;
        public string description;
        public Slot slot;
        public WeaponType type;
        public int price;

        public float damage, fireRate, reloadTime, range;
        public int magazine, reserve, pellets = 1;
        public bool auto;
        public float spreadMin, spreadMax, recoil = 1f, adsFov = 45f, adsSpeed = 10f;
        public string fireMode;

        // procedural look of the weapon model
        public Vector3 bodyScale = new Vector3(0.06f, 0.08f, 0.35f);
        public float barrelLength = 0.1f;
        public Color tint = Color.white;
        public bool scope;
    }

    public const int MaxUpgradeRank = 5;
    public static readonly UpgradeStat[] AllStats = { UpgradeStat.Damage, UpgradeStat.FireRate, UpgradeStat.Magazine, UpgradeStat.Reload, UpgradeStat.Accuracy };

    public static readonly List<WeaponDef> All = new List<WeaponDef>
    {
        new WeaponDef { id = "Rifle", name = "MK 14 VECTOR", description = "Reliable full-auto assault rifle.", slot = Slot.Primary, type = WeaponType.Rifle, price = 0,
            damage = 25, fireRate = 9, magazine = 30, reserve = 120, reloadTime = 1.8f, range = 150, auto = true, spreadMin = 0.012f, spreadMax = 0.045f, recoil = 1.2f, adsFov = 45, adsSpeed = 10, fireMode = "FULL AUTO" },

        new WeaponDef { id = "SMG", name = "VIPER SMG", description = "Very fast fire rate. Great up close.", slot = Slot.Primary, type = WeaponType.SMG, price = 600,
            damage = 14, fireRate = 15, magazine = 40, reserve = 160, reloadTime = 1.5f, range = 80, auto = true, spreadMin = 0.02f, spreadMax = 0.07f, recoil = 0.8f, adsFov = 50, adsSpeed = 12, fireMode = "FULL AUTO",
            bodyScale = new Vector3(0.06f, 0.09f, 0.26f), barrelLength = 0.06f, tint = new Color(0.55f, 0.85f, 1f) },

        new WeaponDef { id = "Shotgun", name = "BREACHER", description = "Eight pellets per shot. Devastating at close range.", slot = Slot.Primary, type = WeaponType.Shotgun, price = 900,
            damage = 12, fireRate = 1.3f, magazine = 6, reserve = 36, reloadTime = 2.4f, range = 40, pellets = 8, auto = false, spreadMin = 0.05f, spreadMax = 0.08f, recoil = 2.4f, adsFov = 52, adsSpeed = 9, fireMode = "PUMP",
            bodyScale = new Vector3(0.07f, 0.08f, 0.38f), barrelLength = 0.16f, tint = new Color(1f, 0.7f, 0.4f) },

        new WeaponDef { id = "Sniper", name = "LONGSHOT", description = "One shot, one kill. Zooms far.", slot = Slot.Primary, type = WeaponType.Sniper, price = 1500,
            damage = 160, fireRate = 0.9f, magazine = 5, reserve = 25, reloadTime = 2.6f, range = 300, auto = false, spreadMin = 0.001f, spreadMax = 0.04f, recoil = 3f, adsFov = 15, adsSpeed = 8, fireMode = "BOLT",
            bodyScale = new Vector3(0.05f, 0.075f, 0.46f), barrelLength = 0.2f, tint = new Color(0.8f, 0.6f, 1f), scope = true },

        new WeaponDef { id = "Pistol", name = "P9 APEX", description = "Accurate sidearm.", slot = Slot.Secondary, type = WeaponType.Pistol, price = 0,
            damage = 35, fireRate = 5, magazine = 15, reserve = 60, reloadTime = 1.2f, range = 80, auto = false, spreadMin = 0.008f, spreadMax = 0.03f, recoil = 1.6f, adsFov = 50, adsSpeed = 12, fireMode = "SEMI AUTO" },

        new WeaponDef { id = "MachinePistol", name = "RAZOR MP", description = "Full-auto sidearm with a high fire rate.", slot = Slot.Secondary, type = WeaponType.Pistol, price = 500,
            damage = 11, fireRate = 14, magazine = 30, reserve = 120, reloadTime = 1.4f, range = 60, auto = true, spreadMin = 0.025f, spreadMax = 0.07f, recoil = 0.9f, adsFov = 52, adsSpeed = 12, fireMode = "FULL AUTO",
            tint = new Color(0.6f, 1f, 0.7f) },

        new WeaponDef { id = "Magnum", name = "BULLDOG .44", description = "Slow but hits like a truck.", slot = Slot.Secondary, type = WeaponType.Pistol, price = 700,
            damage = 90, fireRate = 2f, magazine = 6, reserve = 36, reloadTime = 1.8f, range = 100, auto = false, spreadMin = 0.006f, spreadMax = 0.03f, recoil = 2.4f, adsFov = 48, adsSpeed = 11, fireMode = "SEMI AUTO",
            tint = new Color(1f, 0.55f, 0.45f) },
    };

    public static WeaponDef Get(string id)
    {
        foreach (var w in All) if (w.id == id) return w;
        return null;
    }

    public static List<WeaponDef> ForSlot(Slot slot)
    {
        var list = new List<WeaponDef>();
        foreach (var w in All) if (w.slot == slot) list.Add(w);
        return list;
    }

    // --- upgrades ---

    public static string StatName(UpgradeStat stat)
    {
        switch (stat)
        {
            case UpgradeStat.Damage: return "DAMAGE";
            case UpgradeStat.FireRate: return "FIRE RATE";
            case UpgradeStat.Magazine: return "MAGAZINE";
            case UpgradeStat.Reload: return "RELOAD SPEED";
            default: return "ACCURACY";
        }
    }

    public static string UpgradeKey(string weaponId, UpgradeStat stat) => weaponId + ":" + stat;

    public static int GetRank(PlayerProfile p, string weaponId, UpgradeStat stat) =>
        p.WeaponUpgradeRank(UpgradeKey(weaponId, stat));

    public static int UpgradeCost(WeaponDef def, int currentRank)
    {
        return Mathf.RoundToInt(80f * (currentRank + 1) * (1f + def.price / 1000f));
    }

    public static bool TryUpgrade(PlayerProfile p, WeaponDef def, UpgradeStat stat)
    {
        int rank = GetRank(p, def.id, stat);
        if (!p.OwnsWeapon(def.id) || rank >= MaxUpgradeRank) return false;
        int cost = UpgradeCost(def, rank);
        if (p.coins < cost) return false;
        p.coins -= cost;
        p.SetWeaponUpgradeRank(UpgradeKey(def.id, stat), rank + 1);
        SaveSystem.Save();
        return true;
    }

    // --- shop ---

    public static bool TryBuy(PlayerProfile p, WeaponDef def)
    {
        if (p.OwnsWeapon(def.id) || p.coins < def.price) return false;
        p.coins -= def.price;
        p.ownedWeapons.Add(def.id);
        SaveSystem.Save();
        return true;
    }

    public static void Equip(PlayerProfile p, WeaponDef def)
    {
        if (!p.OwnsWeapon(def.id)) return;
        if (def.slot == Slot.Primary) p.primaryWeapon = def.id; else p.secondaryWeapon = def.id;
        SaveSystem.Save();
    }

    // --- building the runtime stats ---

    /// <summary>
    /// Creates the <see cref="WeaponData"/> used in a run: base stats + this weapon's upgrades + the player's skills.
    /// </summary>
    public static WeaponData BuildData(WeaponDef def, PlayerProfile profile)
    {
        var d = ScriptableObject.CreateInstance<WeaponData>();
        d.name = def.id;
        d.weaponName = def.name;
        d.weaponType = def.type;
        d.pellets = def.pellets;
        d.isFullAuto = def.auto;
        d.fireModeDescription = def.fireMode;
        d.range = def.range;
        d.recoilForce = def.recoil;
        d.adsFOV = def.adsFov;
        d.adsSpeed = def.adsSpeed;

        int dmgRank = GetRank(profile, def.id, UpgradeStat.Damage);
        int rateRank = GetRank(profile, def.id, UpgradeStat.FireRate);
        int magRank = GetRank(profile, def.id, UpgradeStat.Magazine);
        int reloadRank = GetRank(profile, def.id, UpgradeStat.Reload);
        int accRank = GetRank(profile, def.id, UpgradeStat.Accuracy);

        float skillDamage = 1f + 0.05f * profile.SkillRank("damage");
        float skillReload = 1f - 0.08f * profile.SkillRank("reload");

        d.damage = def.damage * (1f + 0.08f * dmgRank) * skillDamage;
        d.fireRate = def.fireRate * (1f + 0.05f * rateRank);
        d.magazineSize = Mathf.RoundToInt(def.magazine * (1f + 0.1f * magRank));
        d.reloadTime = Mathf.Max(0.4f, def.reloadTime * (1f - 0.06f * reloadRank) * skillReload);
        d.spreadMin = def.spreadMin * (1f - 0.08f * accRank);
        d.spreadMax = def.spreadMax * (1f - 0.08f * accRank);

        int extraMags = profile.SkillRank("ammo");
        d.maxReserveAmmo = def.reserve + d.magazineSize * extraMags;
        return d;
    }
}

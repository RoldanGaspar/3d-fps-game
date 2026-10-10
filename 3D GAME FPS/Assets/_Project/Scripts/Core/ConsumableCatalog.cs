using System.Collections.Generic;

/// <summary>Single-use items bought in the shop and used during a level.</summary>
public static class ConsumableCatalog
{
    public class ConsumableDef
    {
        public string id;
        public string name;
        public string description;
        public int price;
        public int maxCarry;
    }

    public const string Medkit = "medkit";
    public const string ArmorPlate = "armorplate";
    public const string AmmoBox = "ammobox";

    public static readonly List<ConsumableDef> All = new List<ConsumableDef>
    {
        new ConsumableDef { id = Medkit,     name = "MEDKIT",      description = "Restores 50% of your health.",   price = 60, maxCarry = 5 },
        new ConsumableDef { id = ArmorPlate, name = "ARMOR PLATE", description = "Adds 50 armor instantly.",       price = 80, maxCarry = 5 },
        new ConsumableDef { id = AmmoBox,    name = "AMMO BOX",    description = "Refills the spare ammo of both weapons.", price = 70, maxCarry = 5 },
    };

    public static ConsumableDef Get(string id)
    {
        foreach (var c in All) if (c.id == id) return c;
        return null;
    }

    public static bool TryBuy(PlayerProfile p, ConsumableDef def)
    {
        int count = p.ConsumableCount(def.id);
        if (count >= def.maxCarry || p.coins < def.price) return false;
        p.coins -= def.price;
        p.SetConsumableCount(def.id, count + 1);
        SaveSystem.Save();
        return true;
    }

    /// <summary>Removes one item from the saved inventory when it is used. Returns false if none is left.</summary>
    public static bool TryConsume(PlayerProfile p, string id)
    {
        int count = p.ConsumableCount(id);
        if (count <= 0) return false;
        p.SetConsumableCount(id, count - 1);
        SaveSystem.Save();
        return true;
    }
}

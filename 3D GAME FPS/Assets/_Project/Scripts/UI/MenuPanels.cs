using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Base class for the screens of the main menu. Each panel builds itself in code.</summary>
public abstract class MenuPanel
{
    protected RectTransform root;
    public RectTransform Root => root;

    public virtual void Show()
    {
        Refresh();
        root.gameObject.SetActive(true);
    }

    public virtual void Hide() => root.gameObject.SetActive(false);
    public abstract void Refresh();

    protected static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }

    protected RectTransform BuildFrame(Transform parent, string title, Vector2 size)
    {
        root = UiFactory.Panel(parent, title + "Panel", UiFactory.PanelColor, size);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, -55f);   // leave room for the profile bar
        var outline = root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(UiFactory.Accent.r, UiFactory.Accent.g, UiFactory.Accent.b, 0.6f);
        outline.effectDistance = new Vector2(3f, -3f);
        UiFactory.VerticalList(root, 8f, 28, TextAnchor.UpperCenter);

        var label = UiFactory.Label(root, title, 54f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(label, 64f);
        root.gameObject.SetActive(false);
        return root;
    }

    protected static TextMeshProUGUI Cell(Transform row, string text, float width, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, FontStyles style = FontStyles.Normal)
    {
        var t = UiFactory.Label(row, text, size, color, align, style);
        ((RectTransform)t.transform).sizeDelta = new Vector2(width, 60f);
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.richText = true;
        return t;
    }
}

/// <summary>Level select with a preview of what the level contains.</summary>
public class PlayPanel : MenuPanel
{
    private int _selected = 1;
    private TextMeshProUGUI _levelLabel;
    private TextMeshProUGUI _info;
    private Button _prev, _next;

    public PlayPanel(Transform parent, Action onBack)
    {
        BuildFrame(parent, "SELECT LEVEL", new Vector2(900f, 720f));
        UiFactory.Row(root, 20f);

        var row = UiFactory.Row(root, 130f, "LevelRow");
        UiFactory.HorizontalRow(row, 30f);
        _prev = UiFactory.MakeButton(row, "<", () => Select(_selected - 1), new Vector2(110f, 110f), UiFactory.PanelLight, 60f);
        _levelLabel = UiFactory.Label(row, "", 80f, UiFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        ((RectTransform)_levelLabel.transform).sizeDelta = new Vector2(420f, 120f);
        _next = UiFactory.MakeButton(row, ">", () => Select(_selected + 1), new Vector2(110f, 110f), UiFactory.PanelLight, 60f);

        _info = UiFactory.Label(root, "", 34f, UiFactory.TextColor, TextAlignmentOptions.Center);
        _info.textWrappingMode = TextWrappingModes.Normal;
        _info.richText = true;
        UiFactory.SizeLabelRow(_info, 260f);

        var btnRow = UiFactory.Row(root, 90f, "Buttons");
        UiFactory.HorizontalRow(btnRow, 24f);
        UiFactory.MakeButton(btnRow, "START", () => GameManager.Instance.StartLevel(_selected), new Vector2(380f, 84f), UiFactory.AccentDim, 40f);
        UiFactory.MakeButton(btnRow, "BACK", () => onBack?.Invoke(), new Vector2(300f, 84f), UiFactory.PanelLight, 34f);
    }

    public override void Show()
    {
        _selected = Mathf.Clamp(SaveSystem.Profile.highestLevelUnlocked, 1, SaveSystem.Profile.highestLevelUnlocked);
        base.Show();
    }

    private void Select(int level)
    {
        _selected = Mathf.Clamp(level, 1, SaveSystem.Profile.highestLevelUnlocked);
        Refresh();
    }

    public override void Refresh()
    {
        var p = SaveSystem.Profile;
        var cfg = DifficultyCurve.Get(_selected);
        _levelLabel.text = "LEVEL " + _selected;
        _prev.interactable = _selected > 1;
        _next.interactable = _selected < p.highestLevelUnlocked;

        string cleared = _selected <= p.highestLevelCleared ? "<color=#40F080>CLEARED</color>" : "<color=#9FB3C8>NOT CLEARED</color>";
        _info.text =
            cleared + "\n" +
            "Waves  <b>" + cfg.waves + "</b>    Enemies  <b>" + DifficultyCurve.TotalEnemies(cfg) + "</b>\n" +
            "Enemy health  <b>x" + cfg.healthMultiplier.ToString("0.00") + "</b>    damage  <b>x" + cfg.damageMultiplier.ToString("0.00") + "</b>\n" +
            "<color=#FFD140>Clear bonus  +" + cfg.clearBonusCoins + " coins</color>\n" +
            "<color=#FFD140>" + cfg.coinsPerKill + " coins per kill</color>   <color=#66E0FF>" + cfg.xpPerKill + " XP per kill</color>" +
            (cfg.hasBoss ? "\n<color=#FF5050>BOSS LEVEL</color>" : "");
    }
}

/// <summary>Permanent skills bought with skill points.</summary>
public class SkillsPanel : MenuPanel
{
    private readonly RectTransform _list;
    private readonly TextMeshProUGUI _points;
    private readonly Action _onChanged;
    private Button _respec;

    public SkillsPanel(Transform parent, Action onBack, Action onChanged)
    {
        _onChanged = onChanged;
        BuildFrame(parent, "SKILLS", new Vector2(1100f, 880f));

        _points = UiFactory.Label(root, "", 34f, UiFactory.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(_points, 44f);

        var list = UiFactory.Row(root, SkillCatalog.All.Count * 76f, "List");
        UiFactory.VerticalList(list, 6f, 0, TextAnchor.UpperCenter);
        _list = list;

        var bottom = UiFactory.Row(root, 76f, "Bottom");
        UiFactory.HorizontalRow(bottom, 24f);
        _respec = UiFactory.MakeButton(bottom, "", () =>
        {
            SkillCatalog.TryRespec(SaveSystem.Profile);
            Refresh();
            _onChanged?.Invoke();
        }, new Vector2(420f, 68f), UiFactory.PanelLight, 26f);
        UiFactory.MakeButton(bottom, "BACK", () => onBack?.Invoke(), new Vector2(300f, 68f), UiFactory.AccentDim, 32f);
    }

    public override void Refresh()
    {
        var p = SaveSystem.Profile;
        _points.text = "SKILL POINTS  " + p.skillPoints + "     <size=26><color=#9FB3C8>(earn 1 per player level)</color></size>";
        _points.richText = true;

        ClearChildren(_list);
        foreach (var skill in SkillCatalog.All)
        {
            int rank = p.SkillRank(skill.id);
            var row = UiFactory.Row(_list, 70f, "Skill_" + skill.id);
            UiFactory.HorizontalRow(row, 14f);

            Cell(row, skill.name, 260f, 28f, UiFactory.TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);

            string now = rank > 0 ? "+" + (skill.perRank * rank).ToString("0") + skill.unit : "-";
            string next = rank < skill.maxRank ? "<color=#40F080>next +" + (skill.perRank * (rank + 1)).ToString("0") + skill.unit + "</color>" : "<color=#FFD140>MAX</color>";
            Cell(row, "<size=22><color=#9FB3C8>" + skill.description + "</color></size>\n" + now + "   " + next, 430f, 24f, UiFactory.TextColor);

            Cell(row, rank + "/" + skill.maxRank, 90f, 28f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);

            bool can = SkillCatalog.CanUpgrade(p, skill);
            string cost = rank >= skill.maxRank ? "MAX" : "+  (" + SkillCatalog.CostForNextRank(rank) + ")";
            var skillRef = skill;
            var btn = UiFactory.MakeButton(row, cost, () =>
            {
                if (SkillCatalog.TryUpgrade(SaveSystem.Profile, skillRef)) { Refresh(); _onChanged?.Invoke(); }
            }, new Vector2(150f, 58f), can ? UiFactory.AccentDim : UiFactory.Disabled, 26f);
            btn.interactable = can;
        }

        int respec = SkillCatalog.RespecCost(p);
        UiFactory.SetButtonText(_respec, "RESET SKILLS (" + respec + " coins)");
        _respec.interactable = respec > 0 && p.coins >= respec;
    }
}

/// <summary>Weapon shop, weapon upgrades and consumables.</summary>
public class ShopPanel : MenuPanel
{
    private enum Tab { Weapons, Items, Upgrade }

    private Tab _tab = Tab.Weapons;
    private WeaponCatalog.WeaponDef _upgradeTarget;
    private readonly RectTransform _content;
    private readonly TextMeshProUGUI _coins;
    private readonly Action _onChanged;
    private readonly Button _tabWeapons, _tabItems;

    public ShopPanel(Transform parent, Action onBack, Action onChanged)
    {
        _onChanged = onChanged;
        BuildFrame(parent, "SHOP", new Vector2(1200f, 900f));

        _coins = UiFactory.Label(root, "", 34f, UiFactory.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(_coins, 44f);

        var tabs = UiFactory.Row(root, 70f, "Tabs");
        UiFactory.HorizontalRow(tabs, 16f);
        _tabWeapons = UiFactory.MakeButton(tabs, "WEAPONS", () => { _tab = Tab.Weapons; Refresh(); }, new Vector2(300f, 62f), UiFactory.PanelLight, 28f);
        _tabItems = UiFactory.MakeButton(tabs, "ITEMS", () => { _tab = Tab.Items; Refresh(); }, new Vector2(300f, 62f), UiFactory.PanelLight, 28f);

        var content = UiFactory.Row(root, 600f, "Content");
        UiFactory.VerticalList(content, 6f, 0, TextAnchor.UpperCenter);
        _content = content;

        var bottom = UiFactory.Row(root, 76f, "Bottom");
        UiFactory.HorizontalRow(bottom, 24f);
        UiFactory.MakeButton(bottom, "BACK", () =>
        {
            if (_tab == Tab.Upgrade) { _tab = Tab.Weapons; Refresh(); }
            else onBack?.Invoke();
        }, new Vector2(300f, 68f), UiFactory.AccentDim, 32f);
    }

    public override void Show()
    {
        _tab = Tab.Weapons;
        base.Show();
    }

    public override void Refresh()
    {
        var p = SaveSystem.Profile;
        _coins.text = "COINS  " + p.coins;
        _tabWeapons.GetComponent<Image>().color = _tab != Tab.Items ? UiFactory.AccentDim : UiFactory.PanelLight;
        _tabItems.GetComponent<Image>().color = _tab == Tab.Items ? UiFactory.AccentDim : UiFactory.PanelLight;

        ClearChildren(_content);
        switch (_tab)
        {
            case Tab.Weapons: BuildWeapons(p); break;
            case Tab.Items: BuildItems(p); break;
            case Tab.Upgrade: BuildUpgrades(p); break;
        }
    }

    private void Changed()
    {
        Refresh();
        _onChanged?.Invoke();
    }

    private static string Summary(WeaponCatalog.WeaponDef d)
    {
        string dmg = d.pellets > 1 ? d.damage.ToString("0") + "x" + d.pellets : d.damage.ToString("0");
        return "DMG " + dmg + "   RPM " + (d.fireRate * 60f).ToString("0") + "   MAG " + d.magazine;
    }

    private void BuildWeapons(PlayerProfile p)
    {
        foreach (var slot in new[] { WeaponCatalog.Slot.Primary, WeaponCatalog.Slot.Secondary })
        {
            var header = UiFactory.Label(_content, slot == WeaponCatalog.Slot.Primary ? "PRIMARY" : "SECONDARY", 24f, UiFactory.AccentDim, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            UiFactory.SizeLabelRow(header, 30f);

            foreach (var def in WeaponCatalog.ForSlot(slot))
            {
                var d = def;
                var row = UiFactory.Row(_content, 66f, "Weapon_" + d.id);
                UiFactory.HorizontalRow(row, 12f);

                bool owned = p.OwnsWeapon(d.id);
                bool equipped = (slot == WeaponCatalog.Slot.Primary ? p.primaryWeapon : p.secondaryWeapon) == d.id;

                Cell(row, d.name, 270f, 28f, equipped ? UiFactory.Good : UiFactory.TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
                Cell(row, "<size=22>" + Summary(d) + "</size>", 380f, 24f, UiFactory.TextColor);

                if (!owned)
                {
                    bool can = p.coins >= d.price;
                    var buy = UiFactory.MakeButton(row, "BUY  " + d.price, () =>
                    {
                        if (WeaponCatalog.TryBuy(SaveSystem.Profile, d)) { WeaponCatalog.Equip(SaveSystem.Profile, d); Changed(); }
                    }, new Vector2(210f, 56f), can ? UiFactory.AccentDim : UiFactory.Disabled, 26f);
                    buy.interactable = can;
                }
                else
                {
                    UiFactory.MakeButton(row, equipped ? "EQUIPPED" : "EQUIP", () => { WeaponCatalog.Equip(SaveSystem.Profile, d); Changed(); },
                        new Vector2(170f, 56f), equipped ? UiFactory.Good : UiFactory.PanelLight, 24f).interactable = !equipped;
                    UiFactory.MakeButton(row, "UPGRADE", () => { _upgradeTarget = d; _tab = Tab.Upgrade; Refresh(); },
                        new Vector2(170f, 56f), UiFactory.AccentDim, 24f);
                }
            }
        }
    }

    private void BuildUpgrades(PlayerProfile p)
    {
        var d = _upgradeTarget;
        var title = UiFactory.Label(_content, d.name + "  -  UPGRADES", 36f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(title, 52f);
        var info = UiFactory.Label(_content, "<size=24><color=#9FB3C8>" + d.description + "</color></size>", 28f, UiFactory.TextColor);
        info.richText = true;
        UiFactory.SizeLabelRow(info, 40f);

        foreach (var stat in WeaponCatalog.AllStats)
        {
            var s = stat;
            int rank = WeaponCatalog.GetRank(p, d.id, s);
            var row = UiFactory.Row(_content, 80f, "Up_" + s);
            UiFactory.HorizontalRow(row, 16f);

            Cell(row, WeaponCatalog.StatName(s), 260f, 28f, UiFactory.TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            var pips = new System.Text.StringBuilder();
            for (int i = 0; i < WeaponCatalog.MaxUpgradeRank; i++) pips.Append(i < rank ? "<color=#1AE6FF>#</color> " : "<color=#35506A>#</color> ");
            Cell(row, pips.ToString(), 250f, 34f, UiFactory.TextColor, TextAlignmentOptions.Center);

            Cell(row, StatPreview(d, s, rank), 260f, 24f, UiFactory.TextColor);

            bool maxed = rank >= WeaponCatalog.MaxUpgradeRank;
            int cost = WeaponCatalog.UpgradeCost(d, rank);
            bool can = !maxed && p.coins >= cost;
            var btn = UiFactory.MakeButton(row, maxed ? "MAX" : cost.ToString(), () =>
            {
                if (WeaponCatalog.TryUpgrade(SaveSystem.Profile, d, s)) Changed();
            }, new Vector2(170f, 58f), can ? UiFactory.AccentDim : UiFactory.Disabled, 28f);
            btn.interactable = can;
        }
    }

    private static string StatPreview(WeaponCatalog.WeaponDef d, WeaponCatalog.UpgradeStat stat, int rank)
    {
        switch (stat)
        {
            case WeaponCatalog.UpgradeStat.Damage: return (d.damage * (1f + 0.08f * rank)).ToString("0.#") + "  <color=#40F080>-> " + (d.damage * (1f + 0.08f * (rank + 1))).ToString("0.#") + "</color>";
            case WeaponCatalog.UpgradeStat.FireRate: return (d.fireRate * 60f * (1f + 0.05f * rank)).ToString("0") + " rpm";
            case WeaponCatalog.UpgradeStat.Magazine: return Mathf.RoundToInt(d.magazine * (1f + 0.1f * rank)) + " rounds";
            case WeaponCatalog.UpgradeStat.Reload: return (d.reloadTime * (1f - 0.06f * rank)).ToString("0.0#") + " s";
            default: return "spread -" + (8 * rank) + "%";
        }
    }

    private void BuildItems(PlayerProfile p)
    {
        var hint = UiFactory.Label(_content, "<size=24><color=#9FB3C8>Items are used during a level (keys 4 / 5 / 6 or the item buttons).</color></size>", 26f, UiFactory.TextColor);
        hint.richText = true;
        UiFactory.SizeLabelRow(hint, 40f);

        foreach (var def in ConsumableCatalog.All)
        {
            var d = def;
            int count = p.ConsumableCount(d.id);
            var row = UiFactory.Row(_content, 90f, "Item_" + d.id);
            UiFactory.HorizontalRow(row, 14f);

            Cell(row, d.name, 250f, 30f, UiFactory.TextColor, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
            Cell(row, "<size=24>" + d.description + "</size>", 450f, 26f, UiFactory.TextColor);
            Cell(row, "OWNED  <b>" + count + "</b>/" + d.maxCarry, 150f, 26f, UiFactory.Accent, TextAlignmentOptions.Center);

            bool can = count < d.maxCarry && p.coins >= d.price;
            var buy = UiFactory.MakeButton(row, count >= d.maxCarry ? "FULL" : "BUY  " + d.price, () =>
            {
                if (ConsumableCatalog.TryBuy(SaveSystem.Profile, d)) Changed();
            }, new Vector2(190f, 60f), can ? UiFactory.AccentDim : UiFactory.Disabled, 26f);
            buy.interactable = can;
        }
    }
}

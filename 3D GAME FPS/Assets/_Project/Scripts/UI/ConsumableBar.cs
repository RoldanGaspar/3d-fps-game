using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// In-run quick-use buttons for the consumables bought in the shop (Medkit, Armor Plate, Ammo Box).
/// Keys 4 / 5 / 6 on keyboard, buttons on the left edge for touch.
/// </summary>
public class ConsumableBar : MonoBehaviour
{
    private struct Slot
    {
        public string id;
        public Button button;
        public TextMeshProUGUI label;
    }

    private readonly List<Slot> _slots = new List<Slot>();
    private PlayerHealth _health;
    private WeaponManager _weapons;

    private void Awake()
    {
        var canvas = UiFactory.CreateCanvas("ConsumableCanvas", 7, transform);
        var column = UiFactory.Panel(canvas.transform, "Column", new Color(0f, 0f, 0f, 0f), new Vector2(190f, 360f));
        column.anchorMin = column.anchorMax = new Vector2(0f, 0.5f);
        column.pivot = new Vector2(0f, 0.5f);
        column.anchoredPosition = new Vector2(24f, 40f);
        column.GetComponent<Image>().raycastTarget = false;
        UiFactory.VerticalList(column, 14f, 0, TextAnchor.MiddleLeft);

        string[] hints = { "4", "5", "6" };
        int i = 0;
        foreach (var def in ConsumableCatalog.All)
        {
            string id = def.id;
            var row = UiFactory.Row(column, 100f, "Slot_" + id);
            var button = UiFactory.MakeButton(row, "", () => Use(id), new Vector2(180f, 96f), UiFactory.PanelColor, 22f);
            var label = button.GetComponentInChildren<TextMeshProUGUI>();
            label.textWrappingMode = TextWrappingModes.Normal;
            label.richText = true;
            _slots.Add(new Slot { id = id, button = button, label = label });
            label.text = HintFor(hints[Mathf.Min(i, 2)], def, SaveSystem.Profile.ConsumableCount(id));
            i++;
        }
    }

    private void Start()
    {
        _health = FindFirstObjectByType<PlayerHealth>();
        _weapons = FindFirstObjectByType<WeaponManager>();
        Refresh();
    }

    private static string HintFor(string key, ConsumableCatalog.ConsumableDef def, int count)
    {
        return "<size=18><color=#9FB3C8>[" + key + "]</color></size>\n" + def.name + "\n<color=#FFD140>x" + count + "</color>";
    }

    private void Refresh()
    {
        string[] hints = { "4", "5", "6" };
        for (int i = 0; i < _slots.Count; i++)
        {
            var def = ConsumableCatalog.Get(_slots[i].id);
            int count = SaveSystem.Profile.ConsumableCount(_slots[i].id);
            _slots[i].label.text = HintFor(hints[Mathf.Min(i, 2)], def, count);
            _slots[i].button.interactable = count > 0;
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || Time.timeScale == 0f) return;
        if (kb.digit4Key.wasPressedThisFrame) Use(ConsumableCatalog.Medkit);
        if (kb.digit5Key.wasPressedThisFrame) Use(ConsumableCatalog.ArmorPlate);
        if (kb.digit6Key.wasPressedThisFrame) Use(ConsumableCatalog.AmmoBox);
    }

    private void Use(string id)
    {
        if (_health == null || !_health.IsAlive || Time.timeScale == 0f) return;
        var profile = SaveSystem.Profile;
        if (profile.ConsumableCount(id) <= 0) return;

        bool used = false;
        switch (id)
        {
            case ConsumableCatalog.Medkit:
                if (_health.CurrentHealth < _health.MaxHealth - 0.5f)
                {
                    _health.Heal(_health.MaxHealth * 0.5f);
                    used = true;
                }
                break;
            case ConsumableCatalog.ArmorPlate:
                if (_health.MaxArmor <= 0f) _health.SetMaxArmor(50f, true);
                else _health.AddArmor(50f);
                used = true;
                break;
            case ConsumableCatalog.AmmoBox:
                if (_weapons != null)
                {
                    foreach (var w in _weapons.Weapons)
                        if (w != null && w.Data != null) w.AddAmmo(w.Data.maxReserveAmmo);
                    used = true;
                }
                break;
        }

        if (used)
        {
            ConsumableCatalog.TryConsume(profile, id);
            VfxPool.Instance.PlaySfxAt(ProceduralSfx.Get(SfxId.KillConfirm), _health.transform.position, 0.5f);
        }
        Refresh();
    }
}

using UnityEngine;

/// <summary>
/// Applies the saved profile to the player at the start of a level: health, armor, regeneration, speed,
/// and the two equipped weapons with their upgrades.
/// </summary>
public static class PlayerLoadout
{
    public static void Apply(GameObject player)
    {
        if (player == null) return;
        var profile = SaveSystem.Profile;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.SetMaxHealth(PlayerStats.MaxHealth, true);
            health.SetMaxArmor(PlayerStats.MaxArmor, true);
            health.RegenPerSecond = PlayerStats.RegenPerSecond;
        }

        var motor = player.GetComponent<PlayerMotor>();
        if (motor != null) motor.SpeedMultiplier = PlayerStats.SpeedMultiplier;

        var manager = player.GetComponentInChildren<WeaponManager>(true);
        if (manager == null || manager.Weapons.Count == 0) return;

        AssignWeapon(manager.Weapons[0], WeaponCatalog.Get(profile.primaryWeapon), profile);
        if (manager.Weapons.Count > 1)
            AssignWeapon(manager.Weapons[1], WeaponCatalog.Get(profile.secondaryWeapon), profile);

        manager.RefreshCurrent();
    }

    private static void AssignWeapon(WeaponBase weapon, WeaponCatalog.WeaponDef def, PlayerProfile profile)
    {
        if (weapon == null || def == null) return;
        weapon.Initialize(WeaponCatalog.BuildData(def, profile));
        WeaponVisualStyler.Apply(weapon.transform, def);
    }
}

/// <summary>
/// Gives each weapon a different silhouette by reshaping the box-built view model, so a shotgun doesn't look like a rifle.
/// </summary>
public static class WeaponVisualStyler
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    public static void Apply(Transform weapon, WeaponCatalog.WeaponDef def)
    {
        var visual = weapon.Find("VisualModel");
        if (visual == null) return;

        var body = visual.Find("Body");
        var accent = visual.Find("Accent");
        var barrel = visual.Find("Barrel");

        // Primary weapons start from the rifle proportions; pistols keep their own and only get tinted.
        if (def.slot == WeaponCatalog.Slot.Primary)
        {
            if (body != null)
            {
                body.localScale = def.bodyScale;
                body.localPosition = new Vector3(0f, 0f, def.bodyScale.z * 0.28f);
            }
            if (accent != null)
            {
                var s = accent.localScale; s.z = def.bodyScale.z * 0.85f; accent.localScale = s;
                accent.localPosition = new Vector3(0f, def.bodyScale.y * 0.62f, def.bodyScale.z * 0.28f);
            }
            if (barrel != null)
            {
                var s = barrel.localScale; s.y = def.barrelLength; barrel.localScale = s;
                barrel.localPosition = new Vector3(0f, 0.01f, def.bodyScale.z * 0.28f + def.bodyScale.z * 0.5f + def.barrelLength * 0.3f);
            }
        }

        Tint(body, def.tint);
        Tint(accent, def.tint);

        var scope = visual.Find("Scope");
        if (def.scope && scope == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Scope";
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(visual, false);
            go.transform.localScale = new Vector3(0.035f, 0.035f, 0.2f);
            go.transform.localPosition = new Vector3(0f, def.bodyScale.y * 0.5f + 0.045f, def.bodyScale.z * 0.28f);
            go.GetComponent<MeshRenderer>().sharedMaterial = body != null ? body.GetComponent<MeshRenderer>().sharedMaterial : null;
        }
        else if (!def.scope && scope != null)
        {
            Object.Destroy(scope.gameObject);
        }
    }

    private static void Tint(Transform part, Color tint)
    {
        if (part == null) return;
        var r = part.GetComponent<MeshRenderer>();
        if (r == null) return;
        if (tint == Color.white)
        {
            r.SetPropertyBlock(null);
            return;
        }

        // The property block replaces the material colour, so multiply the tint into it.
        Color baseColor = r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId)
            ? r.sharedMaterial.GetColor(BaseColorId) : Color.white;
        var block = new MaterialPropertyBlock();
        block.SetColor(BaseColorId, baseColor * tint);
        r.SetPropertyBlock(block);
    }
}

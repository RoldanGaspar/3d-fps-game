using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads and saves the <see cref="PlayerProfile"/> as JSON in the persistent data path.
/// Writes go to a temporary file first so a crash can never leave a half-written save.
/// </summary>
public static class SaveSystem
{
    private const string FileName = "profile.json";
    private static PlayerProfile _profile;

    public static event Action OnProfileChanged;

    private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static PlayerProfile Profile
    {
        get
        {
            if (_profile == null) Load();
            return _profile;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _profile = null;
        OnProfileChanged = null;
    }

    public static void Load()
    {
        _profile = null;
        try
        {
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                _profile = JsonUtility.FromJson<PlayerProfile>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not read save file, starting fresh: " + e.Message);
        }

        if (_profile == null) _profile = new PlayerProfile();
        Migrate(_profile);
    }

    public static void Save()
    {
        if (_profile == null) return;
        try
        {
            string json = JsonUtility.ToJson(_profile, true);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(tmp, FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not write save file: " + e.Message);
        }
        OnProfileChanged?.Invoke();
    }

    public static void ResetProfile()
    {
        _profile = new PlayerProfile();
        Save();
    }

    private static void Migrate(PlayerProfile p)
    {
        // Place future save-format upgrades here, keyed by p.version.
        if (p.version < PlayerProfile.CurrentVersion)
            p.version = PlayerProfile.CurrentVersion;

        if (p.ownedWeapons == null) p.ownedWeapons = new System.Collections.Generic.List<string> { "Rifle", "Pistol" };
        if (p.weaponUpgrades == null) p.weaponUpgrades = new System.Collections.Generic.List<PlayerProfile.UpgradeEntry>();
        if (p.consumables == null) p.consumables = new System.Collections.Generic.List<PlayerProfile.CountEntry>();
        if (p.skills == null) p.skills = new System.Collections.Generic.List<PlayerProfile.UpgradeEntry>();
        if (p.level < 1) p.level = 1;
        if (p.highestLevelUnlocked < 1) p.highestLevelUnlocked = 1;
    }
}

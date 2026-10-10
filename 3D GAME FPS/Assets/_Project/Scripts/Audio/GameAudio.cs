using System;
using UnityEngine;

/// <summary>
/// Global volume buses (Master / SFX / Music), persisted in PlayerPrefs.
/// Master drives AudioListener.volume; SFX and Music are multipliers that sound players read.
/// </summary>
public static class GameAudio
{
    private const string MasterKey = "audio_master";
    private const string SfxKey = "audio_sfx";
    private const string MusicKey = "audio_music";

    private static bool _loaded;
    private static float _master = 1f;
    private static float _sfx = 0.9f;
    private static float _music = 0.6f;

    public static event Action OnVolumesChanged;

    public static float Master
    {
        get { EnsureLoaded(); return _master; }
        set { EnsureLoaded(); _master = Mathf.Clamp01(value); Apply(MasterKey, _master); }
    }

    public static float Sfx
    {
        get { EnsureLoaded(); return _sfx; }
        set { EnsureLoaded(); _sfx = Mathf.Clamp01(value); Apply(SfxKey, _sfx); }
    }

    public static float Music
    {
        get { EnsureLoaded(); return _music; }
        set { EnsureLoaded(); _music = Mathf.Clamp01(value); Apply(MusicKey, _music); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _loaded = false;
        OnVolumesChanged = null;
    }

    private static void EnsureLoaded()
    {
        if (_loaded) return;
        _loaded = true;
        _master = PlayerPrefs.GetFloat(MasterKey, _master);
        _sfx = PlayerPrefs.GetFloat(SfxKey, _sfx);
        _music = PlayerPrefs.GetFloat(MusicKey, _music);
        AudioListener.volume = _master;
    }

    private static void Apply(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        AudioListener.volume = _master;
        OnVolumesChanged?.Invoke();
    }

    /// <summary>Call once at startup so the saved master volume is applied.</summary>
    public static void Initialize()
    {
        EnsureLoaded();
    }
}

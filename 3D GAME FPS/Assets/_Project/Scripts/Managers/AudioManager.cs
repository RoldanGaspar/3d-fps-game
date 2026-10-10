using UnityEngine;

/// <summary>
/// Audio manager for 2D SFX (UI, stingers) and music playback.
/// Respects the Master / SFX / Music buses in <see cref="GameAudio"/>.
/// 3D combat sounds are played through <see cref="VfxPool.PlaySfxAt"/>.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    [Header("Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    protected override void Awake()
    {
        base.Awake();

        if (_sfxSource == null) _sfxSource = CreateSource("SFX", false);
        if (_musicSource == null) _musicSource = CreateSource("Music", true);

        GameAudio.Initialize();
        GameAudio.OnVolumesChanged += ApplyVolumes;
        ApplyVolumes();
    }

    protected override void OnDestroy()
    {
        GameAudio.OnVolumesChanged -= ApplyVolumes;
        base.OnDestroy();
    }

    private AudioSource CreateSource(string childName, bool loop)
    {
        var go = new GameObject(childName);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = loop;
        src.spatialBlend = 0f;
        return src;
    }

    private void ApplyVolumes()
    {
        if (_musicSource != null) _musicSource.volume = GameAudio.Music;
    }

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip != null && _sfxSource != null)
            _sfxSource.PlayOneShot(clip, volume * GameAudio.Sfx);
    }

    public void PlaySFX(SfxId id, float volume = 1f)
    {
        PlaySFX(ProceduralSfx.Get(id), volume);
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        _musicSource.clip = clip;
        _musicSource.loop = loop;
        _musicSource.volume = GameAudio.Music;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        GameAudio.Music = volume;
    }

    public void SetSFXVolume(float volume)
    {
        GameAudio.Sfx = volume;
    }
}

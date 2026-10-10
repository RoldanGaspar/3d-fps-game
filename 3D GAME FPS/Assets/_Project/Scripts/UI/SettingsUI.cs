using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Settings menu: volume, sensitivity, graphics quality.
/// Volume sliders are bound to the Master / SFX / Music buses in <see cref="GameAudio"/>
/// and are saved automatically.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private Slider _masterVolume;
    [SerializeField] private Slider _sfxVolume;
    [SerializeField] private Slider _musicVolume;

    [Header("Controls")]
    [SerializeField] private Slider _sensitivitySlider;

    [Header("Graphics")]
    [SerializeField] private Dropdown _qualityDropdown;

    private void Start()
    {
        BindSlider(_masterVolume, GameAudio.Master, v => GameAudio.Master = v);
        BindSlider(_sfxVolume, GameAudio.Sfx, v => GameAudio.Sfx = v);
        BindSlider(_musicVolume, GameAudio.Music, v => GameAudio.Music = v);
    }

    private static void BindSlider(Slider slider, float current, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.SetValueWithoutNotify(current);
        slider.onValueChanged.AddListener(onChanged);
    }

    public void OnSensitivityChanged(float value)
    {
        // TODO: Apply sensitivity
    }

    public void OnQualityChanged(int index)
    {
        QualitySettings.SetQualityLevel(index);
    }

    public void SaveSettings()
    {
        PlayerPrefs.Save();
    }
}

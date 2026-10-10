using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The settings screen shared by the main menu and the pause menu: frame rate (30 / 60 / 120 / Auto),
/// graphics quality, battery saver, FPS counter, look sensitivity and volumes. Built entirely in code.
/// </summary>
public class SettingsPanel
{
    private readonly RectTransform _root;
    private readonly List<(Button button, int value)> _fpsButtons = new List<(Button, int)>();
    private readonly List<(Button button, int value)> _qualityButtons = new List<(Button, int)>();
    private Button _batteryButton;
    private Button _fpsCounterButton;

    public RectTransform Root => _root;

    public SettingsPanel(Transform parent, System.Action onClose)
    {
        _root = UiFactory.Panel(parent, "SettingsPanel", UiFactory.PanelColor, new Vector2(1000f, 900f));
        _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 0.5f);
        _root.anchoredPosition = new Vector2(0f, -40f);   // leave room for the profile bar
        UiFactory.VerticalList(_root, 8f, 30, TextAnchor.UpperCenter);
        var outline = _root.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(UiFactory.Accent.r, UiFactory.Accent.g, UiFactory.Accent.b, 0.6f);
        outline.effectDistance = new Vector2(3f, -3f);

        var title = UiFactory.Label(_root, "SETTINGS", 56f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(title, 64f);

        // --- Frame rate ---
        AddHeader("FRAME RATE");
        var fpsRow = UiFactory.Row(_root, 72f, "FpsRow");
        UiFactory.HorizontalRow(fpsRow, 16f);
        foreach (int fps in new[] { 30, 60, 120 })
        {
            AddChoice(fpsRow, fps + " FPS", fps, _fpsButtons, v => { GameSettings.SetTargetFps(v); Refresh(); });
            // 120 FPS is shown everywhere but can only be picked on screens that refresh that fast.
            if (!GameSettings.AvailableFpsOptions().Contains(fps))
                _fpsButtons[_fpsButtons.Count - 1].button.interactable = false;
        }
        AddChoice(fpsRow, "AUTO", 0, _fpsButtons, v => { GameSettings.SetTargetFps(v); Refresh(); });

        // --- Graphics ---
        AddHeader("GRAPHICS");
        var gfxRow = UiFactory.Row(_root, 72f, "GraphicsRow");
        UiFactory.HorizontalRow(gfxRow, 16f);
        AddChoice(gfxRow, "LOW", GameSettings.QualityLow, _qualityButtons, v => { GameSettings.SetGraphicsQuality(v); Refresh(); });
        AddChoice(gfxRow, "MEDIUM", GameSettings.QualityMedium, _qualityButtons, v => { GameSettings.SetGraphicsQuality(v); Refresh(); });
        AddChoice(gfxRow, "HIGH", GameSettings.QualityHigh, _qualityButtons, v => { GameSettings.SetGraphicsQuality(v); Refresh(); });

        // --- Toggles ---
        var toggleRow = UiFactory.Row(_root, 72f, "ToggleRow");
        UiFactory.HorizontalRow(toggleRow, 16f);
        _batteryButton = UiFactory.MakeButton(toggleRow, "", () =>
        {
            GameSettings.SetBatterySaver(!SaveSystem.Profile.batterySaver);
            Refresh();
        }, new Vector2(440f, 70f), UiFactory.PanelLight, 26f);
        _fpsCounterButton = UiFactory.MakeButton(toggleRow, "", () =>
        {
            GameSettings.SetShowFps(!SaveSystem.Profile.showFps);
            Refresh();
        }, new Vector2(440f, 70f), UiFactory.PanelLight, 26f);

        // --- Sliders ---
        AddHeader("CONTROLS & AUDIO");
        UiFactory.MakeSlider(_root, "Sensitivity", 0.25f, 3f, SaveSystem.Profile.sensitivity, v => GameSettings.SetSensitivity(v));
        UiFactory.MakeSlider(_root, "Master", 0f, 1f, GameAudio.Master, v => GameAudio.Master = v);
        UiFactory.MakeSlider(_root, "Effects", 0f, 1f, GameAudio.Sfx, v => GameAudio.Sfx = v);
        UiFactory.MakeSlider(_root, "Music", 0f, 1f, GameAudio.Music, v => GameAudio.Music = v);

        var closeRow = UiFactory.Row(_root, 76f, "CloseRow");
        UiFactory.HorizontalRow(closeRow, 16f);
        UiFactory.MakeButton(closeRow, "BACK", () => { PlayerPrefs.Save(); onClose?.Invoke(); }, new Vector2(360f, 70f), UiFactory.AccentDim, 30f);

        Refresh();
    }

    private void AddHeader(string text)
    {
        var label = UiFactory.Label(_root, text, 26f, UiFactory.AccentDim, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        UiFactory.SizeLabelRow(label, 30f);
    }

    private static void AddChoice(Transform row, string text, int value, List<(Button, int)> list, System.Action<int> onPick)
    {
        var button = UiFactory.MakeButton(row, text, () => onPick(value), new Vector2(200f, 70f), UiFactory.PanelLight, 28f);
        list.Add((button, value));
    }

    public void Refresh()
    {
        var p = SaveSystem.Profile;
        foreach (var (button, value) in _fpsButtons)
            button.GetComponent<Image>().color = value == p.targetFps ? UiFactory.AccentDim : UiFactory.PanelLight;
        foreach (var (button, value) in _qualityButtons)
            button.GetComponent<Image>().color = value == p.graphicsQuality ? UiFactory.AccentDim : UiFactory.PanelLight;

        UiFactory.SetButtonText(_batteryButton, "BATTERY SAVER: " + (p.batterySaver ? "ON" : "OFF"));
        UiFactory.SetButtonText(_fpsCounterButton, "SHOW FPS: " + (p.showFps ? "ON" : "OFF"));
    }
}

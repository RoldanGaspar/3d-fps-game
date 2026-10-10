using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// In-run pause menu: Resume, Settings and Quit to menu. Opens with Escape / P, or the pause button
/// in the corner (for touch devices). Created automatically by the LevelDirector.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private Canvas _canvas;
    private RectTransform _menu;
    private SettingsPanel _settings;
    private Button _pauseButton;

    private void Awake()
    {
        _canvas = UiFactory.CreateCanvas("PauseCanvas", 150, transform);

        // small pause button, top-left
        _pauseButton = UiFactory.MakeButton(_canvas.transform, "II", TogglePause, new Vector2(84f, 84f), UiFactory.PanelColor, 36f);
        var brt = (RectTransform)_pauseButton.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0f, 1f);
        brt.pivot = new Vector2(0f, 1f);
        brt.anchoredPosition = new Vector2(30f, -30f);

        var dim = UiFactory.Stretch(_canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
        _menu = dim;

        var column = UiFactory.Panel(dim, "Column", UiFactory.PanelColor, new Vector2(620f, 560f));
        column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
        column.anchoredPosition = Vector2.zero;
        UiFactory.VerticalList(column, 18f, 36, TextAnchor.UpperCenter);
        var title = UiFactory.Label(column, "PAUSED", 64f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(title, 84f);
        AddButton(column, "RESUME", Resume, UiFactory.AccentDim);
        AddButton(column, "SETTINGS", OpenSettings, UiFactory.PanelLight);
        AddButton(column, "QUIT TO MENU", () => { Resume(); GameManager.Instance.ReturnToMenu(); }, UiFactory.PanelLight);

        _settings = new SettingsPanel(dim, CloseSettings);
        _settings.Root.gameObject.SetActive(false);
        _menu.gameObject.SetActive(false);
        _menuColumn = column;
    }

    private RectTransform _menuColumn;

    private static void AddButton(Transform parent, string text, UnityEngine.Events.UnityAction action, Color color)
    {
        var row = UiFactory.Row(parent, 90f, "Row_" + text);
        UiFactory.MakeButton(row, text, action, new Vector2(500f, 84f), color, 38f);
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) TogglePause();
    }

    private void OnDestroy()
    {
        // Never leave the game frozen if this object goes away while paused.
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged;
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        // the pause button only makes sense while the level is running
        if (_pauseButton != null) _pauseButton.gameObject.SetActive(state == GameManager.GameState.Playing);
    }

    public void TogglePause()
    {
        if (PerkPickerUI.IsOpen) return;
        var gm = GameManager.Instance;
        if (gm.CurrentState == GameManager.GameState.Playing) Pause();
        else if (gm.CurrentState == GameManager.GameState.Paused) Resume();
    }

    private void Pause()
    {
        GameManager.Instance.PauseGame();
        _menu.gameObject.SetActive(true);
        _menuColumn.gameObject.SetActive(true);
        _settings.Root.gameObject.SetActive(false);
        _pauseButton.gameObject.SetActive(false);
    }

    private void Resume()
    {
        GameManager.Instance.ResumeGame();
        _menu.gameObject.SetActive(false);
        _pauseButton.gameObject.SetActive(true);
    }

    private void OpenSettings()
    {
        _menuColumn.gameObject.SetActive(false);
        _settings.Refresh();
        _settings.Root.gameObject.SetActive(true);
    }

    private void CloseSettings()
    {
        _settings.Root.gameObject.SetActive(false);
        _menuColumn.gameObject.SetActive(true);
    }
}

using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu controller. Builds the whole menu in code: profile bar, main buttons and the Play / Skills /
/// Shop / Settings panels. Put this on an empty object in the MainMenu scene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    private RectTransform _home;
    private TextMeshProUGUI _profileText;
    private TextMeshProUGUI _xpText;
    private Image _xpFill;
    private RectTransform _overlay;
    private PlayPanel _play;
    private SkillsPanel _skills;
    private ShopPanel _shop;
    private SettingsPanel _settings;
    private MenuPanel _open;

    private void Awake()
    {
        GameManager.Instance.SetState(GameManager.GameState.Menu);
        Time.timeScale = 1f;
        Build();
    }

    private void Start()
    {
        RefreshProfile();
    }

    private void OnEnable() => SaveSystem.OnProfileChanged += RefreshProfile;
    private void OnDisable() => SaveSystem.OnProfileChanged -= RefreshProfile;

    private void Build()
    {
        var canvas = UiFactory.CreateCanvas("MenuCanvas", 10, transform);
        _overlay = UiFactory.Stretch(canvas.transform, "Overlay", new Color(0f, 0f, 0f, 0f));
        _overlay.GetComponent<Image>().raycastTarget = false;

        BuildProfileBar(canvas.transform);
        BuildHome(canvas.transform);

        // overlay panels sit above the home column
        var panels = UiFactory.Stretch(canvas.transform, "Panels", new Color(0f, 0f, 0f, 0f));
        panels.GetComponent<Image>().raycastTarget = false;
        _play = new PlayPanel(panels, CloseCurrent);
        _skills = new SkillsPanel(panels, CloseCurrent, RefreshProfile);
        _shop = new ShopPanel(panels, CloseCurrent, RefreshProfile);
        _settings = new SettingsPanel(panels, CloseCurrent);
        _settings.Root.gameObject.SetActive(false);
    }

    private void BuildProfileBar(Transform parent)
    {
        var bar = UiFactory.Panel(parent, "ProfileBar", UiFactory.PanelColor, new Vector2(1000f, 120f));
        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 1f);
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = new Vector2(0f, -30f);
        var outline = bar.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(UiFactory.Accent.r, UiFactory.Accent.g, UiFactory.Accent.b, 0.5f);

        _profileText = UiFactory.Label(bar, "", 36f, UiFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        _profileText.richText = true;
        var prt = (RectTransform)_profileText.transform;
        prt.anchorMin = new Vector2(0f, 0.45f); prt.anchorMax = new Vector2(1f, 1f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        var back = UiFactory.Panel(bar, "XpBack", new Color(0.12f, 0.18f, 0.27f, 1f), Vector2.zero);
        back.anchorMin = new Vector2(0.08f, 0.18f); back.anchorMax = new Vector2(0.92f, 0.34f);
        back.offsetMin = back.offsetMax = Vector2.zero;
        var fill = UiFactory.Panel(back, "XpFill", UiFactory.Accent, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0.5f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;
        _xpFill = fill.GetComponent<Image>();
        _xpText = UiFactory.Label(back, "", 20f, UiFactory.TextColor, TextAlignmentOptions.Center);
        var xrt = (RectTransform)_xpText.transform;
        xrt.anchorMin = Vector2.zero; xrt.anchorMax = Vector2.one; xrt.offsetMin = xrt.offsetMax = Vector2.zero;
    }

    private void BuildHome(Transform parent)
    {
        _home = UiFactory.Panel(parent, "Home", new Color(0f, 0f, 0f, 0f), new Vector2(520f, 700f));
        _home.anchorMin = _home.anchorMax = new Vector2(0f, 0.5f);
        _home.pivot = new Vector2(0f, 0.5f);
        _home.anchoredPosition = new Vector2(110f, -60f);
        _home.GetComponent<Image>().raycastTarget = false;
        UiFactory.VerticalList(_home, 18f, 0, TextAnchor.MiddleLeft);

        var title = UiFactory.Label(_home, "PARKOUR SHOOTER", 66f, UiFactory.Accent, TextAlignmentOptions.MidlineLeft, FontStyles.Bold);
        UiFactory.SizeLabelRow(title, 90f);
        UiFactory.Row(_home, 30f);

        AddHomeButton("PLAY", () => Open(_play), UiFactory.AccentDim);
        AddHomeButton("SKILLS", () => Open(_skills), UiFactory.PanelLight);
        AddHomeButton("SHOP", () => Open(_shop), UiFactory.PanelLight);
        AddHomeButton("SETTINGS", () => Open(new SettingsMenuAdapter(_settings)), UiFactory.PanelLight);
        AddHomeButton("QUIT", Application.Quit, UiFactory.PanelLight);
    }

    private void AddHomeButton(string text, UnityEngine.Events.UnityAction action, Color color)
    {
        var row = UiFactory.Row(_home, 92f, "Row_" + text);
        UiFactory.MakeButton(row, text, action, new Vector2(460f, 86f), color, 40f);
    }

    private void Open(MenuPanel panel)
    {
        if (_open != null) _open.Hide();
        _open = panel;
        _home.gameObject.SetActive(false);
        panel.Show();
    }

    private void CloseCurrent()
    {
        if (_open != null) _open.Hide();
        _open = null;
        _home.gameObject.SetActive(true);
        RefreshProfile();
    }

    private void RefreshProfile()
    {
        if (_profileText == null) return;
        var p = SaveSystem.Profile;
        int need = DifficultyCurve.XpForNextLevel(p.level);
        _profileText.text = "LEVEL " + p.level + "     <color=#FFD140>" + p.coins + " COINS</color>     <color=#66E0FF>" + p.skillPoints + " SKILL PTS</color>";
        _xpText.text = p.xp + " / " + need + " XP";
        _xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)p.xp / need), 1f);
    }

    /// <summary>Lets the shared SettingsPanel be shown through the same open/close logic as the other panels.</summary>
    private class SettingsMenuAdapter : MenuPanel
    {
        private readonly SettingsPanel _panel;
        public SettingsMenuAdapter(SettingsPanel panel) { _panel = panel; root = panel.Root; }
        public override void Refresh() => _panel.Refresh();
    }
}

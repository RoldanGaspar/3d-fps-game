using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-run screens built in code: "YOU DIED" (revive / retry / menu) and "LEVEL CLEARED"
/// (next level / replay / menu) with the run statistics and rewards.
/// </summary>
public class RunResultUI : MonoBehaviour
{
    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _panel;
    private TextMeshProUGUI _title;
    private TextMeshProUGUI _subtitle;
    private TextMeshProUGUI _stats;
    private Button _primary;
    private Button _secondary;
    private Button _menu;
    private Button _revive;
    private Tween _tween;

    private void Awake()
    {
        Build();
        Hide();
    }

    private void OnEnable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnStateChanged;
        _tween.Stop();
    }

    private void OnStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver) ShowGameOver();
        else if (state == GameManager.GameState.Victory) ShowVictory();
        else Hide();
    }

    private void Build()
    {
        _canvas = UiFactory.CreateCanvas("RunResultCanvas", 200, transform);
        _group = _canvas.gameObject.AddComponent<CanvasGroup>();

        var dim = UiFactory.Stretch(_canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.7f));
        dim.GetComponent<Image>().raycastTarget = true;

        _panel = UiFactory.Panel(dim, "Panel", UiFactory.PanelColor, new Vector2(900f, 840f));
        _panel.anchorMin = _panel.anchorMax = new Vector2(0.5f, 0.5f);
        _panel.anchoredPosition = Vector2.zero;
        var outline = _panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(UiFactory.Accent.r, UiFactory.Accent.g, UiFactory.Accent.b, 0.6f);
        outline.effectDistance = new Vector2(3f, -3f);

        UiFactory.VerticalList(_panel, 14f, 40, TextAnchor.UpperCenter);

        _title = UiFactory.Label(_panel, "", 78f, UiFactory.Danger, TextAlignmentOptions.Center, FontStyles.Bold);
        UiFactory.SizeLabelRow(_title, 100f);
        _subtitle = UiFactory.Label(_panel, "", 34f, UiFactory.TextColor);
        UiFactory.SizeLabelRow(_subtitle, 48f);

        UiFactory.Row(_panel, 10f, "Spacer");

        _stats = UiFactory.Label(_panel, "", 36f, UiFactory.TextColor, TextAlignmentOptions.Center);
        _stats.textWrappingMode = TextWrappingModes.Normal;
        UiFactory.SizeLabelRow(_stats, 300f);

        UiFactory.Row(_panel, 10f, "Spacer2");

        var row = UiFactory.Row(_panel, 90f, "Buttons");
        UiFactory.HorizontalRow(row, 24f);
        _primary = UiFactory.MakeButton(row, "", null, new Vector2(380f, 84f), UiFactory.AccentDim);
        _secondary = UiFactory.MakeButton(row, "", null, new Vector2(380f, 84f), UiFactory.PanelLight);

        var menuRow = UiFactory.Row(_panel, 80f, "MenuRow");
        UiFactory.HorizontalRow(menuRow, 20f);
        _menu = UiFactory.MakeButton(menuRow, "MAIN MENU", () => GameManager.Instance.ReturnToMenu(), new Vector2(380f, 72f), UiFactory.PanelLight, 30f);
        _revive = _primary;
    }

    private void Show()
    {
        _canvas.gameObject.SetActive(true);
        _group.alpha = 0f;
        _panel.localScale = Vector3.one * 0.92f;
        _tween.Stop();
        _tween = Tween.Custom(this, 0f, 1f, 0.35f, (c, k) =>
        {
            c._group.alpha = k;
            c._panel.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, k);
        }, Ease.OutCubic, useUnscaledTime: true);
    }

    private void Hide()
    {
        if (_canvas != null) _canvas.gameObject.SetActive(false);
    }

    private void ShowGameOver()
    {
        var gm = GameManager.Instance;
        _title.text = "YOU DIED";
        _title.color = UiFactory.Danger;
        _subtitle.text = "LEVEL " + GameSession.CurrentLevel + "  -  WAVE " + Mathf.Max(1, GameSession.WaveReached);
        _stats.text = BuildStats(false);

        _primary.onClick.RemoveAllListeners();
        _secondary.onClick.RemoveAllListeners();

        bool canRevive = GameSession.CanRevive;
        UiFactory.SetButtonText(_primary, "REVIVE  (" + GameSession.ReviveCost + " coins)");
        _primary.interactable = canRevive;
        _primary.GetComponent<Image>().color = canRevive ? UiFactory.AccentDim : UiFactory.Disabled;
        _primary.onClick.AddListener(() =>
        {
            var death = FindFirstObjectByType<PlayerDeathController>();
            if (death != null && GameSession.TryRevive())
            {
                death.Revive();
                gm.Revived();
            }
        });

        UiFactory.SetButtonText(_secondary, "RETRY");
        _secondary.GetComponent<Image>().color = UiFactory.PanelLight;
        _secondary.onClick.AddListener(() => gm.RestartLevel());

        Show();
    }

    private void ShowVictory()
    {
        var gm = GameManager.Instance;
        _title.text = "LEVEL CLEARED";
        _title.color = UiFactory.Good;
        _subtitle.text = "LEVEL " + GameSession.CurrentLevel;
        _stats.text = BuildStats(true);

        _primary.onClick.RemoveAllListeners();
        _secondary.onClick.RemoveAllListeners();

        UiFactory.SetButtonText(_primary, "NEXT LEVEL");
        _primary.interactable = true;
        _primary.GetComponent<Image>().color = UiFactory.AccentDim;
        _primary.onClick.AddListener(() => gm.NextLevel());

        UiFactory.SetButtonText(_secondary, "REPLAY");
        _secondary.GetComponent<Image>().color = UiFactory.PanelLight;
        _secondary.onClick.AddListener(() => gm.RestartLevel());

        Show();
    }

    private static string BuildStats(bool victory)
    {
        int seconds = Mathf.FloorToInt(GameSession.Elapsed);
        var cfg = GameSession.Config;
        int coins = GameSession.CoinsEarned;
        string coinLine = victory
            ? "<color=#FFD140>+" + coins + " coins</color>  (incl. +" + cfg.clearBonusCoins + " clear bonus)"
            : "<color=#FFD140>+" + coins + " coins</color>";

        string text =
            "Enemies defeated   <b>" + GameSession.Kills + "</b>\n" +
            "Waves reached      <b>" + Mathf.Max(1, GameSession.WaveReached) + " / " + cfg.waves + "</b>\n" +
            "Time               <b>" + (seconds / 60) + ":" + (seconds % 60).ToString("00") + "</b>\n\n" +
            coinLine + "\n" +
            "<color=#66E0FF>+" + GameSession.XpEarned + " XP</color>";

        if (!victory) text += "\n\n<size=26><color=#9FB3C8>Rewards are saved when you leave this screen.</color></size>";
        return text;
    }
}

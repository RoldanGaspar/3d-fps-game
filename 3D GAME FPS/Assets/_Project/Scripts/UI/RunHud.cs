using TMPro;
using UnityEngine;

/// <summary>
/// Small in-run heads-up display: level, wave, enemies remaining, kills and coins earned.
/// Built in code on its own canvas so it doesn't depend on the existing HUD layout.
/// </summary>
public class RunHud : MonoBehaviour
{
    private TextMeshProUGUI _top;
    private TextMeshProUGUI _side;
    private TextMeshProUGUI _banner;
    private EnemySpawner _spawner;
    private float _bannerUntil;

    private void Awake()
    {
        var canvas = UiFactory.CreateCanvas("RunHudCanvas", 6, transform);
        canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;

        _top = UiFactory.Label(canvas.transform, "", 34f, UiFactory.TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        var rt = (RectTransform)_top.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -22f);
        rt.sizeDelta = new Vector2(900f, 50f);

        _side = UiFactory.Label(canvas.transform, "", 28f, UiFactory.Gold, TextAlignmentOptions.TopRight, FontStyles.Bold);
        _side.richText = true;
        var srt = (RectTransform)_side.transform;
        srt.anchorMin = srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(1f, 1f);
        srt.anchoredPosition = new Vector2(-30f, -120f);
        srt.sizeDelta = new Vector2(420f, 120f);

        _banner = UiFactory.Label(canvas.transform, "", 78f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        var brt = (RectTransform)_banner.transform;
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.72f);
        brt.sizeDelta = new Vector2(1200f, 110f);
        _banner.gameObject.SetActive(false);
    }

    private PlayerHealth _health;

    private void Start()
    {
        _spawner = FindFirstObjectByType<EnemySpawner>();
        if (_spawner != null) _spawner.OnWaveStarted += OnWaveStarted;
        GameSession.OnStatsChanged += Refresh;

        _health = FindFirstObjectByType<PlayerHealth>();
        if (_health != null) _health.OnArmorChanged.AddListener(OnArmorChanged);
        Refresh();
    }

    private void OnDestroy()
    {
        if (_spawner != null) _spawner.OnWaveStarted -= OnWaveStarted;
        GameSession.OnStatsChanged -= Refresh;
        if (_health != null) _health.OnArmorChanged.RemoveListener(OnArmorChanged);
    }

    private void OnArmorChanged(float normalized) => Refresh();

    private void OnWaveStarted(int wave)
    {
        _banner.text = wave == 1 ? "LEVEL " + GameSession.CurrentLevel : "WAVE " + wave;
        _banner.gameObject.SetActive(true);
        _bannerUntil = Time.unscaledTime + 2f;
        Refresh();
    }

    private void Update()
    {
        if (_banner.gameObject.activeSelf)
        {
            float left = _bannerUntil - Time.unscaledTime;
            if (left <= 0f) _banner.gameObject.SetActive(false);
            else _banner.alpha = Mathf.Clamp01(left * 2f);
        }

        // enemies alive changes without a stats event
        if (_spawner != null && Time.frameCount % 15 == 0) Refresh();
    }

    private void Refresh()
    {
        var cfg = GameSession.Config;
        int wave = Mathf.Max(1, GameSession.WaveReached);
        int alive = _spawner != null ? _spawner.EnemiesAlive : 0;
        _top.text = "LEVEL " + GameSession.CurrentLevel + "   |   WAVE " + wave + "/" + cfg.waves + "   |   ENEMIES " + alive;
        string armor = _health != null && _health.MaxArmor > 0f
            ? "\n<color=#66E0FF>ARMOR " + Mathf.CeilToInt(_health.Armor) + "/" + Mathf.CeilToInt(_health.MaxArmor) + "</color>"
            : "";
        _side.text = "KILLS " + GameSession.Kills + "\n+" + GameSession.CoinsEarned + " COINS" + armor;
    }
}

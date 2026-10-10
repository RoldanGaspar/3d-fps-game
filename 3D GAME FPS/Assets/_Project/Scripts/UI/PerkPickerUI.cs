using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Between waves the game pauses and offers three random perks; the player picks one to enhance the run
/// (more health, armor, damage, speed, ammo...). Built in code.
/// </summary>
public class PerkPickerUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    private Canvas _canvas;
    private CanvasGroup _group;
    private RectTransform _cards;
    private TextMeshProUGUI _title;
    private Tween _tween;

    private void Awake()
    {
        _canvas = UiFactory.CreateCanvas("PerkCanvas", 180, transform);
        _group = _canvas.gameObject.AddComponent<CanvasGroup>();
        var dim = UiFactory.Stretch(_canvas.transform, "Dim", new Color(0f, 0f, 0f, 0.72f));

        _title = UiFactory.Label(dim, "WAVE CLEARED  -  CHOOSE AN UPGRADE", 52f, UiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
        var trt = (RectTransform)_title.transform;
        trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.anchoredPosition = new Vector2(0f, 300f);
        trt.sizeDelta = new Vector2(1500f, 80f);

        _cards = UiFactory.Panel(dim, "Cards", new Color(0f, 0f, 0f, 0f), new Vector2(1500f, 420f));
        _cards.anchorMin = _cards.anchorMax = new Vector2(0.5f, 0.5f);
        _cards.anchoredPosition = new Vector2(0f, -30f);
        _cards.GetComponent<Image>().raycastTarget = false;
        UiFactory.HorizontalRow(_cards, 40f);

        _canvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        IsOpen = false;
        _tween.Stop();
    }

    public void Show()
    {
        if (IsOpen) return;
        IsOpen = true;

        for (int i = _cards.childCount - 1; i >= 0; i--) Destroy(_cards.GetChild(i).gameObject);

        foreach (var perk in RunModifiers.Roll(3))
        {
            var p = perk;
            var card = UiFactory.Panel(_cards, "Card_" + p.id, UiFactory.PanelColor, new Vector2(450f, 380f));
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(UiFactory.Accent.r, UiFactory.Accent.g, UiFactory.Accent.b, 0.7f);
            outline.effectDistance = new Vector2(3f, -3f);
            UiFactory.VerticalList(card, 16f, 28, TextAnchor.UpperCenter);

            var name = UiFactory.Label(card, p.name, 44f, UiFactory.Gold, TextAlignmentOptions.Center, FontStyles.Bold);
            UiFactory.SizeLabelRow(name, 70f);
            var desc = UiFactory.Label(card, p.description, 30f, UiFactory.TextColor, TextAlignmentOptions.Center);
            desc.textWrappingMode = TextWrappingModes.Normal;
            UiFactory.SizeLabelRow(desc, 130f);
            var row = UiFactory.Row(card, 80f, "Pick");
            UiFactory.MakeButton(row, "CHOOSE", () => Pick(p), new Vector2(300f, 74f), UiFactory.AccentDim, 34f);
        }

        _canvas.gameObject.SetActive(true);
        _group.alpha = 0f;
        _tween.Stop();
        _tween = Tween.Alpha(_group, 1f, 0.3f, Ease.OutCubic, useUnscaledTime: true);
        GameManager.Instance.PauseGame();
    }

    private void Pick(RunModifiers.PerkDef perk)
    {
        RunModifiers.Apply(perk);
        _canvas.gameObject.SetActive(false);
        IsOpen = false;
        GameManager.Instance.ResumeGame();
    }
}

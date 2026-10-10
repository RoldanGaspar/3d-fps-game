using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds UGUI elements in code with one consistent look (dark navy panels, cyan accents) so menus and
/// result screens don't need hand-made prefabs. Every screen in the game uses these helpers.
/// </summary>
public static class UiFactory
{
    public static readonly Color PanelColor = new Color(0.05f, 0.09f, 0.15f, 0.94f);
    public static readonly Color PanelLight = new Color(0.09f, 0.15f, 0.24f, 0.96f);
    public static readonly Color Accent = new Color(0.1f, 0.9f, 1f, 1f);
    public static readonly Color AccentDim = new Color(0.1f, 0.45f, 0.55f, 1f);
    public static readonly Color Good = new Color(0.25f, 0.95f, 0.5f, 1f);
    public static readonly Color Danger = new Color(1f, 0.3f, 0.3f, 1f);
    public static readonly Color Gold = new Color(1f, 0.82f, 0.25f, 1f);
    public static readonly Color TextColor = new Color(0.92f, 0.97f, 1f, 1f);
    public static readonly Color Disabled = new Color(0.2f, 0.24f, 0.3f, 1f);

    public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        if (parent != null) go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();
        return canvas;
    }

    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>();
    }

    public static RectTransform Panel(Transform parent, string name, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.color = color;
        return rt;
    }

    public static RectTransform Stretch(Transform parent, string name, Color color)
    {
        var rt = Panel(parent, name, color, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return rt;
    }

    public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Label", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(0f, size * 1.4f);
        return tmp;
    }

    public static Button MakeButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick,
        Vector2 size, Color? color = null, float fontSize = 34f)
    {
        var rt = Panel(parent, "Button_" + text, color ?? AccentDim, size);
        var img = rt.GetComponent<Image>();

        var btn = rt.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
        btn.colors = colors;
        if (onClick != null) btn.onClick.AddListener(onClick);

        var outline = rt.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Accent.r, Accent.g, Accent.b, 0.55f);
        outline.effectDistance = new Vector2(2f, -2f);

        var label = Label(rt, text, fontSize, TextColor, TextAlignmentOptions.Center, FontStyles.Bold);
        var lrt = (RectTransform)label.transform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        return btn;
    }

    /// <summary>A labelled slider row: "Label  [=====o----]  value".</summary>
    public static Slider MakeSlider(Transform parent, string label, float min, float max, float value,
        UnityEngine.Events.UnityAction<float> onChanged, float width = 760f)
    {
        var row = Row(parent, 56f, "Slider_" + label);
        HorizontalRow(row, 20f);

        var text = Label(row, label, 30f, TextColor, TextAlignmentOptions.MidlineLeft);
        ((RectTransform)text.transform).sizeDelta = new Vector2(width * 0.33f, 50f);

        var sliderRt = Panel(row, "Slider", new Color(0, 0, 0, 0), new Vector2(width * 0.65f, 40f));
        sliderRt.GetComponent<Image>().raycastTarget = false;

        var bg = Panel(sliderRt, "Background", new Color(0.12f, 0.18f, 0.27f, 1f), Vector2.zero);
        bg.anchorMin = new Vector2(0f, 0.35f); bg.anchorMax = new Vector2(1f, 0.65f);
        bg.offsetMin = bg.offsetMax = Vector2.zero;

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderRt, false);
        var fillAreaRt = (RectTransform)fillArea.transform;
        fillAreaRt.anchorMin = new Vector2(0f, 0.35f); fillAreaRt.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRt.offsetMin = new Vector2(10f, 0f); fillAreaRt.offsetMax = new Vector2(-10f, 0f);
        var fill = Panel(fillAreaRt, "Fill", Accent, Vector2.zero);
        fill.anchorMin = Vector2.zero; fill.anchorMax = new Vector2(0f, 1f);
        fill.offsetMin = fill.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderRt, false);
        var handleAreaRt = (RectTransform)handleArea.transform;
        handleAreaRt.anchorMin = Vector2.zero; handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(10f, 0f); handleAreaRt.offsetMax = new Vector2(-10f, 0f);
        var handle = Panel(handleAreaRt, "Handle", Color.white, new Vector2(30f, 44f));

        var slider = sliderRt.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
        return slider;
    }

    public static void SetButtonText(Button button, string text)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = text;
    }

    public static VerticalLayoutGroup VerticalList(Transform parent, float spacing, int padding = 0,
        TextAnchor align = TextAnchor.UpperCenter)
    {
        var vlg = parent.gameObject.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.padding = new RectOffset(padding, padding, padding, padding);
        vlg.childAlignment = align;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;       // use each child's LayoutElement preferred height
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        return vlg;
    }

    public static HorizontalLayoutGroup HorizontalRow(Transform parent, float spacing,
        TextAnchor align = TextAnchor.MiddleCenter)
    {
        var hlg = parent.gameObject.GetComponent<HorizontalLayoutGroup>();
        if (hlg == null) hlg = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = spacing;
        hlg.childAlignment = align;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        return hlg;
    }

    /// <summary>A fixed-height row inside a vertical list (spacer, label holder, button row...).</summary>
    public static RectTransform Row(Transform parent, float height, string name = "Row")
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(0f, height);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
        return rt;
    }

    public static void SizeLabelRow(TextMeshProUGUI label, float height)
    {
        var le = label.gameObject.GetComponent<LayoutElement>();
        if (le == null) le = label.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;
    }
}

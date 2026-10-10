using TMPro;
using UnityEngine;

/// <summary>
/// Optional FPS counter (Settings > Show FPS). Lives for the whole session and works in release builds.
/// </summary>
public class FpsCounter : MonoBehaviour
{
    private TextMeshProUGUI _label;
    private float _timer;
    private int _frames;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<FpsCounter>() != null) return;
        var go = new GameObject("[FpsCounter]");
        DontDestroyOnLoad(go);
        go.AddComponent<FpsCounter>();
    }

    private void Awake()
    {
        var canvas = UiFactory.CreateCanvas("FpsCanvas", 500, transform);
        canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;
        _label = UiFactory.Label(canvas.transform, "", 30f, new Color(0.4f, 1f, 0.6f, 1f), TextAlignmentOptions.TopRight, FontStyles.Bold);
        var rt = (RectTransform)_label.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -16f);
        rt.sizeDelta = new Vector2(300f, 44f);
    }

    private void Update()
    {
        bool show = GameSettings.ShowFps;
        if (_label.gameObject.activeSelf != show) _label.gameObject.SetActive(show);
        if (!show) return;

        _frames++;
        _timer += Time.unscaledDeltaTime;
        if (_timer < 0.5f) return;
        float fps = _frames / _timer;
        _label.text = fps.ToString("0") + " FPS";
        _label.color = fps >= 55f ? new Color(0.4f, 1f, 0.6f, 1f) : fps >= 28f ? new Color(1f, 0.85f, 0.3f, 1f) : new Color(1f, 0.35f, 0.3f, 1f);
        _frames = 0;
        _timer = 0f;
    }
}

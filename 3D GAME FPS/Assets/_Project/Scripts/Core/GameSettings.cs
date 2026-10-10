using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Applies the saved settings to the engine: target frame rate (30 / 60 / 120 / auto), graphics quality,
/// look sensitivity, battery saver and the FPS counter. Settings live in the <see cref="PlayerProfile"/>.
/// </summary>
public static class GameSettings
{
    public const int QualityLow = 0, QualityMedium = 1, QualityHigh = 2;

    /// <summary>Multiplier for mouse / touch look speed (1 = default).</summary>
    public static float LookScale { get; private set; } = 1f;
    public static bool ShowFps { get; private set; }

    private static bool _applied;
    private static float _originalRenderScale = -1f;
    private static float _originalShadowDistance = -1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        _applied = false;
        ApplyAll();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode) RestoreAssetValues();
        };
#endif
    }

    public static PlayerProfile Profile => SaveSystem.Profile;

    /// <summary>The refresh rate of the screen in Hz (rounded).</summary>
    public static int DeviceRefreshRate
    {
        get
        {
            double hz = Screen.currentResolution.refreshRateRatio.value;
            return hz > 1 ? Mathf.RoundToInt((float)hz) : 60;
        }
    }

    /// <summary>The FPS choices this device can actually show.</summary>
    public static List<int> AvailableFpsOptions()
    {
        var list = new List<int> { 30, 60 };
        if (DeviceRefreshRate >= 110) list.Add(120);
        return list;
    }

    public static void ApplyAll()
    {
        var p = Profile;
        ApplyFrameRate(p.targetFps, p.batterySaver);
        ApplyGraphics(p.graphicsQuality);
        LookScale = Mathf.Clamp(p.sensitivity, 0.25f, 3f);
        ShowFps = p.showFps;
        _applied = true;
    }

    public static void SetTargetFps(int fps)
    {
        Profile.targetFps = fps;
        ApplyFrameRate(fps, Profile.batterySaver);
        SaveSystem.Save();
    }

    public static void SetGraphicsQuality(int level)
    {
        Profile.graphicsQuality = Mathf.Clamp(level, 0, 2);
        ApplyGraphics(Profile.graphicsQuality);
        SaveSystem.Save();
    }

    public static void SetSensitivity(float scale)
    {
        Profile.sensitivity = scale;
        LookScale = Mathf.Clamp(scale, 0.25f, 3f);
        SaveSystem.Save();
    }

    public static void SetBatterySaver(bool on)
    {
        Profile.batterySaver = on;
        ApplyFrameRate(Profile.targetFps, on);
        SaveSystem.Save();
    }

    public static void SetShowFps(bool on)
    {
        Profile.showFps = on;
        ShowFps = on;
        SaveSystem.Save();
    }

    private static void ApplyFrameRate(int requested, bool batterySaver)
    {
        int fps = requested;
        if (fps <= 0) fps = DeviceRefreshRate >= 110 ? 60 : Mathf.Clamp(DeviceRefreshRate, 30, 60);   // auto: smooth but cool
        if (batterySaver) fps = 30;
        // Never ask for more than the screen can show.
        if (fps > DeviceRefreshRate + 5) fps = DeviceRefreshRate;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = fps;
    }

    private static void ApplyGraphics(int level)
    {
        bool battery = Profile.batterySaver;
        if (battery) level = Mathf.Min(level, QualityLow);

        // Effects
        VfxQuality.ParticleScale = level == QualityLow ? 0.4f : level == QualityMedium ? 0.7f : 1f;
        foreach (var fx in Object.FindObjectsByType<PostFxController>(FindObjectsSortMode.None))
            fx.SetEnabled(level >= QualityMedium);

        // Render pipeline
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline != null)
        {
            if (_originalRenderScale < 0f) _originalRenderScale = pipeline.renderScale;
            if (_originalShadowDistance < 0f) _originalShadowDistance = pipeline.shadowDistance;

            pipeline.renderScale = level == QualityLow ? 0.7f : level == QualityMedium ? 0.85f : 1f;
            pipeline.shadowDistance = level == QualityLow ? 20f : level == QualityMedium ? 35f : Mathf.Max(_originalShadowDistance, 50f);
        }
    }

    /// <summary>The pipeline asset is a project file; put its values back when leaving play mode in the editor.</summary>
    public static void RestoreAssetValues()
    {
        var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) return;
        if (_originalRenderScale > 0f) pipeline.renderScale = _originalRenderScale;
        if (_originalShadowDistance > 0f) pipeline.shadowDistance = _originalShadowDistance;
    }
}

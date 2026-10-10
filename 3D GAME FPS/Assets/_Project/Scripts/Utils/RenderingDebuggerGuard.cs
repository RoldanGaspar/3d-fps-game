using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Unity's built-in Rendering Debugger ("Display Stats" window) can be opened in the editor and in development
/// builds with Ctrl+Backspace, a three-finger double tap or a gamepad combo. It is not part of the game, so it is
/// switched off here and a stray double tap can never open it in front of the player.
/// Developers can still bring it back with F4 (see <see cref="PerformanceMonitor"/>).
/// </summary>
public static class RenderingDebuggerGuard
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Disable()
    {
        DebugManager.instance.enableRuntimeUI = false;
    }

    /// <summary>Development helper: show or hide Unity's runtime Rendering Debugger.</summary>
    public static void Toggle()
    {
        var dm = DebugManager.instance;
        bool on = !dm.enableRuntimeUI;
        dm.enableRuntimeUI = on;
        dm.displayRuntimeUI = on;
    }
}

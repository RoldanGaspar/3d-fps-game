#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Development-only overlay (toggle with F3) that shows FPS and per-frame GC allocations,
/// so garbage-producing code is easy to spot on a device. Compiled out of release builds.
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] private float _spikeThresholdBytes = 1024f;

    private bool _show;
    private float _fps;
    private float _fpsTimer;
    private int _fpsFrames;
    private long _totalAlloc;
    private int _sampledFrames;
    private long _worstFrame;
    private int _spikeFrames;
    private long _lastFrameAlloc;
    private long _previousAllocated;
    private bool _hasPrevious;
    private GUIStyle _style;

    public float AverageBytesPerFrame => _sampledFrames > 0 ? (float)_totalAlloc / _sampledFrames : 0f;
    public long WorstFrameBytes => _worstFrame;
    public int SpikeFrames => _spikeFrames;
    public int SampledFrames => _sampledFrames;

    public static PerformanceMonitor Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("[PerformanceMonitor]");
        DontDestroyOnLoad(go);
        go.AddComponent<PerformanceMonitor>();
    }

    private void Awake()
    {
        Instance = this;
    }

    public void ResetStats()
    {
        _hasPrevious = false;
        _totalAlloc = 0;
        _sampledFrames = 0;
        _worstFrame = 0;
        _spikeFrames = 0;
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.f3Key.wasPressedThisFrame) _show = !_show;
            if (Keyboard.current.f4Key.wasPressedThisFrame) RenderingDebuggerGuard.Toggle();   // Unity's own debugger, dev only
        }

        _fpsFrames++;
        _fpsTimer += Time.unscaledDeltaTime;
        if (_fpsTimer >= 0.5f)
        {
            _fps = _fpsFrames / _fpsTimer;
            _fpsFrames = 0;
            _fpsTimer = 0f;
        }

        // Approximate managed allocation per frame from growth of the mono heap. (GC.GetAllocatedBytes
        // is not implemented on Unity's Mono.) A frame in which a collection ran shows a negative delta
        // and is counted as 0, so treat the numbers as a lower bound.
        long used = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
        _lastFrameAlloc = 0;
        if (_hasPrevious)
        {
            long delta = used - _previousAllocated;
            if (delta > 0) _lastFrameAlloc = delta;
        }
        _hasPrevious = true;
        _previousAllocated = used;
        _totalAlloc += _lastFrameAlloc;
        _sampledFrames++;
        if (_lastFrameAlloc > _worstFrame) _worstFrame = _lastFrameAlloc;
        if (_lastFrameAlloc > _spikeThresholdBytes) _spikeFrames++;
    }

    private void OnGUI()
    {
        if (!_show) return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
        }

        GUI.Box(new Rect(8, 8, 330, 92), GUIContent.none);
        GUI.Label(new Rect(14, 10, 320, 22), $"FPS {_fps:0}   ({1000f / Mathf.Max(1f, _fps):0.0} ms)", _style);
        GUI.Label(new Rect(14, 32, 320, 22), $"GC alloc/frame {_lastFrameAlloc} B   avg {AverageBytesPerFrame:0}", _style);
        GUI.Label(new Rect(14, 54, 320, 22), $"worst {_worstFrame} B   spikes>{_spikeThresholdBytes:0}B: {_spikeFrames}", _style);
        GUI.Label(new Rect(14, 76, 320, 22), $"mono heap {UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576f:0.0} MB", _style);
    }
}
#endif

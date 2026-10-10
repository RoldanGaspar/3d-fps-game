#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Development helper: samples a text line every rendered frame so movement and animation can be reviewed
/// afterwards (frame by frame) without a debugger. Not used by the game itself.
/// </summary>
public class DevRecorder : MonoBehaviour
{
    private static DevRecorder _instance;
    private Func<string> _sampler;
    private readonly List<string> _lines = new List<string>();

    public static IReadOnlyList<string> Lines => _instance != null ? _instance._lines : (IReadOnlyList<string>)Array.Empty<string>();

    /// <summary>Starts (or restarts) recording. The sampler is called once per frame, after all updates.</summary>
    public static void Begin(Func<string> sampler)
    {
        if (_instance == null)
        {
            var go = new GameObject("DevRecorder") { hideFlags = HideFlags.HideAndDontSave };
            _instance = go.AddComponent<DevRecorder>();
        }
        _instance._lines.Clear();
        _instance._sampler = sampler;
    }

    public static void Clear()
    {
        if (_instance != null) _instance._lines.Clear();
    }

    public static void End()
    {
        if (_instance != null)
        {
            Destroy(_instance.gameObject);
            _instance = null;
        }
    }

    private void LateUpdate()
    {
        if (_sampler == null) return;
        _lines.Add(Time.frameCount + " " + _sampler());
    }
}
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
/// <summary>
/// Development helper: takes a picture from a camera that follows a target every N frames and stitches the
/// pictures into one contact-sheet PNG, so a whole move (vault, slide, wall run...) can be reviewed at a glance.
/// </summary>
public class DevShots : MonoBehaviour
{
    private static DevShots _instance;

    private Camera _cam;
    private Camera _source;
    private Transform _target;
    private Vector3 _localOffset;      // where the camera sits, in the target's local space
    private Vector3 _lookOffset;       // point to look at, relative to the target position
    private int _every = 4;
    private int _counter;
    private int _width = 320, _height = 320;
    private readonly List<Texture2D> _frames = new List<Texture2D>();

    public static int Count => _instance != null ? _instance._frames.Count : 0;

    public static void Begin(Transform target, Vector3 localOffset, Vector3 lookOffset, int everyNFrames, int width = 320, int height = 320, float fov = 50f)
    {
        End();
        var go = new GameObject("DevShots") { hideFlags = HideFlags.HideAndDontSave };
        _instance = go.AddComponent<DevShots>();
        _instance._cam = go.AddComponent<Camera>();
        _instance._cam.enabled = false;
        _instance._cam.fieldOfView = fov;
        _instance._cam.clearFlags = CameraClearFlags.SolidColor;
        _instance._cam.backgroundColor = new Color(0.55f, 0.65f, 0.75f);
        _instance._cam.nearClipPlane = 0.05f;
        _instance._target = target;
        _instance._localOffset = localOffset;
        _instance._lookOffset = lookOffset;
        _instance._every = Mathf.Max(1, everyNFrames);
        _instance._width = width;
        _instance._height = height;
    }

    /// <summary>Takes pictures exactly as the given camera sees the game (position, rotation and field of view).</summary>
    public static void BeginFromCamera(Camera source, int everyNFrames, int width = 480, int height = 270)
    {
        Begin(source.transform, Vector3.zero, Vector3.forward, everyNFrames, width, height, source.fieldOfView);
        _instance._source = source;
    }

    public static void End()
    {
        if (_instance != null)
        {
            foreach (var t in _instance._frames) Destroy(t);
            Destroy(_instance.gameObject);
            _instance = null;
        }
    }

    public static void Snap()
    {
        if (_instance != null) _instance.Capture();
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        if (_counter++ % _every == 0) Capture();
    }

    private void Capture()
    {
        if (_target == null || _cam == null) return;
        if (_source != null)
        {
            _cam.transform.SetPositionAndRotation(_source.transform.position, _source.transform.rotation);
            _cam.fieldOfView = _source.fieldOfView;
        }
        else
        {
            _cam.transform.position = _target.TransformPoint(_localOffset);
            _cam.transform.LookAt(_target.position + _lookOffset);
        }

        var rt = RenderTexture.GetTemporary(_width, _height, 24, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        _cam.targetTexture = rt;
        _cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(_width, _height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, _width, _height), 0, 0);
        tex.Apply();
        _cam.targetTexture = null;
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        _frames.Add(tex);
    }

    /// <summary>Writes all pictures into one PNG, left to right and top to bottom. Returns the picture count.</summary>
    public static int Save(string path, int columns)
    {
        if (_instance == null || _instance._frames.Count == 0) return 0;
        var f = _instance._frames;
        int w = _instance._width, h = _instance._height;
        int rows = (f.Count + columns - 1) / columns;
        var sheet = new Texture2D(w * columns, h * rows, TextureFormat.RGB24, false);
        for (int i = 0; i < f.Count; i++)
        {
            int cx = (i % columns) * w;
            int cy = (rows - 1 - i / columns) * h;
            sheet.SetPixels(cx, cy, w, h, f[i].GetPixels());
        }
        sheet.Apply();
        System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
        Destroy(sheet);
        return f.Count;
    }

    public static void ClearFrames()
    {
        if (_instance == null) return;
        foreach (var t in _instance._frames) Destroy(t);
        _instance._frames.Clear();
        _instance._counter = 0;
    }
}
#endif

using UnityEngine;

/// <summary>
/// Movement VFX for the player: dust on landing and sprint footsteps, a dust trail while sliding,
/// scuffs on wall runs and a burst on air jumps. Driven by PlayerAudio's events and the parkour state.
/// </summary>
public class PlayerVfx : MonoBehaviour
{
    [SerializeField] private float _slideInterval = 0.06f;
    [SerializeField] private float _wallRunInterval = 0.12f;

    private PlayerAudio _audio;
    private ParkourStateMachine _parkour;
    private CharacterController _controller;
    private float _nextSlideDust;
    private float _nextWallDust;
    private string _lastState = "";

    private void Awake()
    {
        _audio = GetComponent<PlayerAudio>();
        _parkour = GetComponent<ParkourStateMachine>();
        _controller = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (_audio == null) _audio = GetComponent<PlayerAudio>();
        if (_audio == null) return;
        _audio.OnFootstep += OnFootstep;
        _audio.OnLanded += OnLanded;
    }

    private void OnDisable()
    {
        if (_audio == null) return;
        _audio.OnFootstep -= OnFootstep;
        _audio.OnLanded -= OnLanded;
    }

    private static Color Tint(SurfaceType surface)
    {
        switch (surface)
        {
            case SurfaceType.Metal: return new Color(0.7f, 0.72f, 0.75f, 0.35f);
            case SurfaceType.Wood: return new Color(0.55f, 0.42f, 0.28f, 0.4f);
            default: return new Color(0.78f, 0.75f, 0.7f, 0.45f);
        }
    }

    private Vector3 FeetPosition()
    {
        float down = _controller != null ? _controller.height * 0.5f - _controller.center.y : 1f;
        return transform.position + Vector3.down * down;
    }

    private void OnFootstep(Vector3 feet, SurfaceType surface, bool sprinting)
    {
        if (!sprinting) return;
        VfxPool.Instance.SpawnDust(feet, 0.12f, 2, Tint(surface));
    }

    private void OnLanded(Vector3 feet, float intensity)
    {
        VfxPool.Instance.SpawnDust(feet, Mathf.Lerp(0.4f, 1.1f, intensity), Mathf.RoundToInt(Mathf.Lerp(5, 14, intensity)), Tint(SurfaceType.Concrete));
    }

    private void Update()
    {
        if (_parkour == null) return;
        string state = _parkour.CurrentStateName;

        if (state == "SLIDING" && Time.time >= _nextSlideDust)
        {
            _nextSlideDust = Time.time + _slideInterval;
            VfxPool.Instance.SpawnDust(FeetPosition(), 0.18f, 2, Tint(SurfaceType.Concrete));
        }
        else if (state == "WALL RUN" && _parkour.CurrentState is WallRunState wr && Time.time >= _nextWallDust)
        {
            _nextWallDust = Time.time + _wallRunInterval;
            VfxPool.Instance.SpawnDust(wr.WallHit.point + wr.WallHit.normal * 0.05f, 0.1f, 2, Tint(SurfaceType.Concrete));
        }

        if (state != _lastState)
        {
            if (state == "DOUBLE JUMP" || state == "WALL JUMP")
                VfxPool.Instance.SpawnDust(FeetPosition(), 0.5f, 7, Tint(SurfaceType.Concrete));
            _lastState = state;
        }
    }
}

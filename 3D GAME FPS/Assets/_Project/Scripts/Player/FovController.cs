using PrimeTween;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Drives the field of view of the Cinemachine cameras: wider while sprinting / sliding /
/// wall-running and narrower while aiming down sights.
/// Cinemachine overwrites <c>Camera.fieldOfView</c> every frame, so the lens of each virtual camera
/// is modified instead.
/// </summary>
public class FovController : MonoBehaviour
{
    [Header("Movement FOV (multipliers of the base FOV)")]
    [SerializeField] private float _sprintMultiplier = 1.12f;
    [SerializeField] private float _slideMultiplier = 1.2f;
    [SerializeField] private float _wallRunMultiplier = 1.12f;
    [SerializeField] private float _airMultiplier = 1.05f;
    [SerializeField] private float _blendTime = 0.25f;

    [Header("Aim Down Sights")]
    [SerializeField] private float _adsBlendTime = 0.12f;

    private CinemachineCamera[] _vcams;
    private float[] _baseFov;

    private ParkourStateMachine _parkour;
    private WeaponManager _weapons;

    private float _movementMul = 1f;
    private float _adsMul = 1f;
    private float _movementTarget = 1f;
    private float _adsTarget = 1f;
    private Tween _movementTween;
    private Tween _adsTween;

    private void Start()
    {
        _parkour = GetComponent<ParkourStateMachine>();
        _weapons = FindFirstObjectByType<WeaponManager>();

        var modes = GetComponent<CameraModeController>();
        var list = new System.Collections.Generic.List<CinemachineCamera>();
        if (modes != null)
        {
            if (modes.FpsVcam != null) list.Add(modes.FpsVcam);
            if (modes.TpsVcam != null) list.Add(modes.TpsVcam);
        }
        if (list.Count == 0) list.AddRange(FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None));

        _vcams = list.ToArray();
        _baseFov = new float[_vcams.Length];
        for (int i = 0; i < _vcams.Length; i++)
            _baseFov[i] = _vcams[i].Lens.FieldOfView;
    }

    private void OnDisable()
    {
        _movementTween.Stop();
        _adsTween.Stop();
        Apply(1f);
    }

    private void Update()
    {
        UpdateTargets();
        Apply(_movementMul * _adsMul);
    }

    private void UpdateTargets()
    {
        float movement = 1f;
        string state = _parkour != null ? _parkour.CurrentStateName : "";
        switch (state)
        {
            case "SPRINTING": movement = _sprintMultiplier; break;
            case "SLIDING": movement = _slideMultiplier; break;
            case "WALL RUN":
            case "WALL JUMP": movement = _wallRunMultiplier; break;
            case "AIRBORNE":
            case "DOUBLE JUMP": movement = _airMultiplier; break;
        }

        if (!Mathf.Approximately(movement, _movementTarget))
        {
            _movementTarget = movement;
            _movementTween.Stop();
            _movementTween = Tween.Custom(this, _movementMul, movement, _blendTime,
                (c, v) => c._movementMul = v, Ease.OutCubic);
        }

        float ads = 1f;
        var weapon = _weapons != null ? _weapons.CurrentWeapon as HitscanWeapon : null;
        if (weapon != null && weapon.IsAiming && weapon.Data != null)
        {
            // adsFOV is authored against a 60 degree reference FOV.
            ads = Mathf.Clamp(weapon.Data.adsFOV / 60f, 0.3f, 1f);
        }

        if (!Mathf.Approximately(ads, _adsTarget))
        {
            _adsTarget = ads;
            _adsTween.Stop();
            _adsTween = Tween.Custom(this, _adsMul, ads, _adsBlendTime,
                (c, v) => c._adsMul = v, Ease.OutQuad);
        }
    }

    private void Apply(float multiplier)
    {
        if (_vcams == null) return;
        for (int i = 0; i < _vcams.Length; i++)
        {
            if (_vcams[i] == null) continue;
            var lens = _vcams[i].Lens;
            lens.FieldOfView = _baseFov[i] * multiplier;
            _vcams[i].Lens = lens;
        }
    }
}

using UnityEngine;

/// <summary>
/// Procedural first-person weapon motion. It sits on the weapon holder (the parent of the weapon models) and
/// moves the whole weapon with critically damped springs, so nothing snaps:
///  - look sway and tilt, strafe and forward lag
///  - walking and sprinting bob
///  - poses for sprint, slide, wall run, vault and mantle, reload and equip
///  - air float, and a dip on landing that grows with the fall
/// Aiming down the sights calms all of it. Firing pulls the weapon out of the sprint pose at once, so
/// shooting never feels delayed. (WeaponRecoil on each weapon adds the kick of the shot on top.)
/// Runs without allocating memory.
/// </summary>
public class WeaponSway : MonoBehaviour
{
    [Header("Look Sway")]
    [SerializeField] private float _swayAmount = 0.02f;
    [SerializeField] private float _maxSway = 0.06f;
    [SerializeField] private float _smoothSpeed = 8f;
    [SerializeField] private float _tiltAmount = 1.5f;
    [SerializeField] private float _maxTilt = 3.0f;

    [Header("Movement")]
    [SerializeField] private float _bobSpeed = 9f;
    [SerializeField] private Vector2 _bobAmount = new Vector2(0.008f, 0.010f);
    [SerializeField] private float _strafeLag = 0.0035f;
    [SerializeField] private float _forwardLag = 0.0015f;
    [SerializeField] private float _airFloat = 0.0022f;

    [Header("Poses (offset from the rest pose: position metres, rotation degrees)")]
    [SerializeField] private Vector3 _sprintPos = new Vector3(0.04f, -0.03f, -0.01f);
    [SerializeField] private Vector3 _sprintRot = new Vector3(5f, -18f, -6f);
    [SerializeField] private Vector3 _slidePos = new Vector3(0.04f, -0.05f, 0.01f);
    [SerializeField] private Vector3 _slideRot = new Vector3(4f, -6f, -16f);
    [SerializeField] private Vector3 _vaultPos = new Vector3(0f, -0.14f, -0.03f);
    [SerializeField] private Vector3 _vaultRot = new Vector3(26f, 0f, 0f);
    [SerializeField] private Vector3 _reloadPos = new Vector3(-0.09f, 0.025f, 0f);
    [SerializeField] private Vector3 _reloadRot = new Vector3(-8f, 14f, -26f);
    [SerializeField] private float _wallRunRoll = 9f;

    [Header("Events")]
    [SerializeField] private float _equipDrop = 0.17f;
    [SerializeField] private float _equipPitch = 32f;
    [SerializeField] private float _landDip = 0.07f;
    [SerializeField] private float _landPitch = 9f;

    [Header("Feel")]
    [Tooltip("Seconds the weapon takes to settle into a new pose. Lower = snappier.")]
    [SerializeField] private float _poseSmoothTime = 0.085f;
    [Tooltip("How much of the motion is left while aiming down the sights.")]
    [SerializeField] [Range(0f, 1f)] private float _aimMotionScale = 0.15f;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    private PlayerMotor _motor;
    private ParkourStateMachine _parkour;
    private WeaponManager _weapons;
    private PlayerAudio _audio;
    private WeaponBase _subscribed;

    private Vector3 _pos, _posVel;      // current offset and its spring velocity
    private Vector3 _rot, _rotVel;      // current euler offset
    private float _sprintW, _slideW, _wallW, _vaultW, _reloadW, _aimW;
    private float _equip, _land, _bobTimer, _lastFireTime = -10f;

    private void Start()
    {
        _initialPosition = transform.localPosition;
        _initialRotation = transform.localRotation;

        _motor = GetComponentInParent<PlayerMotor>();
        _parkour = GetComponentInParent<ParkourStateMachine>();
        _audio = GetComponentInParent<PlayerAudio>();
        _weapons = GetComponent<WeaponManager>();

        if (_audio != null) _audio.OnLanded += HandleLanded;
        if (_weapons != null)
        {
            _weapons.OnWeaponSwitched += HandleWeaponSwitched;
            Subscribe(_weapons.CurrentWeapon);
        }
    }

    private void OnDestroy()
    {
        if (_audio != null) _audio.OnLanded -= HandleLanded;
        if (_weapons != null) _weapons.OnWeaponSwitched -= HandleWeaponSwitched;
        Unsubscribe();
    }

    private void HandleWeaponSwitched(WeaponBase weapon)
    {
        _equip = 1f;
        Subscribe(weapon);
    }

    private void Subscribe(WeaponBase weapon)
    {
        Unsubscribe();
        _subscribed = weapon;
        if (_subscribed != null) _subscribed.OnFired += HandleFired;
    }

    private void Unsubscribe()
    {
        if (_subscribed != null) _subscribed.OnFired -= HandleFired;
        _subscribed = null;
    }

    private void HandleFired() => _lastFireTime = Time.time;

    private void HandleLanded(Vector3 at, float intensity) => _land = Mathf.Max(_land, Mathf.Clamp01(intensity));

    public void SetInitialPosition(Vector3 pos, Quaternion rot)
    {
        _initialPosition = pos;
        _initialRotation = rot;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        var weapon = _weapons != null ? _weapons.CurrentWeapon : null;
        var hitscan = weapon as HitscanWeapon;
        bool aiming = hitscan != null && hitscan.IsAiming;
        _aimW = Mathf.MoveTowards(_aimW, aiming ? 1f : 0f, dt * 9f);
        float calm = Mathf.Lerp(1f, _aimMotionScale, _aimW);   // 1 at the hip, small while aiming

        // ---- which poses are wanted ----
        string state = _parkour != null ? _parkour.CurrentStateName : "";
        bool firing = Time.time - _lastFireTime < 0.3f;
        bool reloading = weapon != null && weapon.IsReloading;

        float sprintTarget = state == "SPRINTING" && !aiming && !firing && !reloading ? 1f : 0f;
        float slideTarget = state == "SLIDING" ? 1f : 0f;
        float wallTarget = state == "WALL RUN" ? 1f : 0f;
        float vaultTarget = state == "VAULT" || state == "MANTLE" ? 1f : 0f;
        float reloadTarget = reloading ? 1f : 0f;

        _sprintW = Mathf.MoveTowards(_sprintW, sprintTarget, dt * (sprintTarget > 0f ? 5f : 12f));   // leaves fast, enters gently
        _slideW = Mathf.MoveTowards(_slideW, slideTarget, dt * 8f);
        _wallW = Mathf.MoveTowards(_wallW, wallTarget, dt * 6f);
        _vaultW = Mathf.MoveTowards(_vaultW, vaultTarget, dt * 7f);
        _reloadW = Mathf.MoveTowards(_reloadW, reloadTarget, dt * 7f);
        _equip = Mathf.MoveTowards(_equip, 0f, dt * 3.2f);
        _land = Mathf.MoveTowards(_land, 0f, dt * 3.5f);

        // ---- look sway ----
        Vector2 look = InputManager.Instance != null ? InputManager.Instance.LookDelta : Vector2.zero;
        Vector3 pos = new Vector3(
            Mathf.Clamp(-look.x * _swayAmount, -_maxSway, _maxSway),
            Mathf.Clamp(-look.y * _swayAmount, -_maxSway, _maxSway),
            0f);
        Vector3 rot = new Vector3(
            Mathf.Clamp(look.y * _tiltAmount, -_maxTilt, _maxTilt),
            0f,
            Mathf.Clamp(-look.x * _tiltAmount, -_maxTilt, _maxTilt));

        // ---- movement: bob, lag, air ----
        if (_motor != null)
        {
            Vector3 vel = _motor.HorizontalVelocity;
            float speed01 = Mathf.Clamp01(vel.magnitude / Mathf.Max(0.1f, _motor.SprintSpeed));
            Vector3 local = transform.parent != null ? transform.parent.InverseTransformDirection(vel) : vel;

            if (_motor.IsGrounded && state != "SLIDING")
            {
                _bobTimer += dt * _bobSpeed * Mathf.Lerp(0.6f, 1.25f, speed01) * speed01;
                float sprintBoost = Mathf.Lerp(1f, 1.8f, _sprintW);
                pos.x += Mathf.Cos(_bobTimer * 0.5f) * _bobAmount.x * speed01 * sprintBoost;
                pos.y += Mathf.Abs(Mathf.Sin(_bobTimer)) * _bobAmount.y * speed01 * sprintBoost - _bobAmount.y * 0.5f * speed01;
            }

            pos.x -= local.x * _strafeLag;
            pos.z -= local.z * _forwardLag;
            rot.z -= local.x * 0.25f;

            if (!_motor.IsGrounded)
            {
                // weapon trails behind the body: drops when jumping up, floats up when falling
                float vy = Mathf.Clamp(_motor.Velocity.y, -20f, 10f);
                pos.y -= vy * _airFloat;
                rot.x -= vy * 0.35f;
            }
        }

        pos *= calm;
        rot *= calm;

        // ---- poses ----
        pos += _sprintPos * _sprintW + _slidePos * _slideW + _vaultPos * _vaultW + _reloadPos * _reloadW;
        rot += _sprintRot * _sprintW + _slideRot * _slideW + _vaultRot * _vaultW + _reloadRot * _reloadW;

        if (_wallW > 0.001f && _parkour != null && _parkour.CurrentState is WallRunState wr)
            rot.z += (wr.IsLeft ? -1f : 1f) * _wallRunRoll * _wallW;

        // equip: the weapon comes up from below
        float e = _equip * _equip;
        pos.y -= _equipDrop * e;
        rot.x += _equipPitch * e;

        // landing: dips and rolls back up
        pos.y -= _landDip * _land;
        rot.x += _landPitch * _land;

        // ---- spring toward the target pose ----
        float smooth = Mathf.Max(0.01f, _poseSmoothTime);
        _pos = Vector3.SmoothDamp(_pos, pos, ref _posVel, smooth, Mathf.Infinity, dt);
        _rot = Vector3.SmoothDamp(_rot, rot, ref _rotVel, smooth, Mathf.Infinity, dt);

        // Rotate around the weapon itself, not around the holder (which sits at the camera). Otherwise a few
        // degrees of tilt would swing the weapon half a metre sideways and out of view.
        Quaternion tilt = Quaternion.Euler(_rot);
        Vector3 pivot = weapon != null ? weapon.transform.localPosition : Vector3.zero;
        transform.localPosition = _initialPosition + _pos + _initialRotation * (pivot - tilt * pivot);
        transform.localRotation = _initialRotation * tilt;
    }
}

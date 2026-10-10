using UnityEngine;

/// <summary>
/// Procedural body motion layered on top of the animation clips, used by the player and the enemies:
///  - Strafe / backpedal: only forward clips exist, so the legs are turned towards the direction of travel
///    while the torso twists back to keep facing the aim direction.
///  - Lean into turns and acceleration.
///  - Wall-run lean towards the wall.
///  - Landing compression (hips drop, torso pitches forward) that scales with the fall speed.
/// Works through the Animator IK pass (bone rotations / body position), so it never fights the clips.
/// </summary>
public class ProceduralLocomotion : MonoBehaviour, IAnimatorIKHandler
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _root;

    [Header("Strafe")]
    [SerializeField] private float _maxLegYaw = 105f;
    [SerializeField] private float _legTurnSpeed = 9f;
    [SerializeField] private float _minSpeed = 0.35f;

    [Header("Lean")]
    [SerializeField] private float _turnLeanDegrees = 8f;
    [SerializeField] private float _accelerationLeanDegrees = 5f;
    [SerializeField] private float _leanSmoothing = 8f;

    [Header("Landing")]
    [SerializeField] private float _landDrop = 0.28f;
    [SerializeField] private float _landPitch = 22f;
    [SerializeField] private float _landRecover = 7f;

    private ParkourStateMachine _parkour;
    private CharacterController _controller;
    private Quaternion _baseLocalRotation;

    private Vector3 _lastPosition;
    private Vector3 _smoothedVelocity;
    private Vector3 _lastVelocity;
    private float _legYaw;
    private float _leanRoll;
    private float _leanPitch;
    private float _landCompression;
    private bool _wasGrounded = true;
    private bool _suppressed;
    private float _peakFall;

    private Transform _spine, _chest, _upperChest, _hips;

    /// <summary>Extra torso pitch in degrees (positive bends forward), set by other procedural systems such as the slide.</summary>
    public float ExtraPitch { get; set; }
    /// <summary>Extra torso roll in degrees.</summary>
    public float ExtraRoll { get; set; }

    /// <summary>Horizontal direction the legs and hips face (the aim direction turned by the strafe leg yaw).</summary>
    public Vector3 LegFacing
    {
        get
        {
            Vector3 f = Quaternion.AngleAxis(_legYaw, Vector3.up) * _root.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : _root.forward;
        }
    }

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_root == null) _root = transform.parent != null ? transform.parent : transform;
        _baseLocalRotation = transform.localRotation;

        _parkour = _root.GetComponent<ParkourStateMachine>();
        _controller = _root.GetComponent<CharacterController>();
        _lastPosition = _root.position;

        var relay = GetComponent<AnimatorIKRelay>();
        if (relay == null) relay = gameObject.AddComponent<AnimatorIKRelay>();
        relay.AddTarget(this, 10);
    }

    private void Start()
    {
        if (_animator != null && _animator.isHuman)
        {
            _hips = _animator.GetBoneTransform(HumanBodyBones.Hips);
            _spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
            _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
            _upperChest = _animator.GetBoneTransform(HumanBodyBones.UpperChest);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 velocity = (_root.position - _lastPosition) / dt;
        _lastPosition = _root.position;
        velocity.y = 0f;
        // The agent / controller produce stepped movement, so smooth it a little.
        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, velocity, 1f - Mathf.Exp(-14f * dt));

        // Scripted parkour moves (vault / mantle) are fully driven by their own path and clip scrubbing.
        _suppressed = _parkour != null && _parkour.CurrentStateName is "VAULT" or "MANTLE";

        UpdateLegYaw(dt);
        UpdateLean(dt);
        UpdateLanding(dt);

        _lastVelocity = _smoothedVelocity;
    }

    private void UpdateLegYaw(float dt)
    {
        float target = 0f;
        float speed = _smoothedVelocity.magnitude;
        if (speed > _minSpeed && !_suppressed)
        {
            Vector3 local = _root.InverseTransformDirection(_smoothedVelocity);
            float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg; // 0 forward, +90 right, 180 backwards
            // Moving backwards: keep the legs close to the aim direction instead of spinning them 180 degrees.
            if (angle > 135f) angle -= 180f;
            else if (angle < -135f) angle += 180f;
            target = Mathf.Clamp(angle, -_maxLegYaw, _maxLegYaw);
        }

        _legYaw = Mathf.Lerp(_legYaw, target, 1f - Mathf.Exp(-_legTurnSpeed * dt));
    }

    private void UpdateLean(float dt)
    {
        float roll = 0f, pitch = 0f;

        if (!_suppressed)
        {
            Vector3 accel = (_smoothedVelocity - _lastVelocity) / dt;
            Vector3 localAccel = _root.InverseTransformDirection(accel);
            // lean into lateral acceleration (turning) and slightly forward when accelerating
            roll = Mathf.Clamp(-localAccel.x * 0.35f, -1f, 1f) * _turnLeanDegrees;
            pitch = Mathf.Clamp(localAccel.z * 0.15f, -1f, 1f) * _accelerationLeanDegrees;
        }

        _leanRoll = Mathf.Lerp(_leanRoll, roll, 1f - Mathf.Exp(-_leanSmoothing * dt));
        _leanPitch = Mathf.Lerp(_leanPitch, pitch, 1f - Mathf.Exp(-_leanSmoothing * dt));
    }

    private void UpdateLanding(float dt)
    {
        _landCompression = Mathf.Lerp(_landCompression, 0f, 1f - Mathf.Exp(-_landRecover * dt));
        if (_controller == null) return;

        bool grounded = _controller.isGrounded;
        if (!grounded) _peakFall = Mathf.Max(_peakFall, -_controller.velocity.y);

        if (grounded && !_wasGrounded && _peakFall > 3f)
            _landCompression = Mathf.Max(_landCompression, Mathf.InverseLerp(3f, 14f, _peakFall));
        if (grounded) _peakFall = 0f;
        _wasGrounded = grounded;
    }

    private void LateUpdate()
    {
        // Turn the whole model so the legs follow the travel direction.
        transform.localRotation = Quaternion.AngleAxis(_legYaw, Vector3.up) * _baseLocalRotation;
    }

    public void HandleAnimatorIK(int layerIndex)
    {
        if (_animator == null || layerIndex != 0 || !_animator.isHuman) return;

        // Landing: drop the hips
        if (_landCompression > 0.01f)
            _animator.bodyPosition = _animator.bodyPosition + Vector3.down * (_landDrop * _landCompression);

        // Torso: counter-twist the strafe yaw, lean and landing pitch (all in world space around the root axes)
        Quaternion twist = Quaternion.AngleAxis(-_legYaw, Vector3.up);
        Quaternion roll = Quaternion.AngleAxis(_leanRoll + ExtraRoll, _root.forward);
        Quaternion pitch = Quaternion.AngleAxis(_leanPitch + ExtraPitch + _landPitch * _landCompression, _root.right);
        Quaternion lean = roll * pitch;

        // Shares are added up the chain (children inherit the parent's rotation), so they sum to 1.
        Rotate(HumanBodyBones.Spine, _spine, twist, 0.35f, lean, 0.3f);
        Rotate(HumanBodyBones.Chest, _chest, twist, 0.35f, lean, 0.35f);
        if (_upperChest != null)
            Rotate(HumanBodyBones.UpperChest, _upperChest, twist, 0.3f, lean, 0.35f);
    }

    private void Rotate(HumanBodyBones id, Transform bone, Quaternion twist, float twistShare, Quaternion lean, float leanShare)
    {
        if (bone == null || bone.parent == null) return;

        Quaternion world = bone.rotation;
        world = Quaternion.Slerp(Quaternion.identity, twist, twistShare) * world;
        world = Quaternion.Slerp(Quaternion.identity, lean, leanShare) * world;
        _animator.SetBoneLocalRotation(id, Quaternion.Inverse(bone.parent.rotation) * world);
    }
}

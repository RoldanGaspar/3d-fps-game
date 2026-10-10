using UnityEngine;

/// <summary>
/// Procedural poses for the parkour moves that have no good clip of their own. They work on top of a simple
/// base clip through the Animator IK pass (hip position and rotation, foot goals, knee hints), so they adapt
/// to the real ground, wall and speed instead of replaying a fixed animation:
///  - Slide: hips drop to the ground, the body reclines, one leg stretches ahead and the other tucks under.
///  - Wall run: the whole body rolls away from the wall so the feet reach the wall surface.
///  - Double jump: a front flip around the hips.
/// (Vault and mantle are procedural too, see VaultState / MantleState: the capsule follows the real obstacle
/// and the hands are pinned to it.)
/// </summary>
public class ProceduralParkour : MonoBehaviour, IAnimatorIKHandler
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _root;
    [SerializeField] private ProceduralLocomotion _locomotion;

    [Header("Slide")]
    [Tooltip("Height of the hips above the ground while sliding.")]
    [SerializeField] private float _slideHipHeight = 0.27f;
    [Tooltip("How far the whole body reclines (degrees).")]
    [SerializeField] private float _slideLeanBack = 38f;
    [Tooltip("The torso bends back forward by this much so the weapon stays pointed ahead.")]
    [SerializeField] private float _slideTorsoForward = 16f;
    [SerializeField] private float _slideLeadFootReach = 0.92f;
    [SerializeField] private float _slideBlendSpeed = 9f;

    [Header("Wall Run")]
    [Tooltip("Roll away from the wall (degrees). The feet reach the wall because of this.")]
    [SerializeField] private float _wallRunRoll = 26f;
    [SerializeField] private float _wallRunBlendSpeed = 8f;

    [Header("Double Jump")]
    [SerializeField] private float _flipDuration = 0.55f;

    private ParkourStateMachine _parkour;
    private float _slideWeight;
    private float _wallWeight;
    private bool _wallOnLeft;
    private Vector3 _wallNormal;
    private float _flipTime = -1f;
    private float _groundY;
    private int _groundMask;

    private void Awake()
    {
        if (_animator == null) _animator = GetComponent<Animator>();
        if (_root == null) _root = transform.parent != null ? transform.parent : transform;
        if (_locomotion == null) _locomotion = GetComponent<ProceduralLocomotion>();
        _parkour = _root.GetComponent<ParkourStateMachine>();
        _groundY = _root.position.y - 1f;
        // only real level geometry: the ragdoll bone colliders on this character would otherwise be hit first
        _groundMask = LayerMask.GetMask("Ground", "Obstacle");

        var relay = GetComponent<AnimatorIKRelay>();
        if (relay == null) relay = gameObject.AddComponent<AnimatorIKRelay>();
        relay.AddTarget(this, 20);
    }

    private void OnEnable()
    {
        if (_parkour != null) _parkour.DoubleJumped += StartFlip;
    }

    private void OnDisable()
    {
        if (_parkour != null) _parkour.DoubleJumped -= StartFlip;
    }

    public void StartFlip() => _flipTime = 0f;

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private void Update()
    {
        float dt = Time.deltaTime;
        var state = _parkour != null ? _parkour.CurrentState : null;

        _slideWeight = Mathf.MoveTowards(_slideWeight, state is SlidingState ? 1f : 0f, dt * _slideBlendSpeed);

        bool onWall = state is WallRunState;
        if (state is WallRunState wr)
        {
            _wallOnLeft = wr.IsLeft;
            _wallNormal = wr.WallHit.normal;
        }
        _wallWeight = Mathf.MoveTowards(_wallWeight, onWall ? 1f : 0f, dt * _wallRunBlendSpeed);

        if (_flipTime >= 0f)
        {
            _flipTime += dt;
            if (_flipTime >= _flipDuration) _flipTime = -1f;
        }

        // keep the weapon pointing ahead while the body reclines in the slide
        if (_locomotion != null) _locomotion.ExtraPitch = Smooth(_slideWeight) * _slideTorsoForward;

        if (_slideWeight > 0.001f && Physics.Raycast(_root.position, Vector3.down, out RaycastHit hit, 3f, _groundMask, QueryTriggerInteraction.Ignore))
            _groundY = hit.point.y;
    }

    public void HandleAnimatorIK(int layerIndex)
    {
        if (_animator == null || layerIndex != 0 || !_animator.isHuman) return;

        Vector3 facing = _locomotion != null ? _locomotion.LegFacing : _root.forward;
        Vector3 right = Vector3.Cross(Vector3.up, facing);

        Vector3 bodyPos = _animator.bodyPosition;
        Quaternion bodyRot = _animator.bodyRotation;
        Vector3 startPos = bodyPos;
        Quaternion startRot = bodyRot;

        float slide = Smooth(_slideWeight);
        if (slide > 0.001f) ApplySlide(slide, facing, right, ref bodyPos, ref bodyRot);

        float wall = Smooth(_wallWeight);
        if (wall > 0.001f)
        {
            // roll away from the wall: head away, feet towards the wall
            float roll = (_wallOnLeft ? -1f : 1f) * _wallRunRoll * wall;
            bodyRot = Quaternion.AngleAxis(roll, facing) * bodyRot;
            Vector3 toWall = -Vector3.ProjectOnPlane(_wallNormal, Vector3.up).normalized;
            bodyPos += toWall * (0.06f * wall);
        }

        if (_flipTime >= 0f)
        {
            float u = Mathf.Clamp01(_flipTime / _flipDuration);
            bodyRot = Quaternion.AngleAxis(360f * Smooth(u), right) * bodyRot;
        }

        if (slide > 0.001f || wall > 0.001f || _flipTime >= 0f)
        {
            _animator.bodyPosition = bodyPos;
            _animator.bodyRotation = bodyRot;

            // The hand goal on the weapon was computed before the body moved. Carry it along with the body,
            // otherwise the support hand stays behind in mid-air while the weapon goes with the other hand.
            if (_animator.GetIKPositionWeight(AvatarIKGoal.LeftHand) > 0.001f)
            {
                Quaternion delta = bodyRot * Quaternion.Inverse(startRot);
                Vector3 goal = _animator.GetIKPosition(AvatarIKGoal.LeftHand);
                _animator.SetIKPosition(AvatarIKGoal.LeftHand, bodyPos + delta * (goal - startPos));
            }
        }
    }

    private void ApplySlide(float w, Vector3 facing, Vector3 right, ref Vector3 bodyPos, ref Quaternion bodyRot)
    {
        float g = _groundY;

        // hips down to the ground, a little behind the capsule centre, body reclined
        Vector3 hipsTarget = new Vector3(bodyPos.x, g + _slideHipHeight, bodyPos.z) - facing * 0.05f;
        bodyPos = Vector3.Lerp(bodyPos, hipsTarget, w);
        bodyRot = Quaternion.AngleAxis(-_slideLeanBack * w, right) * bodyRot;

        Vector3 hips = new Vector3(hipsTarget.x, 0f, hipsTarget.z);

        // lead leg: stretched out in front, just above the ground
        Vector3 leadFoot = hips + facing * _slideLeadFootReach + right * 0.10f;
        leadFoot.y = g + 0.09f;
        Vector3 leadKnee = hips + facing * 0.50f + right * 0.14f;
        leadKnee.y = g + 0.30f;

        // trail leg: folded under the body, knee out to the side
        Vector3 trailFoot = hips + facing * 0.12f - right * 0.26f;
        trailFoot.y = g + 0.12f;
        Vector3 trailKnee = hips + facing * 0.42f - right * 0.55f;
        trailKnee.y = g + 0.42f;

        SetFoot(AvatarIKGoal.RightFoot, AvatarIKHint.RightKnee, leadFoot, leadKnee, w);
        SetFoot(AvatarIKGoal.LeftFoot, AvatarIKHint.LeftKnee, trailFoot, trailKnee, w);
    }

    private void SetFoot(AvatarIKGoal goal, AvatarIKHint hint, Vector3 foot, Vector3 knee, float w)
    {
        _animator.SetIKPositionWeight(goal, w);
        _animator.SetIKPosition(goal, foot);
        _animator.SetIKHintPositionWeight(hint, w);
        _animator.SetIKHintPosition(hint, knee);
    }
}

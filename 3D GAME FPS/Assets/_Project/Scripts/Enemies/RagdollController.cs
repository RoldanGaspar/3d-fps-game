using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physics ragdoll for a humanoid character. The rigid bodies, colliders and joints are created once
/// (see <see cref="RagdollBuilder"/>), start disabled, and are switched on when the character dies.
/// A global limit keeps the number of simultaneously active ragdolls low for mobile.
/// </summary>
public class RagdollController : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private Rigidbody[] _bodies;
    [SerializeField] private Collider[] _colliders;

    private static int _activeRagdolls;
    public static int MaxActiveRagdolls = 6;

    private bool _active;

    public bool IsActive => _active;
    public bool IsBuilt => _bodies != null && _bodies.Length > 0;

    public static bool HasCapacity => _activeRagdolls < MaxActiveRagdolls;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _activeRagdolls = 0;
    }

    [SerializeField] private Vector3[] _restPositions;
    [SerializeField] private Quaternion[] _restRotations;

    public void Setup(Animator animator, Rigidbody[] bodies, Collider[] colliders)
    {
        _animator = animator;
        _bodies = bodies;
        _colliders = colliders;

        // Remember the rest pose of every physics bone so the character can be brought back (revive).
        _restPositions = new Vector3[bodies.Length];
        _restRotations = new Quaternion[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            _restPositions[i] = bodies[i].transform.localPosition;
            _restRotations[i] = bodies[i].transform.localRotation;
        }
        SetPhysics(false);
    }

    /// <summary>Turns the physics off again and returns the skeleton to its animated pose.</summary>
    public void Deactivate()
    {
        if (!_active) return;
        _active = false;
        _activeRagdolls = Mathf.Max(0, _activeRagdolls - 1);

        SetPhysics(false);
        if (_restPositions != null)
        {
            for (int i = 0; i < _bodies.Length && i < _restPositions.Length; i++)
            {
                if (_bodies[i] == null) continue;
                _bodies[i].transform.localPosition = _restPositions[i];
                _bodies[i].transform.localRotation = _restRotations[i];
            }
        }
        if (_animator != null) _animator.enabled = true;
    }

    private void Awake()
    {
        if (IsBuilt) SetPhysics(false);
    }

    private void OnDestroy()
    {
        if (_active) _activeRagdolls = Mathf.Max(0, _activeRagdolls - 1);
    }

    /// <summary>Switches the character to physics and pushes it away from the hit.</summary>
    public void Activate(Vector3 hitPoint, Vector3 impulse, Collider[] ignoreCollisionWith = null)
    {
        if (_active || !IsBuilt) return;
        _active = true;
        _activeRagdolls++;

        // Keep the bone pose from the animation, then hand control to physics.
        if (_animator != null) _animator.enabled = false;
        SetPhysics(true);

        if (ignoreCollisionWith != null)
        {
            foreach (var other in ignoreCollisionWith)
            {
                if (other == null) continue;
                foreach (var c in _colliders)
                    if (c != null) Physics.IgnoreCollision(c, other, true);
            }
        }

        // Give every body a little of the hit velocity, with the biggest push near the impact.
        Rigidbody nearest = null;
        float best = float.MaxValue;
        foreach (var rb in _bodies)
        {
            if (rb == null) continue;
            float d = (rb.worldCenterOfMass - hitPoint).sqrMagnitude;
            if (d < best) { best = d; nearest = rb; }
        }

        foreach (var rb in _bodies)
        {
            if (rb == null) continue;
            rb.AddForce(impulse * 0.25f, ForceMode.Impulse);
        }
        if (nearest != null) nearest.AddForceAtPosition(impulse, hitPoint, ForceMode.Impulse);
    }

    private void SetPhysics(bool on)
    {
        if (_bodies != null)
        {
            foreach (var rb in _bodies)
            {
                if (rb == null) continue;
                rb.isKinematic = !on;
                rb.detectCollisions = on;
            }
        }
        if (_colliders != null)
        {
            foreach (var c in _colliders)
                if (c != null) c.enabled = on;
        }
    }
}

/// <summary>
/// Builds a ragdoll (capsule / box / sphere colliders, rigid bodies and character joints) on a humanoid rig.
/// All sizes are given in world metres, so it works with any bone scale.
/// </summary>
public static class RagdollBuilder
{
    private struct Part
    {
        public HumanBodyBones bone;
        public HumanBodyBones? child;     // bone the capsule points towards (null = box / sphere)
        public HumanBodyBones? parent;    // bone it is jointed to
        public float radius;
        public float mass;
        public bool sphere;
        public Vector3 boxSize;
        public float swing1, swing2, twistLow, twistHigh;
    }

    public static RagdollController Build(GameObject root, Animator animator)
    {
        if (animator == null || !animator.isHuman) return null;

        var parts = new List<Part>
        {
            new Part { bone = HumanBodyBones.Hips, parent = null, boxSize = new Vector3(0.30f, 0.20f, 0.20f), mass = 12f },
            new Part { bone = HumanBodyBones.Spine, parent = HumanBodyBones.Hips, child = HumanBodyBones.Chest, radius = 0.13f, mass = 8f, swing1 = 25, swing2 = 20, twistLow = -20, twistHigh = 20 },
            new Part { bone = HumanBodyBones.Chest, parent = HumanBodyBones.Spine, child = HumanBodyBones.Head, radius = 0.15f, mass = 14f, swing1 = 25, swing2 = 20, twistLow = -20, twistHigh = 20 },
            new Part { bone = HumanBodyBones.Head, parent = HumanBodyBones.Chest, sphere = true, radius = 0.11f, mass = 4f, swing1 = 40, swing2 = 40, twistLow = -30, twistHigh = 30 },

            new Part { bone = HumanBodyBones.LeftUpperArm, parent = HumanBodyBones.Chest, child = HumanBodyBones.LeftLowerArm, radius = 0.05f, mass = 2f, swing1 = 80, swing2 = 60, twistLow = -30, twistHigh = 30 },
            new Part { bone = HumanBodyBones.LeftLowerArm, parent = HumanBodyBones.LeftUpperArm, child = HumanBodyBones.LeftHand, radius = 0.04f, mass = 1.5f, swing1 = 70, swing2 = 10, twistLow = -20, twistHigh = 20 },
            new Part { bone = HumanBodyBones.RightUpperArm, parent = HumanBodyBones.Chest, child = HumanBodyBones.RightLowerArm, radius = 0.05f, mass = 2f, swing1 = 80, swing2 = 60, twistLow = -30, twistHigh = 30 },
            new Part { bone = HumanBodyBones.RightLowerArm, parent = HumanBodyBones.RightUpperArm, child = HumanBodyBones.RightHand, radius = 0.04f, mass = 1.5f, swing1 = 70, swing2 = 10, twistLow = -20, twistHigh = 20 },

            new Part { bone = HumanBodyBones.LeftUpperLeg, parent = HumanBodyBones.Hips, child = HumanBodyBones.LeftLowerLeg, radius = 0.08f, mass = 7f, swing1 = 70, swing2 = 30, twistLow = -20, twistHigh = 20 },
            new Part { bone = HumanBodyBones.LeftLowerLeg, parent = HumanBodyBones.LeftUpperLeg, child = HumanBodyBones.LeftFoot, radius = 0.06f, mass = 4f, swing1 = 80, swing2 = 5, twistLow = -10, twistHigh = 10 },
            new Part { bone = HumanBodyBones.RightUpperLeg, parent = HumanBodyBones.Hips, child = HumanBodyBones.RightLowerLeg, radius = 0.08f, mass = 7f, swing1 = 70, swing2 = 30, twistLow = -20, twistHigh = 20 },
            new Part { bone = HumanBodyBones.RightLowerLeg, parent = HumanBodyBones.RightUpperLeg, child = HumanBodyBones.RightFoot, radius = 0.06f, mass = 4f, swing1 = 80, swing2 = 5, twistLow = -10, twistHigh = 10 },
        };

        Vector3 up = Vector3.up;
        Vector3 sideways = root.transform.right;

        var bodies = new Dictionary<HumanBodyBones, Rigidbody>();
        var allBodies = new List<Rigidbody>();
        var allColliders = new List<Collider>();

        foreach (var p in parts)
        {
            var t = animator.GetBoneTransform(p.bone);
            if (t == null) continue;

            var rb = t.GetComponent<Rigidbody>();
            if (rb == null) rb = t.gameObject.AddComponent<Rigidbody>();
            rb.mass = p.mass;
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.6f;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            bodies[p.bone] = rb;
            allBodies.Add(rb);

            float scale = Mathf.Max(0.0001f, t.lossyScale.x);

            Collider col;
            if (p.child.HasValue)
            {
                var childT = animator.GetBoneTransform(p.child.Value);
                Vector3 a = t.position;
                Vector3 b = childT != null ? childT.position : t.position + up * 0.2f;
                Vector3 dirWorld = (b - a);
                float length = dirWorld.magnitude;
                dirWorld = length > 0.0001f ? dirWorld / length : up;

                var cap = t.gameObject.AddComponent<CapsuleCollider>();
                Vector3 localDir = t.InverseTransformDirection(dirWorld);
                int axis = 0;
                float ax = Mathf.Abs(localDir.x), ay = Mathf.Abs(localDir.y), az = Mathf.Abs(localDir.z);
                if (ay >= ax && ay >= az) axis = 1; else if (az >= ax && az >= ay) axis = 2;
                cap.direction = axis;
                cap.radius = p.radius / scale;
                cap.height = Mathf.Max(length, p.radius * 2f) / scale;
                cap.center = t.InverseTransformPoint((a + b) * 0.5f);
                col = cap;
            }
            else if (p.sphere)
            {
                var sph = t.gameObject.AddComponent<SphereCollider>();
                sph.radius = p.radius / scale;
                sph.center = t.InverseTransformPoint(t.position + up * p.radius * 1.0f);
                col = sph;
            }
            else
            {
                var box = t.gameObject.AddComponent<BoxCollider>();
                box.size = p.boxSize / scale;
                box.center = Vector3.zero;
                col = box;
            }
            allColliders.Add(col);
        }

        // Joints
        foreach (var p in parts)
        {
            if (!p.parent.HasValue) continue;
            if (!bodies.TryGetValue(p.bone, out var rb) || !bodies.TryGetValue(p.parent.Value, out var parentRb)) continue;

            var t = rb.transform;
            var joint = t.GetComponent<CharacterJoint>();
            if (joint == null) joint = t.gameObject.AddComponent<CharacterJoint>();
            joint.connectedBody = parentRb;
            joint.enablePreprocessing = false;
            joint.enableProjection = true;
            joint.enableCollision = false;

            // Twist along the bone, swing around the character's sideways axis.
            Vector3 boneDir = up;
            if (p.child.HasValue)
            {
                var childT = animator.GetBoneTransform(p.child.Value);
                if (childT != null) boneDir = (childT.position - t.position).normalized;
            }
            joint.axis = t.InverseTransformDirection(boneDir);
            joint.swingAxis = t.InverseTransformDirection(Vector3.Cross(boneDir, sideways).sqrMagnitude > 0.01f ? sideways : root.transform.forward);

            joint.lowTwistLimit = new SoftJointLimit { limit = p.twistLow };
            joint.highTwistLimit = new SoftJointLimit { limit = p.twistHigh };
            joint.swing1Limit = new SoftJointLimit { limit = p.swing1 };
            joint.swing2Limit = new SoftJointLimit { limit = p.swing2 };
        }

        var controller = root.GetComponent<RagdollController>();
        if (controller == null) controller = root.AddComponent<RagdollController>();
        controller.Setup(animator, allBodies.ToArray(), allColliders.ToArray());
        return controller;
    }
}

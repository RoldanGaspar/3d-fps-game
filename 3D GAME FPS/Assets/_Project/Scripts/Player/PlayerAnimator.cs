using UnityEngine;

/// <summary>
/// Drives character Animator from PlayerMotor velocity, ParkourStateMachine state,
/// WeaponManager actions, and PlayerHealth events.
/// Supports both FPS and TPS modes.
/// </summary>
public class PlayerAnimator : MonoBehaviour, IAnimatorIKHandler
{
    [Header("Core References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private ParkourStateMachine _parkourStateMachine;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private WeaponManager _weaponManager;
    [SerializeField] private PlayerHealth _playerHealth;

    [Header("Third Person Weapon Visuals")]
    [SerializeField] private Transform _tpRifleVisual;
    [SerializeField] private Transform _tpPistolVisual;

    [Header("Smoothing")]
    [SerializeField] private float _locomotionDampTime = 0.1f;
    [SerializeField] private float _turnDampTime = 0.15f;

    [Header("Landing")]
    [SerializeField] private float _landMinAirTime = 0.4f;
    [SerializeField] private float _landMinFallSpeed = 6f;

    [Header("Foot IK")]
    [SerializeField] private bool _enableFootIK = true;
    [SerializeField] private float _footHeight = 0.1f;
    [SerializeField] private float _footRayDistance = 1.5f;
    [SerializeField] private float _footIKBlendSpeed = 8f;
    [SerializeField] private LayerMask _footGroundMask = ~0;

    [Header("Hand IK")]
    [SerializeField] private Transform _leftHandGrip;

    [Header("IK Settings")]
    [SerializeField] private bool _enableLookIK = true;
    [SerializeField] [Range(0f, 1f)] private float _lookIKWeight = 0.6f;
    [SerializeField] private float _lookIKSmoothSpeed = 5f;
    [SerializeField] private float _lookIKMaxAngle = 70f;

    // Animator parameter hashes (cached for zero-allocation performance)
    private static readonly int Hash_Speed = Animator.StringToHash("Speed");
    private static readonly int Hash_MoveX = Animator.StringToHash("MoveX");
    private static readonly int Hash_MoveZ = Animator.StringToHash("MoveZ");
    private static readonly int Hash_VerticalVelocity = Animator.StringToHash("VerticalVelocity");
    private static readonly int Hash_IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int Hash_IsAirborne = Animator.StringToHash("IsAirborne");
    private static readonly int Hash_IsCrouching = Animator.StringToHash("IsCrouching");
    private static readonly int Hash_IsSprinting = Animator.StringToHash("IsSprinting");
    private static readonly int Hash_IsSliding = Animator.StringToHash("IsSliding");
    private static readonly int Hash_IsWallRunning = Animator.StringToHash("IsWallRunning");
    private static readonly int Hash_WallRunSide = Animator.StringToHash("WallRunSide");

    private static readonly int Hash_Jump = Animator.StringToHash("Jump");
    private static readonly int Hash_DoubleJump = Animator.StringToHash("DoubleJump");
    private static readonly int Hash_Vault = Animator.StringToHash("Vault");
    private static readonly int Hash_Mantle = Animator.StringToHash("Mantle");
    private static readonly int Hash_Land = Animator.StringToHash("Land");

    private static readonly int Hash_WeaponType = Animator.StringToHash("WeaponType");
    private static readonly int Hash_IsAiming = Animator.StringToHash("IsAiming");
    private static readonly int Hash_Fire = Animator.StringToHash("Fire");
    private static readonly int Hash_Reload = Animator.StringToHash("Reload");
    private static readonly int Hash_Hit = Animator.StringToHash("Hit");
    private static readonly int Hash_IsDead = Animator.StringToHash("IsDead");

    // IK
    private Vector3 _lookIKTarget;
    private float _currentIKWeight;

    // State tracking
    private string _previousState;
    private bool _wasGrounded = true;
    private float _nextHitReactionTime;
    private float _footIKWeight;
    private float _leftHandIKWeight;
    private float _airTime;
    private float _peakFallSpeed;
    private WeaponBase _subscribedWeapon;

    private void Start()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        // Movement is driven by CharacterController; root motion would lift/shift the model off the capsule.
        if (_animator != null)
        {
            _animator.applyRootMotion = false;
            // OnAnimatorIK is only sent to components on the Animator's own GameObject.
            var relay = _animator.GetComponent<AnimatorIKRelay>();
            if (relay == null) relay = _animator.gameObject.AddComponent<AnimatorIKRelay>();
            relay.SetTarget(this);
        }
        if (_motor == null)
            _motor = GetComponentInParent<PlayerMotor>();
        if (_parkourStateMachine == null)
            _parkourStateMachine = GetComponentInParent<ParkourStateMachine>();
        if (_characterController == null)
            _characterController = GetComponentInParent<CharacterController>();
        if (_weaponManager == null)
            _weaponManager = FindFirstObjectByType<WeaponManager>();
        if (_playerHealth == null)
            _playerHealth = GetComponentInParent<PlayerHealth>();

        // Wire Health events
        if (_playerHealth != null)
        {
            _playerHealth.OnDamaged.AddListener(TriggerHit);
            _playerHealth.OnDeath.AddListener(() => SetDead(true));
        }

        // Wire WeaponManager events
        if (_weaponManager != null)
        {
            _weaponManager.OnWeaponSwitched += HandleWeaponSwitched;
            if (_weaponManager.CurrentWeapon != null)
            {
                HandleWeaponSwitched(_weaponManager.CurrentWeapon);
            }
        }
    }

    private void OnDestroy()
    {
        if (_subscribedWeapon != null)
        {
            _subscribedWeapon.OnFired -= TriggerFire;
            _subscribedWeapon.OnReloadStarted -= TriggerReload;
        }

        if (_weaponManager != null)
        {
            _weaponManager.OnWeaponSwitched -= HandleWeaponSwitched;
        }
    }

    private void HandleWeaponSwitched(WeaponBase newWeapon)
    {
        if (_subscribedWeapon != null)
        {
            _subscribedWeapon.OnFired -= TriggerFire;
            _subscribedWeapon.OnReloadStarted -= TriggerReload;
        }

        _subscribedWeapon = newWeapon;

        if (_subscribedWeapon != null)
        {
            _subscribedWeapon.OnFired += TriggerFire;
            _subscribedWeapon.OnReloadStarted += TriggerReload;
        }

        UpdateWeaponVisuals();
    }

    private void Update()
    {
        if (_animator == null || !_animator.isActiveAndEnabled) return;

        UpdateLocomotion();
        UpdateParkourState();
        UpdateLanding();
        UpdateCombat();
    }

    private void UpdateLocomotion()
    {
        Vector3 velocity = _characterController != null
            ? _characterController.velocity
            : Vector3.zero;

        Vector3 localVel = transform.InverseTransformDirection(velocity);
        float speed = new Vector2(localVel.x, localVel.z).magnitude;

        _animator.SetFloat(Hash_Speed, speed, _locomotionDampTime, Time.deltaTime);
        _animator.SetFloat(Hash_MoveX, localVel.x, _locomotionDampTime, Time.deltaTime);
        _animator.SetFloat(Hash_MoveZ, localVel.z, _locomotionDampTime, Time.deltaTime);
        _animator.SetFloat(Hash_VerticalVelocity, velocity.y);

        bool grounded = _motor != null ? _motor.IsGrounded : (_characterController != null && _characterController.isGrounded);
        _animator.SetBool(Hash_IsGrounded, grounded);
        _animator.SetBool(Hash_IsAirborne, !grounded);
    }

    private void UpdateParkourState()
    {
        if (_parkourStateMachine == null) return;

        string currentState = _parkourStateMachine.CurrentStateName;

        if (currentState != _previousState)
        {
            _animator.SetBool(Hash_IsCrouching, false);
            _animator.SetBool(Hash_IsSprinting, false);
            _animator.SetBool(Hash_IsSliding, false);
            _animator.SetBool(Hash_IsWallRunning, false);

            switch (currentState)
            {
                // Names must match ParkourState.StateName of each state class.
                case "SPRINTING":
                    _animator.SetBool(Hash_IsSprinting, true);
                    break;
                case "SLIDING":
                    _animator.SetBool(Hash_IsSliding, true);
                    break;
                case "CROUCH":
                    _animator.SetBool(Hash_IsCrouching, true);
                    break;
                case "WALL RUN":
                    _animator.SetBool(Hash_IsWallRunning, true);
                    break;
                case "DOUBLE JUMP":
                    _animator.SetTrigger(Hash_DoubleJump);
                    break;
                case "VAULT":
                    _animator.SetTrigger(Hash_Vault);
                    break;
                case "MANTLE":
                    _animator.SetTrigger(Hash_Mantle);
                    break;
            }

            _previousState = currentState;
        }
    }

    private void UpdateLanding()
    {
        bool grounded = _motor != null ? _motor.IsGrounded : (_characterController != null && _characterController.isGrounded);

        if (_wasGrounded && !grounded && _characterController != null && _characterController.velocity.y > 1.5f)
            _animator.SetTrigger(Hash_Jump);

        if (!grounded)
        {
            _airTime += Time.deltaTime;
            if (_characterController != null)
                _peakFallSpeed = Mathf.Max(_peakFallSpeed, -_characterController.velocity.y);
        }
        else if (!_wasGrounded)
        {
            // Only play the hard-landing clip after a real fall. Spawn frames and short hops
            // would otherwise leave a stale "Land" trigger that plays the 2.5s clip later.
            bool realFall = _airTime >= _landMinAirTime && _peakFallSpeed >= _landMinFallSpeed;
            if (realFall) _animator.SetTrigger(Hash_Land);
            else _animator.ResetTrigger(Hash_Land);

            _airTime = 0f;
            _peakFallSpeed = 0f;
        }
        else
        {
            _airTime = 0f;
            _peakFallSpeed = 0f;
        }

        _wasGrounded = grounded;
    }

    private void UpdateCombat()
    {
        if (_weaponManager == null) return;

        int weaponType = 0;
        int activeIdx = _weaponManager.CurrentWeaponIndex;
        if (activeIdx == 0) weaponType = 1;      // Rifle
        else if (activeIdx == 1) weaponType = 2; // Pistol

        _animator.SetInteger(Hash_WeaponType, weaponType);

        var hitscan = _weaponManager.CurrentWeapon as HitscanWeapon;
        if (hitscan != null)
        {
            _animator.SetBool(Hash_IsAiming, hitscan.IsAiming);
        }

        UpdateWeaponVisuals();
    }

    public void UpdateWeaponVisuals()
    {
        if (_weaponManager == null) return;

        int activeIdx = _weaponManager.CurrentWeaponIndex;
        if (_tpRifleVisual != null) _tpRifleVisual.gameObject.SetActive(activeIdx == 0);
        if (_tpPistolVisual != null) _tpPistolVisual.gameObject.SetActive(activeIdx == 1);
    }

    // --- Public API ---

    public void TriggerJump()
    {
        if (_animator != null)
            _animator.SetTrigger(Hash_Jump);
    }

    public void TriggerFire()
    {
        if (_animator != null)
            _animator.SetTrigger(Hash_Fire);
    }

    public void TriggerReload()
    {
        if (_animator != null)
            _animator.SetTrigger(Hash_Reload);
    }

    public void TriggerHit()
    {
        // The hit-reaction clip is long; don't restart it on every bullet.
        if (_animator == null || Time.time < _nextHitReactionTime) return;
        _nextHitReactionTime = Time.time + 0.8f;
        _animator.SetTrigger(Hash_Hit);
    }

    public void SetDead(bool isDead)
    {
        if (_animator != null)
            _animator.SetBool(Hash_IsDead, isDead);
    }

    public void SetAiming(bool aiming)
    {
        if (_animator != null)
            _animator.SetBool(Hash_IsAiming, aiming);
    }

    public void SetWallRunSide(float side)
    {
        if (_animator != null)
            _animator.SetFloat(Hash_WallRunSide, side);
    }

    // --- Procedural parkour ---

    private static readonly int Hash_ParkourTime = Animator.StringToHash("ParkourTime");
    private static readonly int Hash_ParkourActive = Animator.StringToHash("ParkourActive");

    private bool _parkourActive;
    private Vector3 _parkourLeftHand;
    private Vector3 _parkourRightHand;
    private float _parkourHandWeight;

    /// <summary>Starts a scrubbed parkour clip (vault / mantle). The caller drives the clip time.</summary>
    public void BeginParkourMove(float clipTime)
    {
        _parkourActive = true;
        _parkourHandWeight = 0f;
        if (_animator == null) return;
        _animator.SetFloat(Hash_ParkourTime, clipTime);
        _animator.SetBool(Hash_ParkourActive, true);
    }

    public void SetParkourClipTime(float clipTime)
    {
        if (_animator != null) _animator.SetFloat(Hash_ParkourTime, clipTime);
    }

    /// <summary>World-space hand targets (e.g. the ledge) and how strongly the hands are pulled to them.</summary>
    public void SetParkourHands(Vector3 left, Vector3 right, float weight)
    {
        _parkourLeftHand = left;
        _parkourRightHand = right;
        _parkourHandWeight = Mathf.Clamp01(weight);
    }

    public void EndParkourMove()
    {
        _parkourActive = false;
        _parkourHandWeight = 0f;
        if (_animator != null) _animator.SetBool(Hash_ParkourActive, false);
    }

    private void ApplyParkourHandsIK()
    {
        if (!_parkourActive || _parkourHandWeight <= 0.001f) return;

        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _parkourHandWeight);
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, _parkourLeftHand);
        _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _parkourHandWeight);
        _animator.SetIKPosition(AvatarIKGoal.RightHand, _parkourRightHand);
    }

    // --- IK ---

    public void HandleAnimatorIK(int layerIndex)
    {
        if (_animator == null) return;
        if (layerIndex != 0) return;

        if (_enableFootIK && !_parkourActive) ApplyFootIK();
        if (_parkourActive) _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
        else ApplyLeftHandGripIK();
        ApplyParkourHandsIK();
        if (_enableLookIK) ApplyLookIK();
    }

    /// <summary>
    /// The humanoid rig leaves one foot hovering in idle/crouch poses, so plant both feet on the
    /// ground while standing still. Weight fades out when moving so walk/run cycles are untouched.
    /// </summary>
    private void ApplyFootIK()
    {
        bool grounded = _motor != null ? _motor.IsGrounded : (_characterController != null && _characterController.isGrounded);
        bool still = _animator.GetFloat(Hash_Speed) < 0.15f && !_animator.GetBool(Hash_IsSliding);
        float target = grounded && still ? 1f : 0f;
        _footIKWeight = Mathf.MoveTowards(_footIKWeight, target, Time.deltaTime * _footIKBlendSpeed);
        if (_footIKWeight <= 0.001f)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0f);
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0f);
            return;
        }

        PlantFoot(AvatarIKGoal.LeftFoot);
        PlantFoot(AvatarIKGoal.RightFoot);
    }

    private void PlantFoot(AvatarIKGoal goal)
    {
        Vector3 foot = _animator.GetIKPosition(goal);
        Vector3 origin = new Vector3(foot.x, transform.position.y, foot.z);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, _footRayDistance, _footGroundMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 planted = hit.point + Vector3.up * _footHeight;
            // Only ever pull a hovering foot down; never push the foot into the ground.
            if (planted.y < foot.y)
                foot.y = planted.y;
        }

        _animator.SetIKPositionWeight(goal, _footIKWeight);
        _animator.SetIKPosition(goal, foot);
    }

    /// <summary>
    /// Moves the free hand onto the rifle foregrip so the weapon is held with both hands.
    /// </summary>
    private void ApplyLeftHandGripIK()
    {
        if (_leftHandGrip == null && _tpRifleVisual != null)
            _leftHandGrip = _tpRifleVisual.Find("LeftHandGrip");

        bool holdingRifle = _leftHandGrip != null
            && _tpRifleVisual != null
            && _tpRifleVisual.gameObject.activeInHierarchy
            && _animator.GetInteger(Hash_WeaponType) == 1
            && !_animator.GetCurrentAnimatorStateInfo(1).IsName("Rifle_Reload")
            && !_animator.IsInTransition(1);

        _leftHandIKWeight = Mathf.MoveTowards(_leftHandIKWeight, holdingRifle ? 1f : 0f, Time.deltaTime * 6f);
        if (_leftHandIKWeight <= 0.001f)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            return;
        }

        _animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _leftHandIKWeight);
        _animator.SetIKPosition(AvatarIKGoal.LeftHand, _leftHandGrip.position);
    }

    private void ApplyLookIK()
    {
        Camera activeCam = Camera.main;
        if (activeCam != null)
        {
            Vector3 desiredTarget = activeCam.transform.position + activeCam.transform.forward * 20f;
            var headBone = _animator.GetBoneTransform(HumanBodyBones.Head);
            if (headBone != null)
            {
                Vector3 toTarget = (desiredTarget - headBone.position).normalized;
                float angle = Vector3.Angle(transform.forward, toTarget);

                float targetWeight = angle < _lookIKMaxAngle ? _lookIKWeight : 0f;
                _currentIKWeight = Mathf.Lerp(_currentIKWeight, targetWeight,
                                               Time.deltaTime * _lookIKSmoothSpeed);
                _lookIKTarget = Vector3.Lerp(_lookIKTarget, desiredTarget,
                                              Time.deltaTime * _lookIKSmoothSpeed);

                _animator.SetLookAtPosition(_lookIKTarget);
                _animator.SetLookAtWeight(_currentIKWeight, 0.3f, 0.6f, 1f, 0.5f);
            }
        }
    }

    // --- Setup & References ---

    public void SetAnimator(Animator animator)
    {
        _animator = animator;
    }

    public void SetThirdPersonWeaponVisuals(Transform rifle, Transform pistol)
    {
        _tpRifleVisual = rifle;
        _tpPistolVisual = pistol;
    }
}
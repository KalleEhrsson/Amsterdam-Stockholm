using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    // Simple movement state machine
    public enum MovementState { Idle = 0, Walking = 1, Jumping = 2, Falling = 3, Crouching = 4}
    private MovementState currentState = MovementState.Idle;
    
    public enum SurfaceType { Wood = 0, Metal = 1 }

    // Expose current state for external systems
    public MovementState CurrentState => currentState;

    // Inspector: movement tuning values visible in the editor
    #region Inspector: Movement
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float jumpForce = 5f;

    [Tooltip("Current speed after crouch and slow modifiers.")]
    [SerializeField] private float moveSpeedRuntime;

    [Tooltip("Reserved for external systems (buffs, sprint, etc.). Not applied by default.")]
    [SerializeField] private float externalSpeedMultiplier = 1f;
    
    private Vector3 lastWallNormal;
    private bool hasWallNormal;
    private Vector3 lastGroundNormal = Vector3.up;
    // Steep slope runtime state
    private bool isOnSteepSlope;
    private float steepSlopeTimer;
    private float lastSlopeAngleDeg;
    
    // Events exposed for external systems (VFX, audio, animation, etc.). Prefer the C# events.
    #region Events
    public event Action OnLand;
    public event Action OnJump;
    public event Action OnLeaveGround;
    public event Action OnInteract;
    #endregion
    #endregion
    
    /// <summary>
    /// Animation: read-only values consumed by the animation controller.
    /// </summary>
    #region Animation (Exposed)
    public bool FacingRight { get; private set; } = true;
    public float HorizontalInput { get; private set; }
    public float HorizontalSpeedNormalized { get; private set; }
    public float VerticalVelocity { get; private set; }
    public float MoveX => HorizontalInput;
    public float IdleX => FacingRight ? 1f : -1f;
    #endregion

    /// <summary>
    /// Inspector: ground detection and jump timing settings
    /// </summary>
    #region Inspector: Ground & Jump Timing
    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Jump Timing")]
    [Tooltip("Allows jump input slightly before landing.")]
    [SerializeField] private float jumpBufferTime = 0.15f;

    [Tooltip("Allows jumping shortly after leaving the ground.")]
    [SerializeField] private float coyoteTime = 0.1f;

    [Header("Jump Feel")]
    [Tooltip("Multiplier applied to gravity while falling to make falls snappier.")]
    [SerializeField] private float fallMultiplier = 2.5f;

    [Tooltip("Multiplier applied to gravity when the jump button is released early to cut the jump height.")]
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Slope")]
    [Tooltip("Small downward force applied along ground normal to keep the character stuck to slopes while walking.")]
    [SerializeField] private float groundStickForce = 5f;
    
    [Tooltip("Maximum slope angle (in degrees) the player can 'walk' on. Slopes steeper than this will cause sliding.")]
    [SerializeField] private float maxWalkableSlopeDegrees = 50f;

    [Tooltip("Acceleration applied down the slope when standing on a slope steeper than maxWalkableSlopeDegrees.")]
    [SerializeField] private float slopeSlideAcceleration = 9f;
    
    [SerializeField, Tooltip("How long (seconds) the player can still move on slopes steeper than maxWalkableSlopeDegrees before slipping.")]
    private float steepSlopeGraceTime = 1.0f;
    [Tooltip("Rate (units/sec) at which the player's uphill movement is reduced while pushing into an unwalkable slope.")]
    [SerializeField] private float uphillReductionRate = 1.0f;

    [Tooltip("Rate (units/sec) at which the player's uphill movement recovers when not pushing into a steep slope.")]
    [SerializeField] private float uphillRecoveryRate = 2.0f;
    
    // (Debug values removed - moveSpeedRuntime now displays effective max speed)

    [Header("Ground Query")]
    [SerializeField, Tooltip("SphereCast distance used for ground detection.")]
    private float groundCastDistance = 0.2f;

    [SerializeField, Tooltip("Radius multiplier applied to collider.radius when performing ground SphereCast.")]
    private float groundSphereRadiusMultiplier = 0.98f;

    [SerializeField, Tooltip("Small offset downward applied to ground check origin to reduce false negatives.")]
    private float groundBottomBias = 0.05f;
    #endregion
    
    #region Inspector: Surface Detection
    [Header("Surface Detection (Physics Material)")]
    [SerializeField] private PhysicsMaterial woodMaterial;
    [SerializeField] private PhysicsMaterial metalMaterial;
    #endregion

    /// <summary>
    /// Inspector: crouch-related settings (visuals, timings)
    /// </summary>
    #region Inspector: Crouch
    [Header("Crouch Settings")]
    [Tooltip("Scaled during crouch. Defaults to this transform if not set.")]
    [SerializeField] private Transform visual;

    [SerializeField] private float standUpDuration = 0.2f;
    [SerializeField] private float crouchDownDuration = 0.2f;

    [Tooltip("Delay before movement speed recovers while standing up.")]
    [SerializeField] private float standSpeedHoldTime = 0.3f;

    [Tooltip("Jump height multiplier applied when jumping while crouched.")]
    [SerializeField] private float crouchJumpMultiplier = 0.6f;
    #endregion

    /// <summary>
    /// Component references and cached geometry
    /// </summary>
    #region Components
    private Rigidbody rb;
    private CapsuleCollider col;
    private Transform tr;
    private Vector3 cachedUp;
    private Vector3 cachedRight;
    private float cachedColRadius;
    private float cachedHalfHeight;
    private float cachedBottomOffset;
    private Vector3 cachedColCenterLocal;
    #endregion

    /// <summary>
    /// Runtime jump-related timers and flags
    /// </summary>
    #region Jump State
    bool isGrounded;
    private float jumpBufferCounter;
    private float coyoteCounter;
    #endregion
    
    #region Input Cache
    private float horizontalInputRaw;
    private bool jumpHeld;
    #endregion

    /// <summary>
    /// Runtime crouch state and lerp bookkeeping
    /// </summary>
    #region Crouch State
    private float crouchSpeed;
    private float originalHeight;
    private float crouchedHeight;
    private float originalBottom;

    private bool isCrouching;
    private bool isStandingLerping;
    private bool isCrouchLerping;

    private float standSpeedRecoveryDelay;
    private float standLerpT;
    private float crouchLerpT;

    private Vector3 originalCenter;
    #endregion

    /// <summary>
    /// Slow/buff system runtime state
    /// </summary>
    #region Slow System
    private float baseSpeedRuntime;
    private float slowMultiplierTotal = 1f;
    private readonly List<float> activeSlows = new();
    // Runtime multiplier used to gradually reduce uphill movement when on very steep slopes.
    private float uphillSpeedMultiplier = 1f;
    #endregion

    /// <summary>
    /// Unity lifecycle methods (Awake/Start/Update/FixedUpdate)
    /// </summary>
    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        tr = transform;
        
        CacheColliderGeometry();
        
        cachedUp = tr.up;
        cachedRight = tr.right;

        rb.freezeRotation = true;

        AutoAssignGroundLayer();
        
        if (visual == null)
        {
            if (transform.parent != null)
            {
                Transform sibling = transform.parent.Find("Visual");
                if (sibling != null)
                    visual = sibling;
            }

            if (visual == null)
            {
                Transform child = transform.Find("Visual");
                if (child != null)
                    visual = child;
            }

            visual ??= transform;
        }
    }
    
    private void Start()
    {
        // Runtime speed is derived from base speed plus modifiers (crouch, slows).
        baseSpeedRuntime = moveSpeed;
        ApplySlowToRuntime();

        InitializeCrouchGeometry();
    }

    private void Update()
    {
        UpdateCachedTransform();

        ReadInput();
        UpdateJumpBuffer(Input.GetButtonDown("Jump"));
        HandleCrouchInput(Input.GetKey(KeyCode.LeftControl));

        if (Input.GetKeyDown(KeyCode.E))
            OnInteract?.Invoke();
    }

    private void FixedUpdate()
    {
        UpdateCachedTransform();
        bool prevGrounded = isGrounded;
        bool groundHit = ComputeIsGrounded();

        if (groundHit && isOnSteepSlope)
            steepSlopeTimer += Time.fixedDeltaTime;
        else
            steepSlopeTimer = 0f;

        bool isSlippingOnSteepSlope = groundHit && isOnSteepSlope && (steepSlopeTimer >= steepSlopeGraceTime || uphillSpeedMultiplier <= 0.05f);
        UpdateGroundedState(prevGrounded, groundHit, isSlippingOnSteepSlope);
        
        HandleHorizontalMovement(isSlippingOnSteepSlope);
        HandleJump();
        HandleVariableJumpGravity();
        HandleCrouchLerps();
        UpdateAirborneState();
        UpdateGroundMoveState();

        if (rb != null)
            VerticalVelocity = rb.linearVelocity.y;
    }
    
    private void UpdateGroundedState(bool wasGrounded, bool groundHit, bool isSlippingOnSteepSlope)
    {
        isGrounded = groundHit && !isSlippingOnSteepSlope;

        if (wasGrounded && !isGrounded)
        {
            OnLeaveGround?.Invoke();

            if (rb == null || rb.linearVelocity.y <= 0.01f)
                currentState = MovementState.Falling;
        }

        if (!wasGrounded && isGrounded)
        {
            OnLand?.Invoke();

            currentState = isCrouching
                ? MovementState.Crouching
                : Mathf.Abs(horizontalInputRaw) > 0.1f
                    ? MovementState.Walking
                    : MovementState.Idle;
        }
    }
    #endregion

    /// <summary>
    /// Cache update helper
    /// </summary>
    private void UpdateCachedTransform()
    {
        if (tr == null)
            tr = transform;

        cachedUp = tr.up;
        cachedRight = tr.right;
    }

    /// <summary>
    /// Movement handling (horizontal, wall/ground projection)
    /// </summary>
    #region Movement
    private void ReadInput()
    {
        horizontalInputRaw = Input.GetAxis("Horizontal");
        HorizontalInput = horizontalInputRaw;
        jumpHeld = Input.GetButton("Jump");

        if (Mathf.Abs(horizontalInputRaw) > 0.01f)
            FacingRight = horizontalInputRaw > 0f;
    }
    
    private void UpdateJumpBuffer(bool jumpPressedThisFrame)
    {
        jumpBufferCounter = jumpPressedThisFrame
            ? jumpBufferTime
            : Mathf.Max(jumpBufferCounter - Time.deltaTime, 0f);
    }

    private void HandleCrouchInput(bool crouchHeld)
    {
        if (crouchHeld)
        {
            StartCrouch();
            return;
        }

        TryStand();
    }
    
    private void HandleHorizontalMovement(bool isSlippingOnSteepSlope)
    {
        if (rb == null)
            return;
        
        float moveInput = horizontalInputRaw;
        float baseRuntimeSpeed = baseSpeedRuntime * slowMultiplierTotal * externalSpeedMultiplier;
        Vector3 inputDir = cachedRight * moveInput;

        bool pushingUphillEarly = false;
        if (isOnSteepSlope && Mathf.Abs(moveInput) > 0.01f)
        {
            Vector3 slideDirEarly = Vector3.ProjectOnPlane(Physics.gravity, lastGroundNormal).normalized;
            float uphillDotEarly = Vector3.Dot(inputDir, -slideDirEarly);
            pushingUphillEarly = uphillDotEarly > 0.01f;
        }

        moveSpeedRuntime = baseRuntimeSpeed * (pushingUphillEarly ? uphillSpeedMultiplier : 1f);

        Vector3 desired;

        if (hasWallNormal)
        {
            float intoWall = Vector3.Dot(inputDir, lastWallNormal);
            if (intoWall < -0.001f)
                desired = Vector3.ProjectOnPlane(inputDir * baseRuntimeSpeed, lastWallNormal);
            else
                desired = inputDir * baseRuntimeSpeed;
        }
        else if (isGrounded)
        {
            desired = Vector3.ProjectOnPlane(inputDir * baseRuntimeSpeed, lastGroundNormal);
            float slopeAngle = Vector3.Angle(lastGroundNormal, Vector3.up);
            if (slopeAngle > maxWalkableSlopeDegrees)
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Physics.gravity, lastGroundNormal).normalized;
                float slopeProportion = Mathf.InverseLerp(maxWalkableSlopeDegrees, 90f, slopeAngle);

                float uphillDot = Vector3.Dot(desired, -slideDir);
                bool pushingUphill = uphillDot > 0.01f;

                if (pushingUphill)
                {
                    float reductionAmplifier = Mathf.Lerp(1f, 6f, slopeProportion);
                    uphillSpeedMultiplier = Mathf.Max(0f, uphillSpeedMultiplier - uphillReductionRate * reductionAmplifier * Time.fixedDeltaTime);
                }
                else
                {
                    uphillSpeedMultiplier = Mathf.Min(1f, uphillSpeedMultiplier + uphillRecoveryRate * Time.fixedDeltaTime);
                }

                if (uphillSpeedMultiplier < 1f && pushingUphill)
                {
                    Vector3 uphillComp = Vector3.Project(desired, -slideDir);
                    Vector3 otherComp = desired - uphillComp;
                    desired = otherComp + uphillComp * uphillSpeedMultiplier;
                }
            }
            else
            {
                uphillSpeedMultiplier = Mathf.Min(1f, uphillSpeedMultiplier + uphillRecoveryRate * Time.fixedDeltaTime);
            }
        }
        else
        {
            desired = inputDir * baseRuntimeSpeed;
            uphillSpeedMultiplier = 1f;
        }

        Vector3 v = rb.linearVelocity;

        v.x = desired.x;
        v.z = 0f;

        if (isGrounded && v.y <= 0.1f)
        {
            float stickScale = -groundStickForce * Time.fixedDeltaTime;
            v += lastGroundNormal * stickScale;
        }

        // If we're on a steep slope (regardless of whether we are still considered grounded), apply sliding.
        if (isOnSteepSlope)
        {
            if (isSlippingOnSteepSlope)
            {
                v.x = 0f;
                v.z = 0f;
                currentState = MovementState.Falling;

                if (v.y > -0.5f)
                    v.y = -0.5f;
            }

            Vector3 slide = Vector3.ProjectOnPlane(Physics.gravity, lastGroundNormal);

            if (slide.sqrMagnitude > 0.0001f)
            {
                Vector3 slideDir = slide.normalized;

                float slideFactor = Mathf.InverseLerp(maxWalkableSlopeDegrees, 90f, lastSlopeAngleDeg);
                float strengthMultiplier = isSlippingOnSteepSlope ? 1f : 0.35f;

                float slideScale = slopeSlideAcceleration * slideFactor * strengthMultiplier * Time.fixedDeltaTime;

                v += slideDir * slideScale;
            }
        }

        rb.linearVelocity = v;
        float vx = Vector3.Dot(rb.linearVelocity, cachedRight);
        float absVx = Mathf.Abs(vx);
        float maxSpeed = Mathf.Max(0.01f, baseRuntimeSpeed);
        HorizontalSpeedNormalized = Mathf.Clamp01(absVx / maxSpeed);
    }

    private void UpdateGroundMoveState()
    {
        // Only update Idle/Walking when grounded and not crouching and not in air states.
        if (!isGrounded || isCrouching)
            return;

        // Do not overwrite Jumping/Falling/Crouching states.
        if (currentState == MovementState.Jumping || currentState == MovementState.Falling || currentState == MovementState.Crouching)
            return;

        currentState = Mathf.Abs(horizontalInputRaw) > 0.1f
            ? MovementState.Walking
            : MovementState.Idle;
    }
    
    private void OnCollisionStay(Collision collision)
    {
        hasWallNormal = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint cp = collision.GetContact(i);
            Vector3 n = cp.normal;

            // Ignore ground-ish normals, we only care about walls.
            if (Vector3.Dot(n, cachedUp) > 0.6f)
                continue;

            lastWallNormal = n;
            hasWallNormal = true;

            break;
        }
    }

    private void OnCollisionExit(Collision _)
    {
        hasWallNormal = false;
    }
    #endregion

    /// <summary>
    /// Jump logic and variable gravity handling
    /// </summary>
    #region Jump
    private void HandleJump()
    {
        if (isCrouching)
            return;
    
        // Coyote time: you can still jump briefly after stepping off an edge.
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        // Jump triggers when both timers are valid:
        // buffer means "recently pressed jump", coyote means "still allowed to jump".
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            // Prevent jumping from steep slopes that will cause slipping/falling.
            if (lastSlopeAngleDeg > maxWalkableSlopeDegrees)
            {
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
                return;
            }
            Vector3 v = rb.linearVelocity;
            float effectiveJump = isCrouching ? jumpForce * crouchJumpMultiplier : jumpForce;
            v.y = effectiveJump;
            rb.linearVelocity = v;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            // Notify listeners that a jump happened.
            OnJump?.Invoke();
            // Transition to Jumping state
            currentState = MovementState.Jumping;
        }
    }

    private void HandleVariableJumpGravity()
    {
        if (rb == null)
            return;

        Vector3 v = rb.linearVelocity;

        // Falling: apply stronger gravity for snappier falls.
        if (v.y < 0f)
        {
            float gravityScale = (fallMultiplier - 1f) * Time.fixedDeltaTime;
            v += Physics.gravity * gravityScale;
            rb.linearVelocity = v;
            return;
        }

        // Ascending but jump button released: cut the jump by applying extra gravity.
        if (v.y > 0f && !jumpHeld)
        {
            float gravityScale = (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            v += Physics.gravity * gravityScale;
            rb.linearVelocity = v;
        }
    }
    #endregion

    private void UpdateAirborneState()
    {
        if (isGrounded || rb == null)
            return;

        float verticalVelocity = rb.linearVelocity.y;

        if (verticalVelocity > 0.05f)
        {
            currentState = MovementState.Jumping;
        }
        else if (verticalVelocity < -0.05f)
        {
            currentState = MovementState.Falling;
        }
    }

    /// <summary>
    /// Crouch geometry and lerp helpers
    /// </summary>
    #region Crouch
    private void InitializeCrouchGeometry()
    {
        originalHeight = col.height;
        originalCenter = col.center;

        // Keep the collider's bottom fixed while lerping height by adjusting center.
        originalBottom = originalCenter.y - originalHeight * 0.5f;

        crouchedHeight = originalHeight * 0.6f;

        crouchSpeed = moveSpeed * 0.5f;

        // Cache collider geometry used by ground checks to avoid repeated property access.
        RefreshCachedColliderGeometry();
    }

    private void StartCrouch()
    {
        if (isCrouching)
            return;

        isCrouching = true;

        // Cancel standing lerp if we start crouching again mid-transition.
        isStandingLerping = false;

        isCrouchLerping = true;
        crouchLerpT = 0f;
        // Enter crouch state
        currentState = MovementState.Crouching;
    }

    private void TryStand()
    {
        if (!isCrouching)
            return;

        // Do not stand if we would intersect geometry above us.
        if (!HasStandingClearance())
            return;

        isCrouching = false;

        // Cancel crouch lerp if we stand mid-transition.
        isCrouchLerping = false;

        isStandingLerping = true;
        standLerpT = 0f;

        // Holding speed recovery gives a more "weighty" stand-up feel.
        standSpeedRecoveryDelay = standSpeedHoldTime;
        // When attempting to stand, move to Idle state (or Walking later in FixedUpdate if moving).
        currentState = MovementState.Idle;
    }

    private void HandleCrouchLerps()
    {
        if (isCrouchLerping)
            UpdateCrouchDownLerp();

        if (isStandingLerping)
            UpdateStandUpLerp();
    }

    private void UpdateCrouchDownLerp()
    {
        crouchLerpT += Time.fixedDeltaTime / Mathf.Max(0.0001f, crouchDownDuration);
        float t = Mathf.Clamp01(crouchLerpT);

        col.height = Mathf.Lerp(originalHeight, crouchedHeight, t);

        // Keep the collider bottom pinned to originalBottom while height changes.
        float newCenterY = originalBottom + col.height * 0.5f;
        col.center = new Vector3(originalCenter.x, newCenterY, originalCenter.z);

        // Refresh cached collider geometry
        RefreshCachedColliderGeometry();

        // Crouch changes base speed, then slow system applies on top.
        baseSpeedRuntime = Mathf.Lerp(moveSpeed, crouchSpeed, t);
        ApplySlowToRuntime();

        if (t >= 1f)
            isCrouchLerping = false;
    }

    private void UpdateStandUpLerp()
    {
        standLerpT += Time.fixedDeltaTime / Mathf.Max(0.0001f, standUpDuration);
        float t = Mathf.Clamp01(standLerpT);

        col.height = Mathf.Lerp(crouchedHeight, originalHeight, t);

        // Keep the collider bottom pinned to originalBottom while height changes.
        float newCenterY = originalBottom + col.height * 0.5f;
        col.center = new Vector3(originalCenter.x, newCenterY, originalCenter.z);

        RefreshCachedColliderGeometry();

        if (standSpeedRecoveryDelay > 0f)
        {
            standSpeedRecoveryDelay -= Time.fixedDeltaTime;
        }
        else
        {
            // Restore base speed over time, then apply slow system on top.
            baseSpeedRuntime = Mathf.Lerp(crouchSpeed, moveSpeed, t);
            ApplySlowToRuntime();
        }

        // End the stand state when the geometry and speed are fully restored.
        if (t >= 1f && Mathf.Abs(moveSpeedRuntime - baseSpeedRuntime * slowMultiplierTotal) < 0.01f)
        {
            baseSpeedRuntime = moveSpeed;
            ApplySlowToRuntime();
            isStandingLerping = false;
        }
    }
    #endregion

    /// <summary>
    /// Public API for temporary slows
    /// </summary>
    #region Slows
    public void AddSlow(float multiplier, float duration)
    {
        StartCoroutine(StrongestSlowRoutine(multiplier, duration));
    }

    private IEnumerator StrongestSlowRoutine(float multiplier, float duration)
    {
        // "Strongest slow wins" means we keep the smallest multiplier active.
        activeSlows.Add(multiplier);
        RecalculateSlows();

        yield return new WaitForSeconds(duration);

        activeSlows.Remove(multiplier);
        RecalculateSlows();
    }

    private void RecalculateSlows()
    {
        if (activeSlows.Count == 0)
        {
            slowMultiplierTotal = 1f;
            ApplySlowToRuntime();
            return;
        }

        float strongest = 1f;
        foreach (float m in activeSlows)
        {
            if (m < strongest)
                strongest = m;
        }

        slowMultiplierTotal = strongest;
        ApplySlowToRuntime();
    }

    private void ApplySlowToRuntime()
    {
        // Include external speed multipliers (sprints, buffs) on top of slows.
        moveSpeedRuntime = baseSpeedRuntime * slowMultiplierTotal * externalSpeedMultiplier;
    }
    #endregion

    /// <summary>
    /// Read-only public state properties for other systems
    /// </summary>
    #region Public State
    // Read-only properties for other systems to query without mutating state.
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public float CurrentMoveSpeed => moveSpeedRuntime;
    public SurfaceType CurrentSurface { get; set; }
    #endregion

    /// <summary>
    /// Ground query helpers (spherecast position and checks)
    /// </summary>
    #region Ground Check
    private Vector3 GetGroundCheckPosition()
    {
        // Defensive: if collider/transform missing, fallback to transform position.
        if (col == null || tr == null)
            return transform.position;
        // Compute a point near the capsule bottom in world space using cached collider geometry.
        Vector3 worldCenter = tr.TransformPoint(cachedColCenterLocal);
        Vector3 up = cachedUp;

        float bottomOffset = cachedBottomOffset;

        Vector3 bottomSphereCenter = worldCenter - up * bottomOffset;

        // Small offset downward reduces false negatives on tiny bumps.
        return bottomSphereCenter + up * groundBottomBias;
    }

    private bool ComputeIsGrounded()
    {
        if (col == null || tr == null)
            return false;

        Vector3 origin = GetGroundCheckPosition();
        float radius = cachedColRadius * groundSphereRadiusMultiplier;

        // Tiny cast distance makes it stable across small bumps and solver jitter.
        float castDistance = groundCastDistance;

        if (Physics.SphereCast(
                origin,
                radius,
                -cachedUp,
                out RaycastHit hit,
                castDistance,
                groundLayer,
                QueryTriggerInteraction.Ignore
            ))
        {
            
            lastGroundNormal = hit.normal;
            lastSlopeAngleDeg = Vector3.Angle(hit.normal, Vector3.up);
            isOnSteepSlope = lastSlopeAngleDeg > maxWalkableSlopeDegrees;

            PhysicsMaterial pm = hit.collider != null ? hit.collider.sharedMaterial : null;

            CurrentSurface = SurfaceType.Wood;

            if (pm != null)
            {
                if (pm == metalMaterial)
                    CurrentSurface = SurfaceType.Metal;
                else if (pm == woodMaterial)
                    CurrentSurface = SurfaceType.Wood;
            }

            return true;
        }

        // No ground hit: reset slope info and indicate not grounded.
        lastGroundNormal = cachedUp;
        lastSlopeAngleDeg = 0f;
        isOnSteepSlope = false;

        return false;
    }
    
    private void CacheColliderGeometry()
    {
        if (col == null)
            col = GetComponent<CapsuleCollider>();
        
        RefreshCachedColliderGeometry();
    }

    private void RefreshCachedColliderGeometry()
    {
        if (col == null)
            return;

        cachedColRadius = col.radius;
        cachedHalfHeight = Mathf.Max(col.height * 0.5f, cachedColRadius);
        cachedBottomOffset = cachedHalfHeight - cachedColRadius;
        cachedColCenterLocal = col.center;
    }
    #endregion

    /// <summary>
    /// Checks for standing clearance using capsule overlap
    /// </summary>
    #region Stand Clearance (Capsule)
    private bool HasStandingClearance()
    {
        // Checks if standing to originalHeight would overlap anything in groundLayer.
        // This prevents "standing up into a ceiling" while crouched under something.
        if (col == null || tr == null)
            return true;

        Vector3 up = cachedUp;
        Vector3 worldCenter = tr.TransformPoint(originalCenter);

        float radius = cachedColRadius;
        float halfHeight = Mathf.Max(originalHeight * 0.5f, radius);

        Vector3 top = worldCenter + up * (halfHeight - radius);
        Vector3 bottom = worldCenter - up * (halfHeight - radius);

        float skin = 0.01f;
        
        bottom += up * 0.05f;

        return !Physics.CheckCapsule(
            bottom,
            top,
            Mathf.Max(0f, radius - skin),
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }
    #endregion

    private void AutoAssignGroundLayer()
    {
        int layerIndex = LayerMask.NameToLayer("Walkable");

        if (layerIndex == -1)
        {
            groundLayer = 0;
            return;
        }

        groundLayer = 1 << layerIndex;
    }
}
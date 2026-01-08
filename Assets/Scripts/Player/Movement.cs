using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Movement : MonoBehaviour
{
    // Simple movement state machine
    public enum MovementState { Idle, Walking, Jumping, Falling, Crouching }
    private MovementState currentState = MovementState.Idle;

    // Expose current state for external systems
    public MovementState CurrentState => currentState;

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
    
    // Events: useful for hooking VFX/sound/other systems when player lands or jumps.
    public event Action OnLand;
    public event Action OnJump;
    public event Action OnLeaveGround;

    [SerializeField, HideInInspector] private UnityEvent onLand;
    [SerializeField, HideInInspector] private UnityEvent onJump;
    [SerializeField, HideInInspector] private UnityEvent onLeave;
    #endregion

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
    
    [Header("Ground Query")]
    [SerializeField, Tooltip("SphereCast distance used for ground detection.")]
    private float groundCastDistance = 0.08f;

    [SerializeField, Tooltip("Radius multiplier applied to collider.radius when performing ground SphereCast.")]
    private float groundSphereRadiusMultiplier = 0.95f;

    [SerializeField, Tooltip("Small offset downward applied to ground check origin to reduce false negatives.")]
    private float groundBottomBias = 0.02f;
    #endregion

    #region Inspector: Crouch
    [Header("Crouch Settings")]
    [Tooltip("Scaled during crouch. Defaults to this transform if not set.")]
    [SerializeField] private Transform visual;

    [SerializeField] private float standUpDuration = 0.2f;
    [SerializeField] private float crouchDownDuration = 0.2f;

    [Tooltip("Delay before movement speed recovers while standing up.")]
    [SerializeField] private float standSpeedHoldTime = 0.3f;
    #endregion

    #region Components
    private Rigidbody rb;
    private CapsuleCollider col;
    private Transform tr;
    private Vector3 cachedUp;
    private Vector3 cachedRight;
    // Cached collider geometry (updated when collider or height changes).
    private float cachedColRadius;
    private float cachedHalfHeight;
    private float cachedBottomOffset;
    private Vector3 cachedColCenterLocal;
    #endregion

    #region Jump State
    private bool isGrounded;
    private float jumpBufferCounter;
    private float coyoteCounter;
    #endregion

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
    private Vector3 visualScaleOriginal;
    private Vector3 visualScaleCrouched;
    #endregion

    #region Slow System
    private float baseSpeedRuntime;
    private float slowMultiplierTotal = 1f;
    private readonly List<float> activeSlows = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        visual ??= transform;
        tr = transform;

        // Initialize cached axes to avoid reading transform repeatedly.
        cachedUp = tr.up;
        cachedRight = tr.right;

        rb.freezeRotation = true;

        AutoAssignGroundLayer();
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
        // Keep cached transform axes fresh for methods called from Update.
        UpdateCachedTransform();

        // Jump buffer makes input feel responsive:
        // press jump slightly early and it will still trigger on landing.
        jumpBufferCounter = Input.GetButtonDown("Jump")
            ? jumpBufferTime
            : Mathf.Max(jumpBufferCounter - Time.deltaTime, 0f);

        // Crouch input
        bool crouchHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.S);
        if (crouchHeld)
            StartCrouch();
        else
            TryStand();
    }

    private void FixedUpdate()
    {
        // Keep cached transform axes fresh for physics-step methods.
        UpdateCachedTransform();

        // Cache ground check once per physics step to avoid duplicate Physics queries.
        bool prevGrounded = isGrounded;
        isGrounded = ComputeIsGrounded();

        // Fire leave-ground event when we transition from grounded -> not-grounded
        if (prevGrounded && !isGrounded)
        {
            OnLeaveGround?.Invoke();
            onLeave?.Invoke();
            // transition to falling when we leave ground (passive falls)
            currentState = MovementState.Falling;
        }

        // Fire landing event exactly when we transition from not-grounded -> grounded.
        if (!prevGrounded && isGrounded)
        {
            OnLand?.Invoke();
            onLand?.Invoke();
            // decide Idle/Walking on land
            float h = Input.GetAxis("Horizontal");
            if (isCrouching)
                currentState = MovementState.Crouching;
            else if (Mathf.Abs(h) > 0.1f)
                currentState = MovementState.Walking;
            else
                currentState = MovementState.Idle;
        }

        // State-specific behavior: keep existing handlers but update state where needed.
        HandleHorizontalMovement();
        HandleJump();
        HandleVariableJumpGravity();
        HandleCrouchLerps();

        // If grounded and not crouching, decide between Idle and Walking.
        UpdateGroundMoveState();
    }
    #endregion

    // Cache update helper
    private void UpdateCachedTransform()
    {
        if (tr == null)
            tr = transform;

        cachedUp = tr.up;
        cachedRight = tr.right;
    }

    #region Movement
    private void HandleHorizontalMovement()
    {
        if (rb == null)
            return;

        float moveInput = Input.GetAxis("Horizontal");
        float currentSpeed = moveSpeedRuntime;

        Vector3 inputDir = cachedRight * moveInput;

        Vector3 desired;

        // If we're touching a wall, project movement along the wall to avoid pushing into it.
        if (hasWallNormal)
        {
            desired = Vector3.ProjectOnPlane(inputDir * currentSpeed, lastWallNormal);
        }
        // If we're grounded, project movement onto the ground plane so we walk along slopes naturally.
        else if (isGrounded)
        {
            desired = Vector3.ProjectOnPlane(inputDir * currentSpeed, lastGroundNormal);
        }
        else
        {
            desired = inputDir * currentSpeed;
        }

        Vector3 v = rb.linearVelocity;

        // Apply the horizontal components from the desired slope-aware vector. Leave vertical velocity intact
        // so jumping/falling still behaves as expected.
        v.x = desired.x;
        v.z = desired.z;

        // Small stick-to-ground force: while grounded and not jumping up, push the character into the ground
        // along the ground normal so they don't 'hop' on small geometry when walking uphill.
        if (isGrounded && v.y <= 0.1f)
        {
            v += -lastGroundNormal * groundStickForce * Time.fixedDeltaTime;
        }

        rb.linearVelocity = v;
    }

    private void UpdateGroundMoveState()
    {
        // Only update Idle/Walking when grounded and not crouching and not in air states.
        if (!isGrounded || isCrouching)
            return;

        // Do not overwrite Jumping/Falling/Crouching states.
        if (currentState == MovementState.Jumping || currentState == MovementState.Falling || currentState == MovementState.Crouching)
            return;

        float h = Input.GetAxis("Horizontal");
        if (Mathf.Abs(h) > 0.1f)
            currentState = MovementState.Walking;
        else
            currentState = MovementState.Idle;
    }
    
    private void OnCollisionStay(Collision collision)
    {
        hasWallNormal = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 n = collision.GetContact(i).normal;
            // Ignore ground-ish normals, we only care about walls.
            if (Vector3.Dot(n, cachedUp) > 0.6f)
                continue;

            lastWallNormal = n;
            hasWallNormal = true;
            break;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        hasWallNormal = false;
    }
    #endregion

    #region Jump
    private void HandleJump()
    {
        // Coyote time: you can still jump briefly after stepping off an edge.
        if (isGrounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        // Jump triggers when both timers are valid:
        // buffer means "recently pressed jump", coyote means "still allowed to jump".
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            Vector3 v = rb.linearVelocity;
            v.y = jumpForce;
            rb.linearVelocity = v;

            jumpBufferCounter = 0f;
            coyoteCounter = 0f;

            // Notify listeners that a jump happened.
            OnJump?.Invoke();
            onJump?.Invoke();
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
            v += Physics.gravity * (fallMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = v;
            return;
        }

        // Ascending but jump button released: cut the jump by applying extra gravity.
        if (v.y > 0f && !Input.GetButton("Jump"))
        {
            v += Physics.gravity * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
            rb.linearVelocity = v;
        }
    }
    #endregion

    #region Crouch
    private void InitializeCrouchGeometry()
    {
        originalHeight = col.height;
        originalCenter = col.center;

        // Keep the collider's bottom fixed while lerping height by adjusting center.
        originalBottom = originalCenter.y - originalHeight * 0.5f;

        crouchedHeight = originalHeight * 0.6f;

        crouchSpeed = moveSpeed * 0.5f;

        visualScaleOriginal = visual.localScale;
        visualScaleCrouched = new Vector3(
            visualScaleOriginal.x,
            visualScaleOriginal.y * 0.6f,
            visualScaleOriginal.z
        );

        // Cache collider geometry used by ground checks to avoid repeated property access.
        if (col != null)
        {
            cachedColRadius = col.radius;
            cachedHalfHeight = Mathf.Max(col.height * 0.5f, col.radius);
            cachedBottomOffset = cachedHalfHeight - col.radius;
            cachedColCenterLocal = col.center;
        }
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
        if (col != null)
        {
            cachedColRadius = col.radius;
            cachedHalfHeight = Mathf.Max(col.height * 0.5f, col.radius);
            cachedBottomOffset = cachedHalfHeight - col.radius;
            cachedColCenterLocal = col.center;
        }

        visual.localScale = Vector3.Lerp(visualScaleOriginal, visualScaleCrouched, t);

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

        // Refresh cached collider geometry
        if (col != null)
        {
            cachedColRadius = col.radius;
            cachedHalfHeight = Mathf.Max(col.height * 0.5f, col.radius);
            cachedBottomOffset = cachedHalfHeight - col.radius;
            cachedColCenterLocal = col.center;
        }

        visual.localScale = Vector3.Lerp(visualScaleCrouched, visualScaleOriginal, t);

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

    #region Public State
    // Read-only properties for other systems to query without mutating state.
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public float CurrentMoveSpeed => moveSpeedRuntime;
    #endregion

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
        return bottomSphereCenter - up * groundBottomBias;
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
            // Record the ground normal we hit so movement can be projected onto slopes.
            lastGroundNormal = hit.normal;
            return true;
        }

        lastGroundNormal = cachedUp;
        return false;
    }
    #endregion

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
            Debug.LogError(
                "Movement: No layer named 'Walkable' exists. Ground checks will fail.",
                this
            );
            groundLayer = 0;
            return;
        }

        groundLayer = 1 << layerIndex;
    }
    
    #region Gizmos (Debug)
    private void OnDrawGizmos()
    {
        CapsuleCollider c = GetComponent<CapsuleCollider>();
        if (!c)
            return;

        Transform t = transform;
        Vector3 up = t.up;

        // ---------- Current collider (yellow) ----------
        Gizmos.color = Color.yellow;

        Vector3 currentCenter = t.TransformPoint(c.center);
        float currentHalfHeight = Mathf.Max(c.height * 0.5f, c.radius);

        Vector3 currentTop = currentCenter + up * (currentHalfHeight - c.radius);
        Vector3 currentBottom = currentCenter - up * (currentHalfHeight - c.radius);

        Gizmos.DrawWireSphere(currentTop, c.radius);
        Gizmos.DrawWireSphere(currentBottom, c.radius);

        // ---------- Standing clearance capsule (red) ----------
        // This exactly matches HasStandingClearance()
        Gizmos.color = Color.red;

        Vector3 standCenter = t.TransformPoint(originalCenter);
        float standHalfHeight = Mathf.Max(originalHeight * 0.5f, c.radius);

        Vector3 standTop = standCenter + up * (standHalfHeight - c.radius);
        Vector3 standBottom = standCenter - up * (standHalfHeight - c.radius);

        Gizmos.DrawWireSphere(standTop, c.radius);
        Gizmos.DrawWireSphere(standBottom, c.radius);

        Gizmos.DrawLine(standTop + t.right * c.radius, standBottom + t.right * c.radius);
        Gizmos.DrawLine(standTop - t.right * c.radius, standBottom - t.right * c.radius);
        Gizmos.DrawLine(standTop + t.forward * c.radius, standBottom + t.forward * c.radius);
        Gizmos.DrawLine(standTop - t.forward * c.radius, standBottom - t.forward * c.radius);
    }
    #endregion
}
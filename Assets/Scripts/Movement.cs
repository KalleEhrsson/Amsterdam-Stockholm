using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float moveSpeedRuntime;
    float moveVelocity;

    [Header("Ground Settings")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.2f;

    [Header("Jump Timing")]
    [SerializeField] float jumpBufferTime = 0.15f;
    [SerializeField] float coyoteTime = 0.1f;
    
    [Header("Crouch Settings")]
    [SerializeField] Transform visual;
    [SerializeField] float crouchHeight = 0.7f;
    [SerializeField] float standingHeight = 1.4f;
    [SerializeField] float standUpDuration = 0.2f;
    [SerializeField] float crouchDownDuration = 0.2f;
    [SerializeField] float standSpeedHoldTime = 0.3f;

    Rigidbody rb;
    
    // jump
    bool isGrounded;
    float jumpBufferCounter;
    float coyoteCounter;
    
    // crouch
    CapsuleCollider col;
    float crouchSpeed;
    float originalHeight;
    float crouchedHeight;
    float originalBottom;
    bool isCrouching;
    bool isStandingLerping;
    bool isCrouchLerping;
    float standSpeedRecoveryDelay;
    
    float standLerpT;
    float crouchLerpT;
    
    // collider/scale
    Vector3 originalCenter;
    Vector3 visualScaleOriginal;
    Vector3 visualScaleCrouched;
    
    void Start()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        moveSpeedRuntime = moveSpeed;
        
        #region Crouch Calculations
        originalHeight = col.height;
        originalCenter = col.center;

        // bottom = center - half height
        originalBottom = originalCenter.y - originalHeight * 0.5f;

        // collider shrink factor
        crouchedHeight = originalHeight * 0.6f;

        // speed shrink factor
        crouchSpeed = moveSpeed / 2f;

        // visual scaling (the child only)
        visualScaleOriginal = visual.localScale;
        visualScaleCrouched = new Vector3(
            visualScaleOriginal.x,
            visualScaleOriginal.y * 0.6f,
            visualScaleOriginal.z
        );
        #endregion
    }

    void Update()
    {
        jumpBufferCounter = Input.GetButtonDown("Jump")
            ? jumpBufferTime
            : Mathf.Max(jumpBufferCounter - Time.deltaTime, 0f);
        
        // Crouch input
        (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.S) ? (System.Action)StartCrouch : TryStand)();
    }
    
    void FixedUpdate()
    {
        #region Horizontal Movement
        float moveInput = Input.GetAxis("Horizontal");

        // pick speed first
        float currentSpeed = moveSpeedRuntime;

        Vector3 move = new Vector3(moveInput * currentSpeed, rb.linearVelocity.y, 0f);
        rb.linearVelocity = move;
        #endregion
        
        #region Jump
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundLayer);

        if (isGrounded)
        {
            coyoteCounter = coyoteTime;
        }
        else
        {
            coyoteCounter -= Time.fixedDeltaTime;
        }

        // Perform jump if buffer + ground overlap
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
            jumpBufferCounter = 0f;
        }
        #endregion
        
        #region CrouchLerp
        if (isCrouchLerping)
        {
            crouchLerpT += Time.fixedDeltaTime / crouchDownDuration;
            float t = Mathf.Clamp01(crouchLerpT);

            // height
            col.height = Mathf.Lerp(originalHeight, crouchedHeight, t);

            // lock bottom
            float newCenterY = originalBottom + col.height * 0.5f;
            col.center = new Vector3(originalCenter.x, newCenterY, originalCenter.z);

            // visual
            visual.localScale = Vector3.Lerp(visualScaleOriginal, visualScaleCrouched, t);

            // speed
            moveSpeedRuntime = Mathf.Lerp(moveSpeedRuntime, crouchSpeed, t);

            if (t >= 1f)
            {
                isCrouchLerping = false;
            }
        }
        #endregion

        #region StandLerp
        if (isStandingLerping)
        {
            standLerpT += Time.fixedDeltaTime / standUpDuration;
            float t = Mathf.Clamp01(standLerpT);

            // height
            col.height = Mathf.Lerp(crouchedHeight, originalHeight, t);

            // lock bottom
            float newCenterY = originalBottom + col.height * 0.5f;
            col.center = new Vector3(originalCenter.x, newCenterY, originalCenter.z);

            // visual
            visual.localScale = Vector3.Lerp(visualScaleCrouched, visualScaleOriginal, t);

            // speed: respect recovery delay
            if (standSpeedRecoveryDelay > 0f)
            {
                standSpeedRecoveryDelay -= Time.fixedDeltaTime;
            }
            else
            {
                moveSpeedRuntime = Mathf.Lerp(moveSpeedRuntime, moveSpeed, t);
            }

            if (t >= 1f && Mathf.Abs(moveSpeedRuntime - moveSpeed) < 0.01f)
            {
                moveSpeedRuntime = moveSpeed;
                isStandingLerping = false;
            }
        }
        #endregion
    }

    #region Crouching  
    void StartCrouch()
    {
        if (isCrouching) return;

        isCrouching = true;
        isStandingLerping = false;
        isCrouchLerping = true;
        isCrouchLerping = true;
        crouchLerpT = 0f;
    }
    
    void TryStand()
    {
        if (!isCrouching) return;

        Vector3 left = TopLeftPoint();
        Vector3 right = TopRightPoint();

        float standRoom = originalHeight - crouchedHeight;

        bool leftBlocked = Physics.Raycast(left, Vector3.up, standRoom, groundLayer);
        bool rightBlocked = Physics.Raycast(right, Vector3.up, standRoom, groundLayer);

        if (leftBlocked || rightBlocked)
            return;

        isCrouching = false;
        isCrouchLerping = false;
        isStandingLerping = true;
        isStandingLerping = true;
        standLerpT = 0f;
        
        standSpeedRecoveryDelay = standSpeedHoldTime;
    }

    #region StandRayCast 
    Vector3 TopLeftPoint()
    {
        float topY = originalBottom + crouchedHeight;
        return new Vector3(transform.position.x + originalCenter.x - col.radius, transform.position.y + topY, transform.position.z);
    }

    Vector3 TopRightPoint()
    {
        float topY = originalBottom + crouchedHeight;
        return new Vector3(transform.position.x + originalCenter.x + col.radius, transform.position.y + topY, transform.position.z);
    }
    #endregion
    #endregion
    
    void OnDrawGizmos()
    {
        // Only run if we have a collider
        CapsuleCollider c = GetComponent<CapsuleCollider>();
        if (!c) return;

        Gizmos.color = Color.yellow;

        Vector3 pos = transform.position + c.center;

        float height = c.height;
        float radius = c.radius;

        // Top and bottom sphere centers
        Vector3 top = pos + Vector3.up * (height * 0.5f - radius);
        Vector3 bottom = pos - Vector3.up * (height * 0.5f - radius);

        // Draw cylinder lines
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);
        
        Gizmos.color = Color.red;

        // stand height
        if (col)
        {
            Gizmos.color = Color.red;

            Vector3 left = TopLeftPoint();
            Vector3 right = TopRightPoint();
            float standRoom = originalHeight - crouchedHeight;

            Gizmos.DrawLine(left, left + Vector3.up * standRoom);
            Gizmos.DrawLine(right, right + Vector3.up * standRoom);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float moveSpeedRuntime;
    [SerializeField] float externalSpeedMultiplier = 1f;
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

    // slow system
    float baseSpeedRuntime;
    float slowMultiplierTotal = 1f;
    List<float> activeSlows = new List<float>();
    
    void Start()
    {
        rb  = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();

        baseSpeedRuntime = moveSpeed;
        moveSpeedRuntime = moveSpeed;
        
        #region Crouch Calculations
        originalHeight = col.height;
        originalCenter = col.center;

        originalBottom = originalCenter.y - originalHeight * 0.5f;
        crouchedHeight = originalHeight * 0.6f;

        crouchSpeed = moveSpeed / 2f;

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

        float currentSpeed = moveSpeedRuntime; // slow system handles changes

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

            col.height = Mathf.Lerp(originalHeight, crouchedHeight, t);

            float newCenterY = originalBottom + col.height * 0.5f;
            col.center = new Vector3(originalCenter.x, newCenterY, originalCenter.z);

            visual.localScale = Vector3.Lerp(visualScaleOriginal, visualScaleCrouched, t);

            // patched for slow support
            baseSpeedRuntime = Mathf.Lerp(baseSpeedRuntime, crouchSpeed, t);
            ApplySlowToRuntime();

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

            col.height = Mathf.Lerp(crouchedHeight, originalHeight, t);

            float newCenterY = originalBottom + col.height * 0.5f;
            col.center = new Vector3(originalCenter.x, newCenterY, originalCenter.z);

            visual.localScale = Vector3.Lerp(visualScaleCrouched, visualScaleOriginal, t);

            if (standSpeedRecoveryDelay > 0f)
            {
                standSpeedRecoveryDelay -= Time.fixedDeltaTime;
            }
            else
            {
                // patched for slow support
                baseSpeedRuntime = Mathf.Lerp(baseSpeedRuntime, moveSpeed, t);
                ApplySlowToRuntime();
            }

            if (t >= 1f && Mathf.Abs(moveSpeedRuntime - moveSpeed) < 0.01f)
            {
                baseSpeedRuntime = moveSpeed;
                ApplySlowToRuntime();
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

    #region Slows
    public void AddSlow(float multiplier, float duration)
    {
        StartCoroutine(StrongestSlowRoutine(multiplier, duration));
    }

    IEnumerator StrongestSlowRoutine(float multiplier, float duration)
    {
        // add this slow
        activeSlows.Add(multiplier);
        RecalculateSlows();

        yield return new WaitForSeconds(duration);

        // remove this slow
        activeSlows.Remove(multiplier);
        RecalculateSlows();
    }

    void RecalculateSlows()
    {
        if (activeSlows.Count == 0)
        {
            // no slows active
            slowMultiplierTotal = 1f;
        }
        else
        {
            // strongest slow = lowest multiplier
            slowMultiplierTotal = 1f;

            float strongest = 1f;
            foreach (float m in activeSlows)
            {
                if (m < strongest)
                    strongest = m;
            }

            slowMultiplierTotal = strongest;
        }

        ApplySlowToRuntime();
    }

    void ApplySlowToRuntime()
    {
        moveSpeedRuntime = baseSpeedRuntime * slowMultiplierTotal;
    }
    #endregion

    void OnDrawGizmos()
    {
        CapsuleCollider c = GetComponent<CapsuleCollider>();
        if (!c) return;

        Gizmos.color = Color.yellow;

        Vector3 pos = transform.position + c.center;

        float height = c.height;
        float radius = c.radius;

        Vector3 top = pos + Vector3.up * (height * 0.5f - radius);
        Vector3 bottom = pos - Vector3.up * (height * 0.5f - radius);

        // draw top and bottom balls
        Gizmos.DrawWireSphere(top, radius);
        Gizmos.DrawWireSphere(bottom, radius);

        // cylinder lines
        Gizmos.DrawLine(top + Vector3.right * radius, bottom + Vector3.right * radius);
        Gizmos.DrawLine(top - Vector3.right * radius, bottom - Vector3.right * radius);

        Gizmos.DrawLine(top + Vector3.forward * radius, bottom + Vector3.forward * radius);
        Gizmos.DrawLine(top - Vector3.forward * radius, bottom - Vector3.forward * radius);

        // stand room rays
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
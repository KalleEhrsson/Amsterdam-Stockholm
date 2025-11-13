using System.Collections;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float jumpForce = 5f;

    [Header("Ground Settings")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundDistance = 0.2f;

    [Header("Jump Timing")]
    [SerializeField] float jumpBufferTime = 0.15f;
    [SerializeField] float coyoteTime = 0.1f;

    Rigidbody rb;
    bool isGrounded;
    float jumpBufferCounter;
    float coyoteCounter;
    
    void Start()
    {
        rb  = GetComponent<Rigidbody>();
    }

    void Update()
    {
        jumpBufferCounter = Input.GetButtonDown("Jump")
            ? jumpBufferTime
            : Mathf.Max(jumpBufferCounter - Time.deltaTime, 0f);
        
    }
    
    void FixedUpdate()
    {
        #region Horizontal Movement
        float moveInput = Input.GetAxis("Horizontal");
        Vector3 move = new Vector3(moveInput * moveSpeed, rb.linearVelocity.y, 0f);
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
    }
}
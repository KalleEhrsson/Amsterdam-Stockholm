using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.2f;

    [Header("Jump Timing")]
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private float coyoteTime = 0.1f;

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
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        { 
            jumpBufferCounter -= Time.deltaTime;
        }
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

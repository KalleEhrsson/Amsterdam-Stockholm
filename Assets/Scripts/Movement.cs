using UnityEngine;

public class Movement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    
    Rigidbody rb;
    bool isGrounded;
    
    void Start()
    {
        rb  = GetComponent<Rigidbody>();
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
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, 0f);
        }
        #endregion
    }
}

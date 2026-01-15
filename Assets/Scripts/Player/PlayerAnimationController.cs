using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerAnimationController : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Animator animator;
    [SerializeField] private Movement movement;

    [Header("Animator Parameters")]
    [SerializeField] private string stateParam = "State";
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string facingRightParam = "FacingRight";
    [SerializeField] private string verticalVelocityParam = "VerticalVelocity";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string landTrigger = "Land";
    [SerializeField] private string moveXParam = "MoveX";
    [SerializeField] private string idleXParam = "IdleX";
    #endregion

    #region Cached
    private SpriteRenderer spriteRenderer;
    #endregion

    #region Unity
    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<Movement>();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (movement == null)
            movement = GetComponent<Movement>();
    }

    private void OnEnable()
    {
        if (movement == null)
            return;

        movement.OnJump += HandleJump;
        movement.OnLand += HandleLand;
    }

    private void OnDisable()
    {
        if (movement == null)
            return;

        movement.OnJump -= HandleJump;
        movement.OnLand -= HandleLand;
    }

    private void Update()
    {
        if (animator == null || movement == null)
            return;

        animator.SetInteger(stateParam, (int)movement.CurrentState);

        animator.SetFloat(speedParam, movement.HorizontalSpeedNormalized);
        animator.SetFloat(verticalVelocityParam, movement.VerticalVelocity);

        animator.SetBool(groundedParam, movement.IsGrounded);
        animator.SetBool(facingRightParam, movement.FacingRight);

        animator.SetFloat(moveXParam, movement.MoveX);
        animator.SetFloat(idleXParam, movement.IdleX);
    }
    #endregion

    #region Animation Events
    private void HandleJump()
    {
        if (animator != null)
            animator.SetTrigger(jumpTrigger);
    }

    private void HandleLand()
    {
        if (animator != null)
            animator.SetTrigger(landTrigger);
    }
    #endregion
}
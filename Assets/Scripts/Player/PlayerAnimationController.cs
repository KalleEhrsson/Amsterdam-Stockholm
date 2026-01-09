using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Movement component that provides state and events. Auto-found if null.")]
    [SerializeField] private Movement movement;

    [Tooltip("Animator to drive. Will be auto-found on this GameObject if left empty.")]
    [SerializeField] private Animator animator;

    [Header("Animator Parameters")]
    [SerializeField] private string animStateParam = "State";
    [SerializeField] private string animSpeedParam = "Speed";
    [SerializeField] private string animFacingParam = "FacingRight";
    [SerializeField] private string animJumpTrigger = "Jump";
    [SerializeField] private string animLandTrigger = "Land";

    private int stateHash;
    private int speedHash;
    private int facingHash;
    private int jumpHash;
    private int landHash;

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<Movement>() ?? GetComponentInParent<Movement>();

        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        stateHash = Animator.StringToHash(animStateParam);
        speedHash = Animator.StringToHash(animSpeedParam);
        facingHash = Animator.StringToHash(animFacingParam);
        jumpHash = Animator.StringToHash(animJumpTrigger);
        landHash = Animator.StringToHash(animLandTrigger);
    }

    private void OnEnable()
    {
        if (movement != null)
        {
            movement.OnJump += HandleJump;
            movement.OnLand += HandleLand;
            movement.OnLeaveGround += HandleLeaveGround;
        }
    }

    private void OnDisable()
    {
        if (movement != null)
        {
            movement.OnJump -= HandleJump;
            movement.OnLand -= HandleLand;
            movement.OnLeaveGround -= HandleLeaveGround;
        }
    }

    private void Update()
    {
        if (movement == null || animator == null)
            return;

        // Drive animator parameters each frame for smooth blends.
        animator.SetInteger(stateHash, (int)movement.CurrentState);
        animator.SetFloat(speedHash, movement.HorizontalSpeedNormalized);
        animator.SetBool(facingHash, movement.FacingRight);
    }

    private void HandleJump()
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(animJumpTrigger))
            animator.SetTrigger(jumpHash);
    }

    private void HandleLand()
    {
        if (animator == null) return;
        if (!string.IsNullOrEmpty(animLandTrigger))
            animator.SetTrigger(landHash);
    }

    private void HandleLeaveGround()
    {
        // Currently no specific animation trigger for leaving ground.
    }
}

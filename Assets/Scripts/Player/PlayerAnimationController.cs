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
    [SerializeField] private string jumpTrigger = "Jump";
    [SerializeField] private string landTrigger = "Land";

    [Header("Visual Flip")]
    [SerializeField] private Transform visual;
    [SerializeField] private bool flipByScale = true;
    [SerializeField] private bool flipSpriteRendererInstead = false;
    #endregion

    #region Cached
    private SpriteRenderer spriteRenderer;
    #endregion

    #region Unity
    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        movement = GetComponent<Movement>();

        FindVisual();
    }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (movement == null)
            movement = GetComponent<Movement>();

        FindVisual();

        if (spriteRenderer == null && visual != null)
            spriteRenderer = visual.GetComponent<SpriteRenderer>();
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
        animator.SetBool(facingRightParam, movement.FacingRight);
    }

    private void LateUpdate()
    {
        ApplyFacingFlip();
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

    #region Visual
    private void FindVisual()
    {
        if (visual != null)
            return;

        Transform found = transform.Find("Visual");
        visual = found != null ? found : transform;
    }

    private void ApplyFacingFlip()
    {
        if (movement == null || visual == null)
            return;

        bool facingRight = movement.FacingRight;

        if (flipSpriteRendererInstead)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = !facingRight;

            return;
        }

        if (!flipByScale)
            return;

        Vector3 scale = visual.localScale;
        float x = Mathf.Abs(scale.x);
        scale.x = facingRight ? x : -x;
        visual.localScale = scale;
    }
    #endregion
}
using UnityEngine;

[DisallowMultipleComponent]
public sealed class MovementOneShotBridge : MonoBehaviour
{
    #region Inspector
    [SerializeField] private Movement movement;
    [SerializeField] private OneShotAudio oneShots;

    [Header("Event -> OneShotId")]
    [SerializeField] private OneShotId jumpId = OneShotId.Jump;
    [SerializeField] private OneShotId landId = OneShotId.Land;
    #endregion

    #region Unity
    private void Reset()
    {
        movement = GetComponent<Movement>();
        oneShots = GetComponentInChildren<OneShotAudio>(true);
    }

    private void Awake()
    {
        if (movement == null)
            movement = GetComponent<Movement>();

        if (oneShots == null)
            oneShots = GetComponentInChildren<OneShotAudio>(true);
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
    #endregion

    #region Handlers
    private void HandleJump()
    {
        if (oneShots != null)
            oneShots.Play(jumpId);
    }

    private void HandleLand()
    {
        if (oneShots != null)
            oneShots.Play(landId);
    }
    #endregion
}
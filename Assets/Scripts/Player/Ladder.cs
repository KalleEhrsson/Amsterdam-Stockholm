using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class Ladder : MonoBehaviour
{
    #region Inspector
    [Header("Ladder")]
    [SerializeField] private float climbSpeed = 4.5f;
    [SerializeField] private Transform snapOverride;
    [SerializeField] private Transform topMarker;
    [SerializeField] private Transform bottomMarker;
    #endregion

    #region Public API
    public float ClimbSpeed => climbSpeed;
    #endregion

    #region Unity Lifecycle
    private void Reset()
    {
        Collider ladderCollider = GetComponent<Collider>();
        if (ladderCollider != null)
            ladderCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Movement movement = ResolveMovement(other);
        if (movement == null)
            return;

        movement.EnterLadder(this);
    }

    private void OnTriggerExit(Collider other)
    {
        Movement movement = ResolveMovement(other);
        if (movement == null)
            return;

        movement.ExitLadder();
    }
    #endregion

    #region Snap And Bounds
    public Vector3 GetSnappedPosition(Vector3 playerPosition)
    {
        Vector3 origin = snapOverride != null ? snapOverride.position : transform.position;
        Vector3 ladderUp = transform.up;
        Vector3 projected = origin + Vector3.Project(playerPosition - origin, ladderUp);
        return projected;
    }

    public Vector3 ClampToBounds(Vector3 position)
    {
        Vector3 origin = snapOverride != null ? snapOverride.position : transform.position;
        Vector3 ladderUp = transform.up;

        float axis = Vector3.Dot(position - origin, ladderUp);
        float minAxis = GetBottomAxis(origin, ladderUp);
        float maxAxis = GetTopAxis(origin, ladderUp);

        if (minAxis > maxAxis)
        {
            (minAxis, maxAxis) = (maxAxis, minAxis);
        }

        float clampedAxis = Mathf.Clamp(axis, minAxis, maxAxis);
        return origin + ladderUp * clampedAxis;
    }

    public void AlignPlayerYaw(Transform playerTransform)
    {
        if (playerTransform == null)
            return;

        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (flatForward.sqrMagnitude < 0.0001f)
            return;

        playerTransform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
    }
    #endregion

    #region Helpers
    private Movement ResolveMovement(Collider other)
    {
        if (other == null)
            return null;

        return other.GetComponentInParent<Movement>();
    }

    private float GetTopAxis(Vector3 origin, Vector3 ladderUp)
    {
        if (topMarker != null)
            return Vector3.Dot(topMarker.position - origin, ladderUp);

        GetAxisBoundsFromCollider(origin, ladderUp, out float minAxis, out float maxAxis);
        return maxAxis;
    }

    private float GetBottomAxis(Vector3 origin, Vector3 ladderUp)
    {
        if (bottomMarker != null)
            return Vector3.Dot(bottomMarker.position - origin, ladderUp);

        GetAxisBoundsFromCollider(origin, ladderUp, out float minAxis, out float maxAxis);
        return minAxis;
    }

    private void GetAxisBoundsFromCollider(Vector3 origin, Vector3 ladderUp, out float minAxis, out float maxAxis)
    {
        Collider ladderCollider = GetComponent<Collider>();
        if (ladderCollider == null)
        {
            minAxis = 0f;
            maxAxis = 0f;
            return;
        }

        Bounds bounds = ladderCollider.bounds;

        Vector3 ext = bounds.extents;
        Vector3 c = bounds.center;

        Vector3[] corners =
        {
            c + new Vector3( ext.x,  ext.y,  ext.z),
            c + new Vector3( ext.x,  ext.y, -ext.z),
            c + new Vector3( ext.x, -ext.y,  ext.z),
            c + new Vector3( ext.x, -ext.y, -ext.z),
            c + new Vector3(-ext.x,  ext.y,  ext.z),
            c + new Vector3(-ext.x,  ext.y, -ext.z),
            c + new Vector3(-ext.x, -ext.y,  ext.z),
            c + new Vector3(-ext.x, -ext.y, -ext.z)
        };

        minAxis = float.PositiveInfinity;
        maxAxis = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            float axis = Vector3.Dot(corners[i] - origin, ladderUp);
            if (axis < minAxis)
                minAxis = axis;
            if (axis > maxAxis)
                maxAxis = axis;
        }
    }
    #endregion
}

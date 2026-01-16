using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public class Ladder : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField, HideInInspector] private bool editorPrevRequireUpInputToAttach;
    [SerializeField, HideInInspector] private bool editorPrevIsTrigger;
#endif

    #region Inspector
    [Header("Attach Mode")]
    [Tooltip("Automatically attaches the player to the ladder when they touch the ladder volume.\nWorks with Trigger or Solid colliders.\nBest for ladders that grab the player when walked into from the side.")]
    [SerializeField] private bool autoAttachOnEnter;

    [Tooltip("Player must press Up (W) while inside the ladder trigger to start climbing.\nManual ladders MUST use IsTrigger = true so the player can walk past them.")]
    [SerializeField] private bool requireUpInputToAttach;

    [Header("Snap")]
    [Tooltip("Optional snap position for the player while on the ladder.\nIf not set, the player snaps to the center of the ladder collider bounds.")]
    [SerializeField] private Transform snapPoint;

    [Header("Front-Only Attach")]
    [Tooltip("Minimum dot product required to attach from the ladder front. Higher = narrower cone.")]
    [SerializeField, Range(-1f, 1f)] private float frontAttachDotThreshold = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool ladderDebugGizmos;
    [SerializeField] private float ladderDebugGizmoSurfaceOffset = 0.1f;
    #endregion

    #region Runtime
    private Collider ladderCollider;
    #endregion

    #region Unity
    private void Awake()
    {
        ladderCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!autoAttachOnEnter)
            return;

        TryAutoAttach(other);
    }

    private void OnTriggerExit(Collider other)
    {
        Movement m = other.GetComponentInParent<Movement>();
        if (m == null)
            return;

        m.NotifyLadderExit(this);
    }

    private void OnTriggerStay(Collider other)
    {
        if (autoAttachOnEnter)
            return;

        if (!requireUpInputToAttach)
            return;

        Movement m = other.GetComponentInParent<Movement>();
        if (m == null)
            return;

        if (m.IsOnLadder)
            return;

        float v = Input.GetAxisRaw("Vertical");
        if (v > 0.5f)
            m.TryAttachToLadder(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Solid-collider auto ladders: attach via collision
        if (!autoAttachOnEnter)
            return;

        // If it's a trigger collider, collision callbacks won't be relevant anyway.
        TryAutoAttach(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        Movement m = collision.collider.GetComponentInParent<Movement>();
        if (m == null)
            return;

        m.NotifyLadderExit(this);
    }
    #endregion

    #region Attach Helpers
    private void TryAutoAttach(Collider other)
    {
        Movement m = other.GetComponentInParent<Movement>();
        if (m == null)
            return;

        if (m.IsOnLadder)
            return;

        m.TryAttachToLadder(this);
    }
    #endregion

    #region Public
    public bool CanAttachFrom(Vector3 playerWorldPos)
    {
        Vector3 axis = GetLadderUp();

        Vector3 basePoint = snapPoint != null
            ? snapPoint.position
            : ladderCollider != null
                ? ladderCollider.bounds.center
                : transform.position;

        Vector3 toPlayer = playerWorldPos - basePoint;
        Vector3 planar = Vector3.ProjectOnPlane(toPlayer, axis);

        if (planar.sqrMagnitude < 0.0001f)
            return true;

        planar.Normalize();
        float dot = Vector3.Dot(GetLadderForward(), planar);
        return dot >= frontAttachDotThreshold;
    }


    public Vector3 GetSnapWorldPosition(Vector3 playerWorldPos, float surfaceOffset)
    {
        Vector3 axis = GetLadderUp();
        Vector3 forward = GetLadderForward();
        Vector3 basePoint = snapPoint != null
            ? snapPoint.position
            : ladderCollider != null
                ? ladderCollider.bounds.center
                : transform.position;

        Vector3 toPlayer = playerWorldPos - basePoint;
        float along = Vector3.Dot(toPlayer, axis);
        Vector3 spinePoint = basePoint + axis * along;

        float halfDepth = 0f;

        if (ladderCollider != null)
        {
            Bounds b = ladderCollider.bounds;
            Vector3 absForward = new Vector3(Mathf.Abs(forward.x), Mathf.Abs(forward.y), Mathf.Abs(forward.z));
            halfDepth = Vector3.Dot(b.extents, absForward);
        }

        return spinePoint + forward * (halfDepth + surfaceOffset);
    }
    
    public Bounds GetWorldBounds()
    {
        return ladderCollider != null ? ladderCollider.bounds : new Bounds(transform.position, Vector3.zero);
    }
    
    public Vector3 GetClosestPoint(Vector3 worldPos)
    {
        if (ladderCollider == null)
            return transform.position;

        return ladderCollider.ClosestPoint(worldPos);
    }

    public Vector3 GetClimbAxis()
    {
        return GetLadderUp();
    }

    #region Orientation

    public Vector3 GetLadderUp()
    {
        // Pick the local axis that points most upward in world space
        Vector3[] axes =
        {
            transform.up,
            transform.right,
            transform.forward
        };

        Vector3 best = axes[0];
        float bestDot = Vector3.Dot(best.normalized, Vector3.up);

        for (int i = 1; i < axes.Length; i++)
        {
            float d = Vector3.Dot(axes[i].normalized, Vector3.up);
            if (d > bestDot)
            {
                bestDot = d;
                best = axes[i];
            }
        }

        return best.normalized;
    }

    public Vector3 GetLadderForward()
    {
        Vector3 axis = GetLadderUp();

        // 2.5D: ladder front must be on ±X, not ±Z.
        Vector3 forward = Vector3.ProjectOnPlane(Vector3.right, axis);

        // Fallback if axis is almost parallel to X
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.ProjectOnPlane(Vector3.forward, axis);

        forward.Normalize();

        // Pick the side that "faces upward" on inclined ladders
        if (Vector3.Dot(-forward, Vector3.up) > Vector3.Dot(forward, Vector3.up))
            forward = -forward;

        return forward;
    }

    /// <summary>
    /// Rough hint for which way is "out" from the ladder,
    /// based on collider bounds.
    /// </summary>
    private Vector3 GetOutwardHint()
    {
        if (ladderCollider == null)
            return transform.forward;

        Bounds b = ladderCollider.bounds;
        Vector3 centerToSurface = (b.center - transform.position);
        centerToSurface -= GetLadderUp() * Vector3.Dot(centerToSurface, GetLadderUp());

        if (centerToSurface.sqrMagnitude < 0.0001f)
            return transform.forward;

        return centerToSurface.normalized;
    }

    #endregion

    
    public Vector3 GetTopPoint()
    {
        if (ladderCollider == null)
            return transform.position;

        Bounds b = ladderCollider.bounds;
        Vector3 axis = GetLadderUp();

        Vector3 c = b.center;
        Vector3 e = b.extents;

        Vector3 best = c;
        float bestDot = float.NegativeInfinity;

        for (int xi = -1; xi <= 1; xi += 2)
        for (int yi = -1; yi <= 1; yi += 2)
        for (int zi = -1; zi <= 1; zi += 2)
        {
            Vector3 p = c + new Vector3(e.x * xi, e.y * yi, e.z * zi);
            float d = Vector3.Dot(p, axis);
            if (d > bestDot)
            {
                bestDot = d;
                best = p;
            }
        }

        return best;
    }
    #endregion

    #region Gizmos
    private void OnDrawGizmosSelected()
    {
        if (!ladderDebugGizmos)
            return;

        if (ladderCollider == null)
            ladderCollider = GetComponent<Collider>();

        Vector3 forward = GetLadderForward();
        Vector3 basePoint = snapPoint != null
            ? snapPoint.position
            : ladderCollider != null
                ? ladderCollider.bounds.center
                : transform.position;

        Vector3 snap = GetSnapWorldPosition(basePoint, ladderDebugGizmoSurfaceOffset);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(snap, 0.05f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(snap, snap + forward * 0.35f);
    }
    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider c = GetComponent<Collider>();

        bool isTriggerNow = c != null && c.isTrigger;
        bool requireManualNow = requireUpInputToAttach;

        // Mutual exclusion (popup)
        if (autoAttachOnEnter && requireUpInputToAttach)
        {
            autoAttachOnEnter = false;

            EditorUtility.DisplayDialog(
                "Invalid Ladder Configuration",
                "A ladder cannot use both Auto Attach and Require Up Input at the same time.\n\n" +
                "These modes conflict:\n" +
                "• Auto Attach: grabs the player when touched\n" +
                "• Require Up Input: allows walking past and climbing only when pressing W\n\n" +
                "Auto Attach has been disabled.",
                "OK"
            );
        }

        // Manual ladder requires trigger (popup only on transition to invalid)
        bool wasInvalidManual = editorPrevRequireUpInputToAttach && !editorPrevIsTrigger;
        bool isInvalidManualNow = requireManualNow && !isTriggerNow;

        if (isInvalidManualNow && !wasInvalidManual)
        {
            EditorUtility.DisplayDialog(
                "Manual Ladder Needs Trigger",
                $"'{name}' is configured as a manual (press W) ladder, but its collider is NOT set to Trigger.\n\n" +
                "Manual ladders MUST use IsTrigger = true so the player can walk past them while still being able to press W to start climbing.\n\n" +
                "Auto-attach ladders do NOT require a trigger (they may be solid or trigger).",
                "OK"
            );

            Debug.LogWarning(
                $"[Ladder] '{name}' is a manual (press W) ladder but collider IsTrigger is OFF. Manual ladders require IsTrigger = true.",
                this
            );
        }

        editorPrevRequireUpInputToAttach = requireManualNow;
        editorPrevIsTrigger = isTriggerNow;
    }
#endif
}
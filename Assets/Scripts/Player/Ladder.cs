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
    public Vector3 GetSnapWorldPosition()
    {
        if (snapPoint != null)
            return snapPoint.position;

        if (ladderCollider != null)
            return ladderCollider.bounds.center;

        return transform.position;
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
        return transform.up.normalized;
    }
    
    public Vector3 GetTopPoint()
    {
        if (ladderCollider == null)
            return transform.position;

        Bounds b = ladderCollider.bounds;
        Vector3 axis = GetClimbAxis();

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
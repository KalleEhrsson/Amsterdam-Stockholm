using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class Ladder : MonoBehaviour
{
    #region Inspector
    [Header("Ladder")] [SerializeField] private float ladderCenterX; // Where the player should snap horizontally while climbing (local X by default)
    [SerializeField] private bool useWorldX; // If true, ladderCenterX is world X
    [SerializeField] private float snapSpeed = 25f; // How quickly the player snaps to the ladder center
    #endregion

    private void Reset()
    {
        var c = GetComponent<Collider>();
        c.isTrigger = true;
    }

    private float GetCenterX()
    {
        return useWorldX ? ladderCenterX : transform.TransformPoint(new Vector3(ladderCenterX, 0f, 0f)).x;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent(out Movement climber))
            return;

        climber.SetLadder(this, GetCenterX(), snapSpeed);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.TryGetComponent(out Movement climber))
            return;

        climber.ClearLadder(this);
    }
}
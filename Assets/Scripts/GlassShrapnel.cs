using UnityEngine;

public class GlassShrapnel : MonoBehaviour
{
    [Header("Slow")]
    [SerializeField] float slowMultiplier = 0.5f;
    [SerializeField] float slowDuration = 2f;

    void OnTriggerStay(Collider other)
    {
        Movement movement = other.GetComponent<Movement>();
        if (movement != null)
        {
            movement.AddSlow(slowMultiplier, slowDuration);
        }
    }
}
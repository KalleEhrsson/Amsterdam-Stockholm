using Unity.VisualScripting;
using UnityEngine;

public class ItemDropInCarage : MonoBehaviour
{
    public GameObject DroppableItem;
    public GameManager gamemanager;
    public Canvas canvas;

    private void Start()
    {
        ResolveGameManager();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Room2Ground"))
        {
            if (gamemanager != null)
            {
                gamemanager.itemdropped_in_correct_room();
            }
            else
            {
                Debug.LogWarning("ItemDropInCarage: GameManager reference is null when item dropped.");
            }

            if (DroppableItem != null)
            {
                Destroy(DroppableItem);
            }
            else
            {
                Debug.LogWarning("ItemDropInCarage: DroppableItem is null when trying to destroy.");
            }

            if (canvas != null)
            {
                canvas.enabled = false;
            }
        }
        else
        {
            if (canvas != null)
            {
                canvas.enabled = true;
            }
        }
    }

    private void ResolveGameManager()
    {
        if (gamemanager == null)
        { 
            // Try finding any GameManager in the scenegamemanager = FindFirstObjectByType<GameManager>();
        }

        if (gamemanager == null)
        {
            Debug.LogError("ItemDropInCarage: Could not find a GameManager in the scene. Please assign it in the inspector.",this);
        }
    }
}

using Unity.VisualScripting;
using UnityEngine;

public class ItemDropInCarage : MonoBehaviour
{
    public GameObject DroppableItem;
    public GameObject itemcol;
    public GameManager gamemanager;
    public Canvas canvas;

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
        }
        else
        {
            canvas.enabled = true;
        }    
    }
    void Start()
    {
        // If gamemanager set in inspector, keep it. Otherwise try several safe lookups.
        if (gamemanager == null)
        {
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj != null)
            {
                gamemanager = gmObj.GetComponent<GameManager>();
            }
        }

        if (gamemanager == null)
        {
            // Try finding any GameManager in the scene
            gamemanager = FindObjectOfType<GameManager>();
        }

        if (gamemanager == null)
        {
            Debug.LogError("ItemDropInCarage: Could not find a GameManager in the scene. Please assign it in the inspector or name the object 'GameManager'.", this);
        }
    }
}

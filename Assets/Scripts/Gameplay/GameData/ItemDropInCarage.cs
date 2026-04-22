using UnityEngine;

public class ItemDropInCarage : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public Canvas canvas;

    [Header("Item Settings")]
    public int levelIndex = 1;
    public int itemIndex;

    private bool hasTriggered = false;

    // =========================
    // TRIGGER ONLY (cleaner)
    // =========================
    private void OnTriggerEnter(Collider other)
    {
        HandleDrop(other.gameObject);
    }

    private void HandleDrop(GameObject other)
    {
        if (hasTriggered) return;
        if (other == null) return;

        if (!other.CompareTag("Room2Ground")) return;

        hasTriggered = true;

        if (canvas != null)
            canvas.enabled = true;

        Debug.Log($"Drop detected on {name}");

        // Get ItemData from the ACTUAL object that hit the ground
        ItemData itemData = GetComponent<ItemData>();
        if (itemData == null)
            itemData = GetComponentInChildren<ItemData>();

        int value = itemData != null ? itemData.value : 0;

        Debug.Log($"Item value = {value}");

        // Send to GameManager
        if (gameManager != null)
        {
            gameManager.CollectItem(levelIndex, itemIndex, value);
        }
        else
        {
            Debug.LogWarning("GameManager not assigned!");
        }

        // Destroy THIS object safely
        Destroy(gameObject);
    }

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("No GameManager found in scene!", this);
        }
    }
}
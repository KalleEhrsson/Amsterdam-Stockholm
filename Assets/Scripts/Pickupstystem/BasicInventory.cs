using System.Collections.Generic;
using UnityEngine;

public class BasicInventory : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public string itemName;
        public int value;
        public GameObject prefab; // used when dropping
    }

    public List<InventoryItem> items = new List<InventoryItem>();

    [Tooltip("How far in front of the player the item will be dropped")]
    public float dropDistance = 1.2f;

    [Tooltip("Upward offset applied when dropping so item does not collide with ground")]
    public float dropUpOffset = 0.5f;

    [Tooltip("Optional transform used as the origin & forward direction for drops (e.g. camera or a child object). If null, the player transform is used.")]
    public Transform dropOrigin;

    [Tooltip("Use the main camera's forward when dropping (if available). This is checked only if dropOrigin is null.")]
    public bool useCameraForward = false;

    [Tooltip("Key used to drop the last item in inventory")]
    public KeyCode dropKey = KeyCode.Q;

    [Tooltip("If true the item will be removed from the inventory when dropped. If false the item stays in the list so it can be picked up again.")]
    public bool removeItemOnDrop = false;
    
    public bool IsHoldingItem => items.Count > 0;
    public ClickablePickup CurrentPickupCandidate => ClickablePickup.CurrentHovered;
    public bool HasPickupCandidate => CurrentPickupCandidate != null && CurrentPickupCandidate.HasItemData;

    // Check input for dropping
    private void Update()
    {
        if (Input.GetKeyDown(dropKey))
        {
            DropLast();
        }
    }

    // Try to pick up an item. The picked gameObject should have an ItemData component.
    public bool PickUp(GameObject pickable)
    {
        if (pickable == null) return false;

        ItemData data = pickable.GetComponent<ItemData>();
        if (data == null) return false;

        InventoryItem it = new InventoryItem();
        it.itemName = data.itemName;
        it.value = data.value;
        // Prefer an explicit prefab set on the ItemData; otherwise use the object passed in
        it.prefab = data.dropPrefab != null ? data.dropPrefab : pickable;

        items.Add(it);

        // remove from scene after pickup
        Destroy(pickable);

        Debug.Log($"Picked up: {it.itemName} (value {it.value})");
        return true;
    }

    // Drop the last item in inventory in front of the player
    public bool DropLast()
    {
        if (items.Count == 0) return false;
        return DropAtIndex(items.Count - 1);
    }

    // Drop a specific inventory index
    public bool DropAtIndex(int index)
    {
        if (index < 0 || index >= items.Count) return false;

        InventoryItem it = items[index];

        // determine origin and forward vector for dropping
        Vector3 originPos = transform.position;
        Vector3 forward = transform.forward;
        if (dropOrigin != null)
        {
            originPos = dropOrigin.position;
            forward = dropOrigin.forward;
        }
        else if (useCameraForward && Camera.main != null)
        {
            originPos = Camera.main.transform.position;
            forward = Camera.main.transform.forward;
        }

        Vector3 dropPos = originPos + forward.normalized * dropDistance + Vector3.up * dropUpOffset;

        if (it.prefab != null)
        {
            // align spawned object's forward to the drop forward so it appears in front
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            GameObject go = Instantiate(it.prefab, dropPos, rot);

            // Ensure the dropped object has ItemData so it can be picked up again
            ItemData id = go.GetComponent<ItemData>();
            if (id == null)
            {
                id = go.AddComponent<ItemData>();
            }
            id.itemName = it.itemName;
            id.value = it.value;
            // set dropPrefab to the prefab used so future drops keep the same prefab
            id.dropPrefab = it.prefab;

            // try to add a small forward impulse if it has a rigidbody
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(forward.normalized * 1.5f + Vector3.up * 0.5f, ForceMode.Impulse);
            }
        }

        Debug.Log($"Dropped: {it.itemName} (value {it.value})");
        if (removeItemOnDrop)
        {
            items.RemoveAt(index);
        }
        return true;
    }

    // Convenience: total value of inventory
    public int TotalValue()
    {
        int sum = 0;
        for (int i = 0; i < items.Count; i++) sum += items[i].value;
        return sum;
    }

    // Auto-pickup for trigger colliders that have ItemData
    private void OnTriggerEnter(Collider other)
    {
        ItemData data = other.GetComponent<ItemData>();
        if (data != null)
        {
            PickUp(other.gameObject);
        }
    }
}


using UnityEngine;
using UnityEngine.EventSystems;

// Make 3D objects clickable to pick them up using the BasicInventory system.
[RequireComponent(typeof(Collider))]
public class ClickablePickup : MonoBehaviour, IPointerClickHandler
{
    public static ClickablePickup CurrentHovered { get; private set; }
    public bool HasItemData => GetComponent<ItemData>() != null;
    
    [Tooltip("Optional reference to the player inventory. If null, will search for one at Start().")]
    public BasicInventory inventory;

    #region Unity Lifecycle
    
    private void Start()
    {
        if (inventory == null)
        {
            inventory = FindObjectOfType<BasicInventory>();
        }
    }
    
    private void OnMouseEnter()
    {
        CurrentHovered = this;
    }

    private void OnMouseExit()
    {
        if (CurrentHovered == this)
        {
            CurrentHovered = null;
        }
    }

    private void OnDisable()
    {
        if (CurrentHovered == this)
        {
            CurrentHovered = null;
        }
    }

    #endregion

    #region Pickup
    
    // Called by Unity when using a physics raycast click (legacy OnMouseDown). Works if object has a Collider.
    private void OnMouseDown()
    {
        TryPickup();
    }

    // Called when using the EventSystem (UI raycast) on this GameObject.
    public void OnPointerClick(PointerEventData eventData)
    {
        TryPickup();
    }

    private void TryPickup()
    {
        if (inventory == null)
        {
            Debug.LogWarning("ClickablePickup: No BasicInventory found to pick up item.");
            return;
        }

        // Use the same PickUp method so game logic stays consistent
        bool ok = inventory.PickUp(gameObject);
        if (!ok)
        {
            Debug.LogWarning($"ClickablePickup: Pickup failed for {gameObject.name}. Ensure ItemData is attached.");
        }
        
        if (!HasItemData)
        {
            Debug.LogWarning($"ClickablePickup: {gameObject.name} has ClickablePickup but no ItemData.");
            return;
        }
    }
    
    #endregion
}

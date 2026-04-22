using UnityEngine;

// Attach this to pickable objects. It describes the item's name, value and an optional prefab to instantiate when dropped.
public class ItemData : MonoBehaviour
{
    [Tooltip("Display name of the item")]
    public string itemName = "Item";

    [Tooltip("Numeric value of the item")]
    public int value = 1;

    [Tooltip("Optional prefab to use when dropping. If not set the original object will be instantiated.")]
    public GameObject dropPrefab;
}

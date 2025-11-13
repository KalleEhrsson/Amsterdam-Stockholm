using Unity.VisualScripting;
using UnityEngine;

public class ItemCollecting : MonoBehaviour
{
    [SerializeField] bool isCollected = false;
    [SerializeField] GameObject itemObject;

    private void Start()
    {
        if (itemObject == null)
        {
            itemObject = this.gameObject;
        }
    }
    
}

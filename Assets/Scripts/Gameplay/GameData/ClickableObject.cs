using UnityEngine;
using System.Collections.Generic;

public class ClickableObject : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private MeshRenderer meshR;

    [Header("Materials")]
    [SerializeField] private List<Material> materials;
    [SerializeField] private List<Material> clickableMaterials;

    [SerializeField] private int itemID;
    [SerializeField] private int itemLevel;

    void Start()
    {
        meshR = GetComponent<MeshRenderer>();

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
    }

    private void OnMouseEnter()
    {
        if (meshR != null)
            meshR.SetMaterials(clickableMaterials);
    }

    private void OnMouseExit()
    {
        if (meshR != null)
            meshR.SetMaterials(materials);
    }

    private void OnMouseDown()
    {
        if (gameManager != null)
        {
            gameManager.CollectItem(itemLevel, itemID);
        }
        else
        {
            Debug.LogWarning("GameManager not assigned!");
        }

        Destroy(gameObject);
    }
}
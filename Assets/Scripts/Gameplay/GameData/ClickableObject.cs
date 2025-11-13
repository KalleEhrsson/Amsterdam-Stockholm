using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

public class ClickableObject : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    private MeshRenderer meshR;

    [Header("Materials")]
    [SerializeField] private List<Material> outlineMaterials;
    [SerializeField] private List<Material> normalMaterials;

    [SerializeField] private int itemID;
    [SerializeField] private int itemLevel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshR = gameObject.GetComponent<MeshRenderer>();
    }
    private void OnMouseEnter()
    {
        meshR.SetMaterials(outlineMaterials);
    }

    private void OnMouseExit()
    {
        meshR.SetMaterials(normalMaterials);
    }

    private void OnMouseDown()
    {
        gameManager.CollectItem(itemLevel, itemID);
        Destroy(this.gameObject);
    }
}

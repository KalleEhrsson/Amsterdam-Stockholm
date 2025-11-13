using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class ClickableObject : MonoBehaviour
{
    private MeshRenderer meshR;

    [Header("Materials")]
    [SerializeField] private List<Material> outlineMaterials;
    [SerializeField] private List<Material> normalMaterials;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meshR = gameObject.GetComponent<MeshRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        
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
        Destroy(this.gameObject);
    }
}

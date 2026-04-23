using UnityEngine;
using UnityEditor;

public class AssignMaterialToChildren : EditorWindow
{
    private Material materialToAssign;
    private GameObject parentObject;

    [MenuItem("Tools/Assign Material To Children")]
    public static void ShowWindow()
    {
        GetWindow<AssignMaterialToChildren>("Assign Material To Children");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign Material to All Children", EditorStyles.boldLabel);

        parentObject = (GameObject)EditorGUILayout.ObjectField(
            "Parent Object", parentObject, typeof(GameObject), true);

        materialToAssign = (Material)EditorGUILayout.ObjectField(
            "Material", materialToAssign, typeof(Material), false);

        EditorGUILayout.Space();

        if (GUILayout.Button("Assign Material"))
        {
            if (parentObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Parent Object.", "OK");
                return;
            }
            if (materialToAssign == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Material.", "OK");
                return;
            }

            AssignMaterial();
        }
    }

    private void AssignMaterial()
    {
        Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            EditorUtility.DisplayDialog("Result", "No Renderers found in children.", "OK");
            return;
        }

        Undo.RecordObjects(renderers, "Add Material To Children");

        int count = 0;
        foreach (Renderer renderer in renderers)
        {
            // Skip if this renderer already has the material assigned
            if (System.Array.IndexOf(renderer.sharedMaterials, materialToAssign) != -1)
                continue;

            // Append the new material to the existing array
            Material[] existing = renderer.sharedMaterials;
            Material[] updated = new Material[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[updated.Length - 1] = materialToAssign;
            renderer.sharedMaterials = updated;

            count++;
        }

        EditorUtility.DisplayDialog(
            "Success", $"Material added to {count} renderer(s). {renderers.Length - count} already had it.", "OK");

        Debug.Log($"[AssignMaterialToChildren] Added '{materialToAssign.name}' " +
                  $"to {count} child renderer(s) under '{parentObject.name}'.");
    }
}
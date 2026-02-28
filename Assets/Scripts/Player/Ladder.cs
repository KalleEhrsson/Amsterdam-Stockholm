using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Collider))]
public sealed class SimpleLadder : MonoBehaviour
{
    #region Inspector
    private readonly float climbSpeed = 4f;
    private readonly float snapOffsetX = 0.45f;
    #endregion

    #region Public API
    public float ClimbSpeed => climbSpeed;
    public float SnapOffsetX => snapOffsetX;
    #endregion

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SimpleLadder))]
public sealed class SimpleLadderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SimpleLadder ladder = (SimpleLadder)target;

        Vector3 e = ladder.transform.eulerAngles;

        float x = NormalizeAngle(e.x);
        float z = NormalizeAngle(e.z);

        bool isRotated = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;

        if (isRotated)
        {
            EditorGUILayout.HelpBox(
                $"Ladder must be straight.\nSet rotation to (0, any, 0).\nCurrent X={x:F2}°, Z={z:F2}°",
                MessageType.Error
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Ladder is straight.",
                MessageType.Info
            );
        }

        DrawDefaultInspector();
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360f;
        if (degrees > 180f) degrees -= 360f;
        return degrees;
    }
}
#endif
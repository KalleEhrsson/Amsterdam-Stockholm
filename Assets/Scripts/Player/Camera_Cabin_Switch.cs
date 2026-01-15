using UnityEngine;

public class Camera_Cabin_Switch : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera Cabincar1;
    [SerializeField] private Camera Cabincar2;

    [Header("Linked Scripts")]
    [SerializeField] private GameManager gamemanager;

    [Header("transition settings")]
    [SerializeField] private float transitionDuration = 1.0f;
    private bool isCabin1Active = true;
    [SerializeField] private AnimationCurve transitionCurve;

    private void Start()
    {
        isCabin1Active = true;
    }

    public void transistionevent()
    {
        if (isCabin1Active)
        {
            Debug.Log("Switching to Cabincar 2");
            isCabin1Active = false;
            StartCoroutine(SmoothTransition(Cabincar1, Cabincar2, transitionDuration));
        }
        else
        {
            StartCoroutine(SmoothTransition(Cabincar2, Cabincar1, transitionDuration));
        }
        isCabin1Active = !isCabin1Active;
    }
    private System.Collections.IEnumerator SmoothTransition(Camera fromCam, Camera toCam, float duration)
    {
        float elapsed = 0f;

        toCam.enabled = true;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curveValue = transitionCurve.Evaluate(t);

            fromCam.fieldOfView = Mathf.Lerp(60f, 20f, curveValue);
            toCam.fieldOfView = Mathf.Lerp(20f, 60f, curveValue);

            elapsed += Time.deltaTime;
            yield return null;
        }

        fromCam.enabled = false;
        fromCam.fieldOfView = 60f;
        toCam.fieldOfView = 60f;
    }

}

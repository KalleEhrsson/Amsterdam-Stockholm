using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PlaceableItem : MonoBehaviour
{
    [Header("Validation")]
    [SerializeField] private string placementKey;
    [SerializeField] private string correctFloorTag = "Room2Ground";
    [SerializeField] private LayerMask correctFloorLayers;
    [SerializeField] private bool useLayerMaskValidation;
    [SerializeField] private float settleDelay = 0.2f;
    [SerializeField] private float checkInterval = 0.15f;
    [SerializeField] private int checksAfterDrop = 4;
    [SerializeField] private float groundCheckDistance = 2f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool drawDebugRay = true;

    [Header("Runtime")]
    [SerializeField] private bool isCorrectlyPlaced;
    [SerializeField] private bool hasBeenCounted;

    private bool isHeld;
    private Coroutine validationRoutine;

    public bool IsCorrectlyPlaced => isCorrectlyPlaced;
    public string PlacementKey => placementKey;

    #region Unity Lifecycle

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(placementKey))
        {
            ItemData itemData = GetComponent<ItemData>();
            placementKey = itemData != null && !string.IsNullOrWhiteSpace(itemData.itemName) ? itemData.itemName : gameObject.name;
        }
    }

    private void OnEnable()
    {
        EnsureManager();
        PlacementManager.Instance?.RegisterTrackedItem(this);
    }

    private void OnDisable()
    {
        if (PlacementManager.Instance != null)
        {
            PlacementManager.Instance.UnregisterTrackedItem(this);
        }
    }

    #endregion

    #region Pickup Drop Hooks

    public void OnPickedUp()
    {
        isHeld = true;

        if (validationRoutine != null)
        {
            StopCoroutine(validationRoutine);
            validationRoutine = null;
        }

        Log($"Picked up object: {name}");

        if (isCorrectlyPlaced || hasBeenCounted)
        {
            isCorrectlyPlaced = false;
            hasBeenCounted = false;
            PlacementManager.Instance?.SetPlacementState(this, false);
        }
    }

    public void OnDropped()
    {
        isHeld = false;

        if (validationRoutine != null)
        {
            StopCoroutine(validationRoutine);
        }

        Log($"Dropped object: {name}");
        validationRoutine = StartCoroutine(ValidateAfterDropRoutine());
    }

    #endregion

    #region Validation

    private IEnumerator ValidateAfterDropRoutine()
    {
        yield return new WaitForSeconds(settleDelay);

        bool latestValid = false;
        string latestSurface = "None";

        for (int i = 0; i < checksAfterDrop; i++)
        {
            if (isHeld)
            {
                yield break;
            }

            latestValid = TryDetectSurface(out latestSurface);
            if (latestValid)
            {
                break;
            }

            if (i < checksAfterDrop - 1)
            {
                yield return new WaitForSeconds(checkInterval);
            }
        }

        Log($"Surface detected: {latestSurface}");
        Log($"Drop result: {(latestValid ? "VALID" : "INVALID")}");

        if (latestValid)
        {
            if (!hasBeenCounted)
            {
                hasBeenCounted = true;
                isCorrectlyPlaced = true;
                PlacementManager.Instance?.SetPlacementState(this, true);
            }
        }
        else
        {
            if (hasBeenCounted || isCorrectlyPlaced)
            {
                hasBeenCounted = false;
                isCorrectlyPlaced = false;
                PlacementManager.Instance?.SetPlacementState(this, false);
            }
        }

        validationRoutine = null;
    }

    private bool TryDetectSurface(out string surfaceName)
    {
        surfaceName = "None";

        Vector3 origin = transform.position + Vector3.up * 0.2f;
        Vector3 direction = Vector3.down;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, groundCheckDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            surfaceName = hit.collider.name;

            if (drawDebugRay)
            {
                Debug.DrawRay(origin, direction * hit.distance, Color.green, 1.5f);
            }

            bool tagMatch = !string.IsNullOrEmpty(correctFloorTag) && hit.collider.CompareTag(correctFloorTag);
            bool layerMatch = useLayerMaskValidation && ((correctFloorLayers.value & (1 << hit.collider.gameObject.layer)) != 0);

            return tagMatch || layerMatch;
        }

        if (drawDebugRay)
        {
            Debug.DrawRay(origin, direction * groundCheckDistance, Color.red, 1.5f);
        }

        return false;
    }

    #endregion

    #region Helpers

    private void EnsureManager()
    {
        if (PlacementManager.Instance != null) return;

        PlacementManager manager = FindFirstObjectByType<PlacementManager>();
        if (manager == null)
        {
            GameObject managerObject = new GameObject("PlacementManager");
            manager = managerObject.AddComponent<PlacementManager>();
            Log("Created PlacementManager automatically.");
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[Placement] {message}", this);
    }

    #endregion
}
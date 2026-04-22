using System.Collections.Generic;
using UnityEngine;

public class PlacementManager : MonoBehaviour
{
    public static PlacementManager Instance { get; private set; }

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Runtime")]
    [SerializeField] private int requiredObjects;
    [SerializeField] private int correctlyPlacedObjects;
    [SerializeField] private bool levelComplete;

    private readonly HashSet<PlaceableItem> trackedItems = new HashSet<PlaceableItem>();
    private readonly HashSet<string> requiredKeys = new HashSet<string>();
    private readonly HashSet<string> countedKeys = new HashSet<string>();

    public bool LevelComplete => levelComplete;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RebuildTrackedItems();
    }

    #endregion

    #region Registration

    public void RegisterTrackedItem(PlaceableItem item)
    {
        if (item == null) return;
        trackedItems.Add(item);

        string key = item.PlacementKey;
        if (!string.IsNullOrWhiteSpace(key))
        {
            requiredKeys.Add(key);
        }

        requiredObjects = requiredKeys.Count;
        Log($"Tracking item: {item.name} ({correctlyPlacedObjects}/{requiredObjects})");
    }

    public void UnregisterTrackedItem(PlaceableItem item)
    {
        if (item == null) return;
        if (!trackedItems.Remove(item)) return;

        string key = item.PlacementKey;
        if (!string.IsNullOrWhiteSpace(key) && countedKeys.Contains(key) && !AnyTrackedItemCorrectForKey(key))
        {
            countedKeys.Remove(key);
            correctlyPlacedObjects = Mathf.Max(0, correctlyPlacedObjects - 1);
            Log($"Unregistered: {item.name} ({correctlyPlacedObjects}/{requiredObjects})");
        }

        requiredObjects = requiredKeys.Count;
        EvaluateCompletion();
    }

    public void SetPlacementState(PlaceableItem item, bool isPlaced)
    {
        if (item == null) return;
        string key = item.PlacementKey;
        if (string.IsNullOrWhiteSpace(key)) return;

        if (isPlaced)
        {
            if (countedKeys.Add(key))
            {
                correctlyPlacedObjects++;
                Log($"Registered: {item.name} ({correctlyPlacedObjects}/{requiredObjects})");
            }
        }
        else
        {
            if (countedKeys.Remove(key))
            {
                correctlyPlacedObjects = Mathf.Max(0, correctlyPlacedObjects - 1);
                Log($"Unregistered: {item.name} ({correctlyPlacedObjects}/{requiredObjects})");
            }
        }

        EvaluateCompletion();
    }

    #endregion

    #region Completion

    private void EvaluateCompletion()
    {
        bool nextComplete = requiredObjects > 0 && correctlyPlacedObjects >= requiredObjects;
        if (nextComplete != levelComplete)
        {
            levelComplete = nextComplete;
            Log($"Level complete = {levelComplete}");
        }
    }

    [ContextMenu("Placement/Print Status")]
    public void PrintStatus()
    {
        Debug.Log($"[Placement] Status: Placed {correctlyPlacedObjects}/{requiredObjects}, LevelComplete={levelComplete}", this);
    }

    [ContextMenu("Placement/Rebuild Tracked Items")]
    public void RebuildTrackedItems()
    {
        trackedItems.Clear();
        requiredKeys.Clear();
        countedKeys.Clear();

        PlaceableItem[] found = FindObjectsByType<PlaceableItem>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++)
        {
            trackedItems.Add(found[i]);
            if (!string.IsNullOrWhiteSpace(found[i].PlacementKey))
            {
                requiredKeys.Add(found[i].PlacementKey);
            }
            if (found[i].IsCorrectlyPlaced)
            {
                if (!string.IsNullOrWhiteSpace(found[i].PlacementKey))
                {
                    countedKeys.Add(found[i].PlacementKey);
                }
            }
        }

        requiredObjects = requiredKeys.Count;
        correctlyPlacedObjects = countedKeys.Count;
        EvaluateCompletion();

        Log($"Rebuild complete: Placed {correctlyPlacedObjects}/{requiredObjects}");
    }

    #endregion

    #region Helpers

    private void Log(string message)
    {
        if (!enableDebugLogs) return;
        Debug.Log($"[Placement] {message}", this);
    }

    private bool AnyTrackedItemCorrectForKey(string key)
    {
        foreach (PlaceableItem trackedItem in trackedItems)
        {
            if (trackedItem == null) continue;
            if (trackedItem.PlacementKey != key) continue;
            if (trackedItem.IsCorrectlyPlaced) return true;
        }

        return false;
    }

    #endregion
}
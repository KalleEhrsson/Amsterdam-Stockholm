using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelData
    {
        public List<GameObject> items;
        public Text statusText;
        public Text valueText;
        public int valueTarget;

        [HideInInspector] public List<bool> collected;
        [HideInInspector] public int collectedCount;
        [HideInInspector] public int totalValue;
        [HideInInspector] public bool completed;
    }

    [Header("Levels")]
    [SerializeField] private LevelData level1;
    [SerializeField] private LevelData level2;
    [SerializeField] private LevelData level3;

    private LevelData[] levels;

    [Header("Doors (MUST match level order)")]
    [SerializeField] private GameObject[] levelDoors;

    [Header("Train Barriers (optional)")]
    [SerializeField] private GameObject[] trainBarrierLevel;

    void Start()
    {
        levels = new LevelData[] { level1, level2, level3 };

        // Init levels safely
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].items == null)
            {
                Debug.LogWarning($"Level {i + 1} has no items assigned!");
                continue;
            }

            levels[i].collected = new List<bool>(new bool[levels[i].items.Count]);
            levels[i].collectedCount = 0;
            levels[i].totalValue = 0;
            levels[i].completed = false;

            UpdateLevelUI(levels[i], i);
        }
    }

    // =========================
    // MAIN COLLECT FUNCTION
    // =========================
    public void CollectItem(int levelIndex, int itemIndex, int value = 0)
    {
        if (levelIndex < 0 || levelIndex >= levels.Length) return;

        LevelData level = levels[levelIndex];

        if (level.completed) return;

        if (level.collected == null || itemIndex >= level.collected.Count) return;

        if (!level.collected[itemIndex])
        {
            level.collected[itemIndex] = true;
            level.collectedCount++;
            level.totalValue += value;

            Debug.Log($"Collected item {itemIndex} in level {levelIndex + 1}");

            UpdateLevelUI(level, levelIndex);
        }
    }

    // =========================
    // LEVEL UPDATE + COMPLETION
    // =========================
    private void UpdateLevelUI(LevelData level, int index)
    {
        if (level.statusText != null)
        {
            level.statusText.text =
                $"Level {index + 1}: {level.collectedCount}/{level.items.Count}";
        }

        if (level.valueText != null)
        {
            level.valueText.text = $"Value: {level.totalValue}";
        }

        bool valueOK = (level.valueTarget == 0 || level.totalValue >= level.valueTarget);
        bool itemsOK = (level.collectedCount >= level.items.Count);

        Debug.Log($"Level {index + 1} check → Items:{itemsOK} Value:{valueOK}");

        if (!level.completed && itemsOK && valueOK)
        {
            level.completed = true;

            Debug.Log($"LEVEL {index + 1} COMPLETED!");

            // Disable text
            if (level.statusText != null)
                level.statusText.enabled = false;

            if (level.valueText != null)
                level.valueText.enabled = false;

            // REMOVE DOOR (FORCE)
            if (levelDoors != null && index < levelDoors.Length)
            {
                if (levelDoors[index] != null)
                {
                    Debug.Log($"Disabling door: {levelDoors[index].name}");
                    levelDoors[index].SetActive(false);
                }
                else
                {
                    Debug.LogWarning($"Door at index {index} is NULL");
                }
            }
            else
            {
                Debug.LogWarning("LevelDoors array not set or too small");
            }

            // OPTIONAL barrier removal
            if (trainBarrierLevel != null && index < trainBarrierLevel.Length)
            {
                if (trainBarrierLevel[index] != null)
                    trainBarrierLevel[index].SetActive(false);
            }
        }
    }
}
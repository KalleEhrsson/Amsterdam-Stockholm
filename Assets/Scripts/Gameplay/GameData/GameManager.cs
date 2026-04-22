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
        public GameObject statusUI;
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

    [Header("Train Barriers")]
    [SerializeField] private GameObject[] trainBarrierLevel;

    void Start()
    {
        levels = new LevelData[] { level1, level2, level3 };

        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].items == null) continue;

            levels[i].collected = new List<bool>(new bool[levels[i].items.Count]);
            levels[i].collectedCount = 0;
            levels[i].totalValue = 0;
            levels[i].completed = false;
        }

        if (trainBarrierLevel.Length > 0) trainBarrierLevel[0].SetActive(true);
        if (trainBarrierLevel.Length > 1) trainBarrierLevel[1].SetActive(true);
        if (trainBarrierLevel.Length > 2) trainBarrierLevel[2].SetActive(true);
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

            UpdateLevelUI(level, levelIndex);
        }
    }

    // =========================
    // UI UPDATE
    // =========================
    private void UpdateLevelUI(LevelData level, int index)
    {
        if (level.statusText != null)
        {
            level.statusText.text =
                $"Level {index + 1}: {level.collectedCount}/{level.items.Count} (Value: {level.totalValue})";
        }

        if (level.valueText != null)
        {
            level.valueText.text = $"Value: {level.totalValue}";
        }

        bool valueOK = (level.valueTarget == 0 || level.totalValue >= level.valueTarget);

        if (level.collectedCount >= level.items.Count && valueOK && !level.completed)
        {
            level.completed = true;

            if (level.statusText != null)
                level.statusText.text += " - Completed!";

            if (trainBarrierLevel.Length > index && trainBarrierLevel[index] != null)
                trainBarrierLevel[index].SetActive(false);

            // Activate next level UI
            if (index + 1 < levels.Length)
            {
                if (levels[index].statusUI != null)
                    levels[index].statusUI.SetActive(false);

                if (levels[index + 1].statusUI != null)
                    levels[index + 1].statusUI.SetActive(true);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]


public class GameManager : MonoBehaviour
{
    [Header("Level 1 items")]

    public List<GameObject> level1Items;
    public List<bool> level1ItemsCollected;
    public Text level1StatusText;
    private int level1TotalItems;
    private int level1CollectedCount;
    private bool level1Completed;

    [Header("Level 2 items")]
    public List<GameObject> level2Items;
    public List<bool> level2ItemsCollected;
    public Text level2StatusText;
    private int level2TotalItems;
    private int level2CollectedCount;
    private bool level2Completed;

    [Header("Level 3 items")]
    public List<GameObject> level3Items;
    public List<bool> level3ItemsCollected;
    public Text level3StatusText;
    private int level3TotalItems;
    private int level3CollectedCount;
    private bool level3Completed;

    void Start()
    {
        // Initialize Level 1
        level1TotalItems = level1Items.Count;
        level1ItemsCollected = new List<bool>(new bool[level1TotalItems]);
        level1CollectedCount = 0;
        level1Completed = false;
        UpdateLevel1Status();

        // Initialize Level 2
        level2TotalItems = level2Items.Count;
        level2ItemsCollected = new List<bool>(new bool[level2TotalItems]);
        level2CollectedCount = 0;
        level2Completed = false;
        UpdateLevel2Status();

        // Initialize Level 3
        level3TotalItems = level3Items.Count;
        level3ItemsCollected = new List<bool>(new bool[level3TotalItems]);
        level3CollectedCount = 0;
        level3Completed = false;
        UpdateLevel3Status();
    }
    public void CollectItem(int level, int itemIndex)
    {
        switch (level)
        {
            case 1:
                if (!level1ItemsCollected[itemIndex])
                {
                    level1ItemsCollected[itemIndex] = true;
                    level1CollectedCount++;
                    UpdateLevel1Status();
                }
                break;
            case 2:
                if (!level2ItemsCollected[itemIndex])
                {
                    level2ItemsCollected[itemIndex] = true;
                    level2CollectedCount++;
                    UpdateLevel2Status();
                }
                break;
            case 3:
                if (!level3ItemsCollected[itemIndex])
                {
                    level3ItemsCollected[itemIndex] = true;
                    level3CollectedCount++;
                    UpdateLevel3Status();
                }
                break;
        }
    }
    private void UpdateLevel1Status()
    {
        level1StatusText.text = $"Level 1 Items Collected: {level1CollectedCount}/{level1TotalItems}";
        if (level1CollectedCount >= level1TotalItems && !level1Completed)
        {
            level1Completed = true;
            level1StatusText.text += " - Level 1 Completed!";
        }
    }
    private void UpdateLevel2Status() {
        level2StatusText.text = $"Level 2 Items Collected: {level2CollectedCount}/{level2TotalItems}";
        if (level2CollectedCount >= level2TotalItems && !level2Completed)
        {
            level2Completed = true;
            level2StatusText.text += " - Level 2 Completed!";
        }
    }
    private void UpdateLevel3Status() {
        level3StatusText.text = $"Level 3 Items Collected: {level3CollectedCount}/{level3TotalItems}";
        if (level3CollectedCount >= level3TotalItems && !level3Completed)
        {
            level3Completed = true;
            level3StatusText.text += " - Level 3 Completed!";
        }
    }


}


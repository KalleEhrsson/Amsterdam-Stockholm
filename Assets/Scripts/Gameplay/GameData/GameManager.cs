using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]


public class GameManager : MonoBehaviour
{
    [Header("Level 1 items")]

    [SerializeField] private List<GameObject> level1Items;
    [SerializeField] private List<bool> level1ItemsCollected;
    [SerializeField] private Text level1StatusText;
    [SerializeField] private GameObject level1statustestobj;
    private int level1TotalItems;
    private int level1CollectedCount;
    private bool level1Completed;

    [Header("Level 2 items")]
    [SerializeField] private List<GameObject> level2Items;
    [SerializeField] private List<bool> level2ItemsCollected;
    [SerializeField] private Text level2StatusText;
    [SerializeField] private GameObject level2statustestobj;
    private int level2TotalItems;
    private int level2CollectedCount;
    private bool level2Completed;

    [Header("Level 3 items")]
    [SerializeField] private List<GameObject> level3Items;
    [SerializeField] private List<bool> level3ItemsCollected;
    [SerializeField] private Text level3StatusText;
    [SerializeField] private GameObject level3statustestobj;
    private int level3TotalItems;
    private int level3CollectedCount;
    private bool level3Completed;

    [Header("TrainBariers")]
    [SerializeField] private GameObject[] trainBarrierLevel;

    void Start()
    {
        // Initialize Level 1
        level1TotalItems = level1Items.Count;
        level1ItemsCollected = new List<bool>(new bool[level1TotalItems]);
        level1CollectedCount = 0;
        level1Completed = false;
        trainBarrierLevel[0].SetActive(true);
        level1statustestobj.SetActive(true);
        UpdateLevel1Status();

        // Initialize Level 2
        level2TotalItems = level2Items.Count;
        level2ItemsCollected = new List<bool>(new bool[level2TotalItems]);
        level2CollectedCount = 0;
        level2Completed = false;
        trainBarrierLevel[1].SetActive(true);
        level2statustestobj.SetActive(false);
        UpdateLevel2Status();

        // Initialize Level 3
        level3TotalItems = level3Items.Count;
        level3ItemsCollected = new List<bool>(new bool[level3TotalItems]);
        level3CollectedCount = 0;
        level3Completed = false;
        trainBarrierLevel[2].SetActive(true);
        level3statustestobj.SetActive(false);
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
            trainBarrierLevel[0].SetActive(false);
            level1statustestobj.SetActive(false);
            level2statustestobj.SetActive(true);
        }
    }
    private void UpdateLevel2Status() {
        level2StatusText.text = $"Level 2 Items Collected: {level2CollectedCount}/{level2TotalItems}";
        if (level2CollectedCount >= level2TotalItems && !level2Completed)
        {
            level2Completed = true;
            level2StatusText.text += " - Level 2 Completed!";
            trainBarrierLevel[1].SetActive(false);
            level2statustestobj.SetActive(false);
            level3statustestobj.SetActive(true);
        }
    }
    private void UpdateLevel3Status() {
        level3StatusText.text = $"Level 3 Items Collected: {level3CollectedCount}/{level3TotalItems}";
        if (level3CollectedCount >= level3TotalItems && !level3Completed)
        {
            level3Completed = true;
            level3StatusText.text = "All level Completed, Head over to the Train Operator";
            trainBarrierLevel[2].SetActive(false);
        }
    }


}


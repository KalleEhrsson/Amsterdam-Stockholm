using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]


public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    [SerializeField] GameObject[] tasks;
    [SerializeField] bool[] taskCompletionStatus;
    List<string> Tasks = new List<string>();

    [Header("ui stuff")]
    [SerializeField] Text[] texttaskslist;

    private void Start()
    {
        foreach (GameObject task in tasks)
        {
            Tasks.Add(task.name);
           //tasks.Length = taskCompletionStatus.Length;
           texttaskslist[0].text = string.Join("\n", Tasks);
        }
        Debug.Log("Tasks Initialized: " + string.Join(", ", Tasks));
    }
    void finishingonetask()
    {
        if (Tasks.Count > 0)
        {
            Tasks.RemoveAt(0);
            Debug.Log("Task completed. Remaining tasks: " + Tasks.Count);
        }
        else
        {
            Debug.Log("All tasks completed!");
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Goals
{
    public string Name;
    public bool Complete;
}

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private List<Goals> goals;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        goals = new List<Goals>
        {
            new Goals { Name = "Find Book", Complete = false }
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

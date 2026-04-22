using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;


public class SpinWheels : MonoBehaviour
{
    [SerializeField] private List<Transform> trainWheels;
    public float rotationSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach(Transform wheel in GetComponentInChildren<Transform>())
        {
            if (wheel.name.Contains("Wheel"))
            {
                trainWheels.Add(wheel);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for(int i = 0; i < trainWheels.Count; i++)
        {
            trainWheels[i].transform.Rotate(0f, 0f, rotationSpeed);
            //trainWheels[i].transform.rotation = new Quaternion(trainWheels[i].transform.rotation.x, trainWheels[i].transform.rotation.y, trainWheels[i].transform.rotation.z + 1f, trainWheels[i].transform.rotation.w);
        }
    }
}

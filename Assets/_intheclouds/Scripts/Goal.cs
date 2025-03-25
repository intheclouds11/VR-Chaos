using System;
using UnityEngine;

public class Goal : MonoBehaviour
{
    public static Goal Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 40f * Time.deltaTime);
    }
}

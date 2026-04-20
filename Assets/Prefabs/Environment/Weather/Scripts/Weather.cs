using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weather : MonoBehaviour
{

    [Header("Wind")]
    [Range(1, 25)]
    public int amount = 10;
    [Range(1, 10)]
    public int speed = 1;
    public float amplitude = 0.1f;
    
    void Start()
    {

    }

    void Update()
    {

    }
}

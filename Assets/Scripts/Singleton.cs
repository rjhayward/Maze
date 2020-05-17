using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton : MonoBehaviour
{

    public Maze mazePrefab;
    [HideInInspector]
    public int numberOfMazes;

    [HideInInspector]
    public bool mazeNeedsUpdate;
    void Start()
    {
        numberOfMazes = 0;
    }
}

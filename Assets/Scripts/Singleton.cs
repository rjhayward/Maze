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

    [HideInInspector]
    public bool toAddNewMaze;

    [HideInInspector]
    public int mazeIndex;


    void Start()
    {
        mazeIndex = 0;
        numberOfMazes = 0;
    }
}

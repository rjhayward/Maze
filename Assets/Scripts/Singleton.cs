using UnityEngine;

public class Singleton : MonoBehaviour
{
    public enum GameState
    {
        PreGame = 0,
        PostGame = 1,
        InGame = 2,
        Paused = 3,
        GameOver = 4
    }

    public Ship ship;

    public Maze mazePrefab;

    [HideInInspector]
    public int numberOfMazes;

    [HideInInspector]
    public bool mazeNeedsUpdate;

    [HideInInspector]
    public bool toAddNewMaze;

    [HideInInspector]
    public int mazeIndex;

    [HideInInspector]
    public Vector3 shipLocation;

    [HideInInspector]
    public GameState gameState;

    [HideInInspector]
    public int sessionHighScore;

    void Start()
    {
        mazeIndex = 0;
        numberOfMazes = 0;
    }
}

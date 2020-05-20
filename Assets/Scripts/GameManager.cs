using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;
using UnityEngine.UI;
using Unity.Entities.UniversalDelegates;

public class GameManager : MonoBehaviour
{
    public Mesh shipMesh;
    public Material shipMaterial;
    public Font font;

    Singleton singleton;

    EntityManager entityManager;
    PipeCases.PipeConnections[] caseArray;
    EntityArchetype mazeArchetype;
    EntityArchetype shipArchetype;
    PipeCases pipeCasesField;


    // Start is called before the first frame update
    void Start()
    {
        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();
        pipeCasesField = GameObject.Find("Singleton").GetComponent<PipeCases>();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        shipArchetype = entityManager.CreateArchetype(
            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(Translation),
            typeof(Rotation),
            typeof(IsShip)
        );

        mazeArchetype = entityManager.CreateArchetype(
            typeof(IntBufferElement), //Triangles
            typeof(Float3BufferElement), //Vertices
            typeof(Translation),
            typeof(ToCreate),
            typeof(NeedsMeshUpdate),
            typeof(PipeCaseComponent) //this entity's PipeCase
        );

        ResetGame();
        singleton.gameState = Singleton.GameState.PreGame;

        CreateShipEntity();

        singleton.mazeIndex = 0;
        singleton.numberOfMazes = 0;
        singleton.toAddNewMaze = false;
        singleton.sessionHighScore = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (singleton.gameState == Singleton.GameState.GameOver)
        {
            if (PlayerPrefs.HasKey("HighScore"))
            {
                if ((singleton.mazeIndex - 1) > PlayerPrefs.GetInt("HighScore"))
                {
                    singleton.sessionHighScore = singleton.mazeIndex - 1;
                    PlayerPrefs.SetInt("HighScore", (singleton.mazeIndex - 1));
                }
            }
            else if ((singleton.mazeIndex - 1) > singleton.sessionHighScore)
            {
                singleton.sessionHighScore = singleton.mazeIndex - 1;
                PlayerPrefs.SetInt("HighScore", (singleton.mazeIndex - 1));
            }

            singleton.gameState = Singleton.GameState.PostGame;
        }
        if (singleton.gameState == Singleton.GameState.InGame)
        {
            UnityEngine.Cursor.visible = false;
            if (singleton.toAddNewMaze)
            {
                singleton.toAddNewMaze = false;
                generateNewMaze(GetNewRandomMazeArray());
            }
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                singleton.gameState = Singleton.GameState.Paused;
            }
        }
        else if (singleton.gameState == Singleton.GameState.PreGame)
        {
            UnityEngine.Cursor.visible = true;
            if (singleton.toAddNewMaze)
            {
                singleton.toAddNewMaze = false;
                generateNewMaze(GetNewRandomMazeArray());
            }
        }
        else if (singleton.gameState == Singleton.GameState.PostGame)
        {
            UnityEngine.Cursor.visible = true;
        }
        else if (singleton.gameState == Singleton.GameState.Paused)
        {
            UnityEngine.Cursor.visible = true;
        }
    }

    protected void OnGUI()
    {
        if (PlayerPrefs.HasKey("HighScore"))
        {
            if (PlayerPrefs.GetInt("HighScore") > singleton.sessionHighScore)
            {
                GameObject.Find("HighScore").GetComponent<Text>().text = "High Score: " + PlayerPrefs.GetInt("HighScore");
            }
            else
            {
                GameObject.Find("HighScore").GetComponent<Text>().text = "High Score: " + singleton.sessionHighScore;
            }
        }
        else
        {
            GameObject.Find("HighScore").GetComponent<Text>().text = "High Score: " + singleton.sessionHighScore;
        }

        if (singleton.gameState == Singleton.GameState.InGame)
        {
            Vector3 mazeExit = new Vector3(PipeCases.torusRadius * 16, 0, PipeCases.torusRadius * 32 * singleton.mazeIndex - 1);
            float maxDistance = math.sqrt(math.pow(PipeCases.torusRadius * 32, 2) + math.pow(PipeCases.torusRadius * 16, 2));
            float distanceFromExit = (mazeExit - singleton.shipLocation).magnitude;

            float normalisedDistance = distanceFromExit / maxDistance;
            normalisedDistance -= 1f;

            shipMaterial.SetColor("_Color", new Color(Mathf.Clamp(normalisedDistance, 0f, 1f), Mathf.Clamp(1f - normalisedDistance, 0f, 1f), 0f));

            int level = singleton.mazeIndex - 1;

            GameObject.Find("Level").GetComponent<Text>().text = "Level: " + level;
        }
        else if (singleton.gameState == Singleton.GameState.PreGame)
        {
            Vector3 mazeExit = new Vector3(PipeCases.torusRadius * 16, 0, PipeCases.torusRadius * 32 * singleton.mazeIndex - 1);
            float maxDistance = math.sqrt(math.pow(PipeCases.torusRadius * 32, 2) + math.pow(PipeCases.torusRadius * 16, 2));
            float distanceFromExit = (mazeExit - singleton.shipLocation).magnitude;

            float normalisedDistance = distanceFromExit / maxDistance;
            normalisedDistance -= 1f;

            shipMaterial.SetColor("_Color", new Color(Mathf.Clamp(normalisedDistance, 0f, 1f), Mathf.Clamp(1f - normalisedDistance, 0f, 1f), 0f));

            GameObject.Find("Tubular").GetComponent<Text>().text = "tubular";
            GameObject.Find("BelowText").GetComponent<Text>().text = "How to play:\n\nUse the A and D keys (or the arrow keys) to navigate the maze without touching any walls.\n\nPress the Spacebar for a speed boost!\n\n\nBackground track:\nPipedreaming by Ryan Hayward\n(Press M to mute/unmute)";

            Rect buttonPos = new Rect((Screen.width / 2.0f) - 100, (Screen.height / 2.0f) - 50, 200, 50);

            GUIStyle styleBtn = GUI.skin.button;

            styleBtn.font = font;

            styleBtn.fontSize = 30;
            styleBtn.fixedHeight = 75;

            if (GUI.Button(buttonPos, "Play", styleBtn) || Input.GetKeyDown(KeyCode.Space))
            {
                GameObject.Find("Tubular").GetComponent<Text>().text = "";
                GameObject.Find("BelowText").GetComponent<Text>().text = "";
                ResetGame();
                StartGame();
            }
        }
        else if (singleton.gameState == Singleton.GameState.PostGame)
        {
            GameObject.Find("Tubular").GetComponent<Text>().text = "game over";
            GameObject.Find("BelowText").GetComponent<Text>().text = "How to play:\n\nUse the A and D keys (or the arrow keys) to navigate the maze without touching any walls.\n\n Press the Spacebar for a speed boost!\n\n\nBackground track:\nPipedreaming by Ryan Hayward\n(Press M to mute/unmute)";

            Rect buttonPos = new Rect((Screen.width / 2.0f) - 150, (Screen.height / 2.0f) - 50, 300, 50);

            GUIStyle styleBtn = GUI.skin.button;

            styleBtn.font = font;

            styleBtn.fontSize = 30;
            styleBtn.fixedHeight = 75;

            if (GUI.Button(buttonPos, "Play Again", styleBtn) || Input.GetKeyDown(KeyCode.Space))
            {
                GameObject.Find("Tubular").GetComponent<Text>().text = "";
                GameObject.Find("BelowText").GetComponent<Text>().text = "";
                ResetGame();
                StartGame();
            }
        }
        else if (singleton.gameState == Singleton.GameState.Paused)
        {
            Time.timeScale = 0.0f;
            GameObject.Find("Tubular").GetComponent<Text>().text = "Paused";

            Rect buttonPos = new Rect((Screen.width / 2.0f) - 150, (Screen.height / 2.0f) - 50, 300, 50);

            GUIStyle styleBtn = GUI.skin.button;

            styleBtn.font = font;

            styleBtn.fontSize = 30;
            styleBtn.fixedHeight = 75;

            if (GUI.Button(buttonPos, "Resume", styleBtn) || Input.GetKeyDown(KeyCode.Space))
            {
                Time.timeScale = 1.0f;
                GameObject.Find("Tubular").GetComponent<Text>().text = "";
                singleton.gameState = Singleton.GameState.InGame;
            }
        }

    }
    void StartGame()
    {
        // create entity archetypes
        CreateShipEntity();

        singleton.mazeIndex = 0;
        singleton.numberOfMazes = 0;
        singleton.toAddNewMaze = false;
        singleton.gameState = Singleton.GameState.InGame;

    }
    void ResetGame()
    {
        NativeArray<Entity> allEntities = entityManager.GetAllEntities();
        entityManager.DestroyEntity(allEntities);
        GameObject[] mazeWallArray = GameObject.FindGameObjectsWithTag("MazeWall");

        for (int i = 0; i < mazeWallArray.Length; i++)
        {
            Destroy(mazeWallArray[i]);
        }
    }
    void CreateShipEntity()
    {
        Entity shipEntity = entityManager.CreateEntity(shipArchetype);

        entityManager.SetSharedComponentData(shipEntity, new RenderMesh
        {
            mesh = shipMesh,
            material = shipMaterial
        });

        entityManager.SetComponentData(shipEntity, new Rotation
        {
            Value = Quaternion.Euler(0, 0, 0)
        });

        entityManager.SetComponentData(shipEntity, new Translation
        {
            Value = new Vector3(PipeCases.torusRadius * 16, 0f, PipeCases.torusRadius * 3f)
        });
    }

    void generateNewMaze(int[,] mazeArray2d)
    {
        PipeCases.PipeConnections[,] caseArray2d = PipeCases.GetCaseArray(mazeArray2d);

        caseArray = new PipeCases.PipeConnections[256];

        int index = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                caseArray[index] = caseArray2d[x % 16, y % 16];
                index++;
            }
        }

        // max amount of mazeEntities (16*16 = 256)
        NativeArray<Entity> mazeEntities = new NativeArray<Entity>(256, Allocator.Temp);

        entityManager.CreateEntity(mazeArchetype, mazeEntities);

        for (int i = 0; i < mazeEntities.Length; i++)
        {
            PipeCases.PipeConnections pipeCase = caseArray[i];

            entityManager.SetComponentData(mazeEntities[i], new Translation
            {
                Value = new Vector3(0, 0, PipeCases.torusRadius * 32 * singleton.mazeIndex)
            });

            entityManager.SetComponentData(mazeEntities[i], new ToCreate
            {
                Value = true
            });

            entityManager.SetComponentData(mazeEntities[i], new NeedsMeshUpdate
            {
                Value = true
            });

            entityManager.SetComponentData(mazeEntities[i], new PipeCaseComponent
            {
                Value = (int)pipeCase
            });

            PipeCases.MeshData meshDataForCase = pipeCasesField.GetMeshDataByCase(pipeCase);

            DynamicBuffer <Float3BufferElement> verticesBuffer =  entityManager.AddBuffer<Float3BufferElement>(mazeEntities[i]);
            float3[] verticesForCase = meshDataForCase.vertices;

            if (verticesForCase != null)
            {
                for (int vertexIndex = 0; vertexIndex < verticesForCase.Length; vertexIndex++)
                {
                    verticesBuffer.Add(new Float3BufferElement { Value = verticesForCase[vertexIndex] });
                }
            }
            
            DynamicBuffer < IntBufferElement > trianglesBuffer = entityManager.AddBuffer<IntBufferElement>(mazeEntities[i]);
            int[] trianglesForCase = meshDataForCase.triangles;

            if (trianglesForCase != null)
            {
                for (int triangleIndex = 0; triangleIndex < trianglesForCase.Length; triangleIndex++)
                {
                    trianglesBuffer.Add(new IntBufferElement { Value = trianglesForCase[triangleIndex] });
                }
            }
        }
        mazeEntities.Dispose();
        singleton.mazeIndex++;

        singleton.numberOfMazes = singleton.mazeIndex;
        singleton.mazeNeedsUpdate = true;
    }

    struct Coords
    {
        public int x;
        public int y;
    }
    // Generate a random maze array using a modified version of Prim's shortest path algorithm
    int[,] GetNewRandomMazeArray()
    {
        List<Coords> frontierList = new List<Coords>();
        List<Coords> visitedFrontierList = new List<Coords>();

        int[,] mazeArray = new int[16, 16];
        //{
        //    {0,1,1,1,1,1,0,1,1,1,1,1,0,0,1,0},
        //    {0,1,0,0,0,1,0,0,0,0,0,1,0,1,1,1},
        //    {0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,0},
        //    {0,1,0,1,0,0,1,0,0,0,1,0,0,1,0,0},
        //    {0,0,0,1,0,0,1,0,0,0,1,1,1,1,1,0},
        //    {0,1,1,1,1,1,1,0,0,0,0,0,1,0,1,1},
        //    {0,0,0,0,0,0,1,0,1,1,1,0,0,0,0,0},
        //    {2,1,1,1,1,1,1,0,1,0,1,1,1,1,1,2}, // these edges are guaranteed part of the maze- the rest is generated randomly
        //    {0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0},
        //    {1,1,0,0,0,1,0,1,0,1,1,1,1,1,1,1},
        //    {0,1,1,1,1,1,0,1,0,0,0,0,0,0,0,1},
        //    {0,0,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
        //    {0,1,1,1,0,1,1,1,0,0,0,1,0,0,0,1},
        //    {0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1},
        //    {0,1,0,0,0,1,0,0,0,0,0,1,0,1,0,0},
        //    {0,1,1,1,1,1,1,1,1,1,0,1,0,1,1,1}
        //};

        // fill in with all wall squares
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                mazeArray[y, x] = 0;
            }
        }

        // select first maze square
        mazeArray[8, 2] = 1;
        //fill in frontier squares
        frontierList.Add(new Coords { x = 2, y = 8 + 2 });
        frontierList.Add(new Coords { x = 2, y = 8 - 2 });
        //frontierList.Add(new Coords { x = 2 + 2, y = 8 });

        bool InList(Coords checkSquare, List<Coords> list)
        {
            bool inList = false;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].x == checkSquare.x && list[i].y == checkSquare.y)
                {
                    inList = true;
                }
            }

            if (checkSquare.x == 4 && checkSquare.y == 8) inList = true;
            return inList;
        }

        //int amountOfLoops = 0;

        while (frontierList.Count > 0)
        {
            //pick a frontier square at random
            int randomIndex = UnityEngine.Random.Range(0, frontierList.Count - 1);
            Coords randomFrontierSquare = frontierList[randomIndex];

            //change the square represented by that coordinate to a maze square
            mazeArray[randomFrontierSquare.y, randomFrontierSquare.x] = 1;

            bool hasCreated = false;
            int random = new System.Random().Next(0, 4);

            for (int direction = random; direction < 4 + random; direction++)
            {
                switch (direction % 4)
                {
                    case 0: // west
                        if (randomFrontierSquare.y - 2 >= 2)
                        {
                            if (mazeArray[randomFrontierSquare.y - 2, randomFrontierSquare.x] == 1)
                            {
                                mazeArray[randomFrontierSquare.y - 1, randomFrontierSquare.x] = 1;
                                hasCreated = true;
                            }

                        }
                        break;
                    case 1: // north
                        if (randomFrontierSquare.x - 2 >= 2)
                        {
                            if (mazeArray[randomFrontierSquare.y, randomFrontierSquare.x - 2] == 1)
                            {
                                mazeArray[randomFrontierSquare.y, randomFrontierSquare.x - 1] = 1;
                                hasCreated = true;
                            }
                        }
                        break;
                    case 2: // east
                        if (randomFrontierSquare.y + 2 < 15)
                        {
                            if (mazeArray[randomFrontierSquare.y + 2, randomFrontierSquare.x] == 1)
                            {
                                mazeArray[randomFrontierSquare.y + 1, randomFrontierSquare.x] = 1;
                                hasCreated = true;
                            }
                        }
                        break;
                    case 3: // south
                        if (randomFrontierSquare.x + 2 < 15)
                        {
                            if (mazeArray[randomFrontierSquare.y, randomFrontierSquare.x + 2] == 1)
                            {
                                mazeArray[randomFrontierSquare.y, randomFrontierSquare.x + 1] = 1;
                                hasCreated = true;
                            }
                        }
                        break;
                }

                if (hasCreated) break;
            }

            for (int direction = 0; direction < 4; direction++)
            {
                Coords newFrontierSquare = new Coords();

                switch (direction)
                {
                    case 0: // west
                        newFrontierSquare = new Coords { x = randomFrontierSquare.x - 2, y = randomFrontierSquare.y };
                        break;
                    case 1: // north
                        newFrontierSquare = new Coords { x = randomFrontierSquare.x, y = randomFrontierSquare.y - 2 };
                        break;
                    case 2: // east
                        newFrontierSquare = new Coords { x = randomFrontierSquare.x + 2, y = randomFrontierSquare.y };
                        break;
                    case 3: // south
                        newFrontierSquare = new Coords { x = randomFrontierSquare.x, y = randomFrontierSquare.y + 2 };
                        break;
                }


                if (!InList(newFrontierSquare, frontierList) && !InList(newFrontierSquare, visitedFrontierList) && newFrontierSquare.x >= 2 && newFrontierSquare.x < 15 && newFrontierSquare.y >= 2 && newFrontierSquare.y < 15)
                {
                    visitedFrontierList.Add(newFrontierSquare);
                    frontierList.Add(newFrontierSquare);
                }

            }

            frontierList.RemoveAt(randomIndex);
            visitedFrontierList.Add(randomFrontierSquare);
        }


        // add final square (as it is an evenly sized array)
        mazeArray[8, 15] = 2;
        if (singleton.mazeIndex == 0)
        {
            // add initial square as a start square
            mazeArray[8, 0] = 3;
        }
        else
        {
            // add initial square as a start square
            mazeArray[8, 0] = 2;
        }


        mazeArray[8, 1] = 1;

        return mazeArray;
    }

}

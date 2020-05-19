﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.UIElements;
using System;
using System.CodeDom;

public class GameManager : MonoBehaviour
{

    [SerializeField] private Mesh shipMesh;
    [SerializeField] private Material shipMaterial;

    Singleton singleton;

    EntityManager entityManager;
    PipeCases.PipeConnections[] caseArray;
    EntityArchetype mazeArchetype;

    PipeCases pipeCasesField;
    // Start is called before the first frame update
    void Start()
    {
        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();
        pipeCasesField = GameObject.Find("Singleton").GetComponent<PipeCases>();

        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // create entity archetypes

        // unused
        EntityArchetype projectileArchetype = entityManager.CreateArchetype(

            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(Translation)
        );

        EntityArchetype boidArchetype = entityManager.CreateArchetype(

            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(Translation),
            typeof(ViewAngle),
            typeof(IsAlive)
        );


        // TODO split these into separate functions

        EntityArchetype shipArchetype = entityManager.CreateArchetype(

            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(Translation),
            typeof(Rotation),
            typeof(IsShip)
        );

        Entity shipEntity = entityManager.CreateEntity(shipArchetype);

        entityManager.SetSharedComponentData(shipEntity, new RenderMesh
        {
            mesh = shipMesh,
            material = shipMaterial
        }); 
        
        entityManager.SetComponentData(shipEntity, new Rotation
        {
            Value = Quaternion.Euler(0,0,0)
        });

        entityManager.SetComponentData(shipEntity, new Translation
        {
            Value = new Vector3(PipeCases.torusRadius*14, -1f, 0)
        });

        // TODO split these into separate functions
        mazeArchetype = entityManager.CreateArchetype(
            typeof(IntBufferElement), //Triangles
            typeof(Float3BufferElement), //Vertices
            typeof(Translation),
            typeof(ToCreate),
            typeof(NeedsMeshUpdate),
            typeof(PipeCaseComponent) //this entity's PipeCase
        );

        singleton.toAddNewMaze = true;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mazeExit = new Vector3(PipeCases.torusRadius * 14, 0, PipeCases.torusRadius * 32 * singleton.mazeIndex);

        if (singleton.toAddNewMaze)
        {
            generateNewMaze(GetNewRandomMazeArray());
            singleton.toAddNewMaze = false;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            generateNewMaze(GetNewRandomMazeArray());
        }

        if (Input.GetTouch(0).phase == TouchPhase.Began)
        {
            generateNewMaze(GetNewRandomMazeArray());
        }

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

            PipeCases.MeshData meshDataForCase = GetMeshDataByCase(pipeCase);

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


    PipeCases.MeshData GetMeshDataByCase(PipeCases.PipeConnections pipeCase)
    {
        float3[] vertices;
        int[] triangles;
        switch (pipeCase)
        {
            // Straight cases
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Left | PipeCases.PipeConnections.Right:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Straight, 0).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Straight, 0).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Up | PipeCases.PipeConnections.Down:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Straight, 90).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Straight, 90).triangles.ToArray();
                break;

            // L cases
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Down | PipeCases.PipeConnections.Right:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 0).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 0).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Down | PipeCases.PipeConnections.Left:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 90).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 90).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Up | PipeCases.PipeConnections.Left:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 180).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 180).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Up | PipeCases.PipeConnections.Right:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 270).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.L, 270).triangles.ToArray();
                break;

            // T cases
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Down | PipeCases.PipeConnections.Up | PipeCases.PipeConnections.Right:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 0).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 0).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Left | PipeCases.PipeConnections.Right | PipeCases.PipeConnections.Down:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 90).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 90).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Up | PipeCases.PipeConnections.Down | PipeCases.PipeConnections.Left:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 180).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 180).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Left | PipeCases.PipeConnections.Right | PipeCases.PipeConnections.Up:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 270).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.T, 270).triangles.ToArray();
                break;

            // DeadEnd cases
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Left:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 0).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 0).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Up:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 90).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 90).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Right:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 180).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 180).triangles.ToArray();
                break;
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Down:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 270).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.DeadEnd, 270).triangles.ToArray();
                break;

            // Cross case
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.Up | PipeCases.PipeConnections.Down | PipeCases.PipeConnections.Left | PipeCases.PipeConnections.Right:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Cross, 0).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Cross, 0).triangles.ToArray();
                break;

            //entrance/exit square case
            case PipeCases.PipeConnections.Exists | PipeCases.PipeConnections.End:
                vertices = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Cross, 0).vertices.ToArray();
                triangles = pipeCasesField.GetRotatedMeshDataByCase(PipeCases.PipeCase.Cross, 0).triangles.ToArray();
                break;

            //this will only be reached when there is a single point on the maze without any connections (we will not render these)
            default:
                vertices = null;
                triangles = null;
                break;
            
        }
        return new PipeCases.MeshData {vertices = vertices, triangles = triangles };
    }

    struct Coords
    {
        public int x;
        public int y;
    }

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
        //    {2,1,1,1,1,1,1,0,1,0,1,1,1,1,1,2}, // these edges are guaranteed part of the maze
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
        mazeArray[8, 0] = 1;
        //fill in frontier squares
        frontierList.Add(new Coords { x = 0, y = 8 + 2 });
        frontierList.Add(new Coords { x = 0, y = 8 - 2 });
        frontierList.Add(new Coords { x = 0 + 2, y = 8 });

        //// select last maze square
        //mazeArray[8, 14] = 1;
        ////fill in frontier squares
        //frontierList.Add(new Coords { x = 14, y = 8 + 2 });
        //frontierList.Add(new Coords { x = 14, y = 8 - 2 });
        //frontierList.Add(new Coords { x = 14 - 2, y = 8 });


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
                switch (direction%4)
                {
                    case 0: // west
                        if (randomFrontierSquare.y - 2 >= 0)
                        {
                            if (mazeArray[randomFrontierSquare.y - 2, randomFrontierSquare.x] == 1)
                            {
                                mazeArray[randomFrontierSquare.y - 1, randomFrontierSquare.x] = 1;
                                hasCreated = true;
                            }

                        }
                        break;
                    case 1: // north
                        if (randomFrontierSquare.x - 2 >= 0)
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


                if (!InList(newFrontierSquare, frontierList) && !InList(newFrontierSquare, visitedFrontierList) && newFrontierSquare.x >= 0 && newFrontierSquare.x < 15 && newFrontierSquare.y >= 0 && newFrontierSquare.y < 15)
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

        // change initial square to a start square
        mazeArray[8, 0] = 2;

        return mazeArray;
    }

}

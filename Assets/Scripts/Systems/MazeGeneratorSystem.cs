using UnityEngine;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;
using System.IO.Pipes;
using UnityEngine.UIElements;
using System.Linq;
using System;
using System.Security;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Entities.UniversalDelegates;

using static PipeCases;
using Packages.Rider.Editor;

public class MazeGeneratorSystem : SystemBase
{

    // On start create 2 native arrays: verts and tris

    // On update fill the entities with new mesh values if key is pressed
    // Stitch the meshes together into the native arrays
    // Update the maze's mesh with this one stitched mesh (mesh update is expensive)


    //public static readonly float torusRadius = 10f;
    //public static readonly float pipeRadius = 6f;
    //public static readonly int pipeSegments = 17;
    //public static readonly int torusSegments = 17;
    //public static readonly float squareSize = 2 * torusRadius;// math.sqrt(2 * math.pow(torusRadius,2)) + pipeRadius;

    //public static MeshData meshDataCrossCase;
    //public static MeshData meshDataTCase;
    //public static MeshData meshDataLCase;
    //public static MeshData meshDataStraightCase;
    //public static MeshData meshDataDeadEndCase;

    //public static int[,] mazeArray2d;

    Singleton singleton;
    GameObject parentMaze;
    protected override void OnCreate()
    {

        
        //mazeArray2d = new int[,]
        //{
        //    {0,0,0,0,0,1,0,0,0,1,0,0,0,0,1,1},
        //    {1,1,0,1,1,1,0,1,0,1,0,1,1,1,1,1},
        //    {1,1,0,1,1,1,0,1,0,1,0,0,0,0,1,1},
        //    {0,0,0,1,1,1,0,0,0,1,0,1,1,1,1,1},
        //    {1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1},
        //    {1,0,0,0,0,1,0,1,1,0,1,1,1,1,1,1},
        //    {1,0,1,1,1,1,0,1,1,0,1,0,0,0,0,1},
        //    {1,0,0,0,0,1,0,1,1,0,1,0,1,1,1,1},
        //    {1,1,1,1,0,1,0,1,1,0,1,0,1,1,1,1},
        //    {1,1,1,1,0,1,0,1,1,0,1,0,1,1,1,1},
        //    {1,0,0,0,0,1,0,0,0,0,1,0,0,0,0,1},
        //    {1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1},
        //    {1,1,1,1,0,1,0,0,0,1,0,0,0,0,1,1},
        //    {1,1,0,0,0,1,1,0,1,1,0,1,1,1,1,1},
        //    {1,1,0,1,0,1,1,0,1,1,0,1,1,1,1,1},
        //    {1,1,0,0,0,1,0,0,0,1,0,0,0,0,1,1}
        //};

    }

    protected override void OnStartRunning()
    {

        parentMaze = GameObject.Find("Maze");


        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();
    }

    // TODO move outside this class, we will use it to pass a 1dCaseArray into the entity and that is all, the entity will not use the raw seed
    public static int[,] Get2dArrayFromSeed(string seed)
    {
        seed = "VGjlIHF16WPrIGJyb3duIFRoZSBxdWljayBicm93biA=";
        byte[] binaryFromSeed = Convert.FromBase64String(seed);

        int[] binary16FromSeed = new int[16];

        int[,] outArray = new int[16, 16];

        //for (int i = 0; i < binary16FromSeed.Length; i++)
        //{
        //    if (i > 0)
        //    {
        //        binary16FromSeed[i] = binaryFromSeed[2* i - 1] + binaryFromSeed[2 * i - 2];
        //    }
        //}

        // TODO combine array of 0b11111111, 0b11111111 into array of 0b1111111111111111

        for (int lineIndex = 0; lineIndex < binary16FromSeed.Length; lineIndex++)
        {
            //Debug.LogError(binaryFromSeed[lineIndex].ToString());

            for (int i = 0; i < 16; i++) // for each bit in 2 bytes
            {
                //if (lineIndex == 0) Debug.LogError((uint)binaryFromSeed[lineIndex] >> i);

                uint logicallyRightShifted = (uint)binaryFromSeed[lineIndex] >> i;

                if (logicallyRightShifted % 2 == 0) // check if the number represented by the binary (shifted i times) is even (check there is a 0 at the end)
                {
                    outArray[i, lineIndex] = 0;
                }
                else
                {
                    outArray[i, lineIndex] = 1;
                }
            }
        }
        //outArray = new int[,]
        //{
        //    {0,1,1,1,1,1,0,1,1,1,1,1,0,0,1,0},
        //    { 0,1,0,0,0,1,0,0,0,0,0,1,0,1,1,1},
        //    { 0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,0},
        //    { 0,1,0,1,0,0,1,0,0,0,1,0,0,1,0,0},
        //    { 0,0,0,1,0,0,1,0,0,0,1,1,1,1,1,0},
        //    { 0,1,1,1,1,1,1,0,0,0,0,0,1,0,1,1},
        //    { 0,0,0,0,0,0,1,0,1,1,1,0,0,0,0,0},
        //    { 2,1,1,1,1,1,1,0,1,0,1,1,1,1,1,2},
        //    { 0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0},
        //    { 1,1,0,0,0,1,0,1,0,1,1,1,1,1,1,1},
        //    { 0,1,1,1,1,1,0,1,0,0,0,0,0,0,0,1},
        //    { 0,0,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
        //    { 0,1,1,1,0,1,1,1,0,0,0,1,0,0,0,1},
        //    { 0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1},
        //    { 0,1,0,0,0,1,0,0,0,0,0,1,0,1,0,0},
        //    { 0,1,1,1,1,1,1,1,1,1,0,1,0,1,1,1}
        //};

        string s = "";

        for (int i = 0; i < 16; i++)
        {
            s += outArray[0, i];
            
        }
        Debug.LogError(s);
        return outArray;
        //return new int[,]
        //{
        //    {0,1,1,1,1,1,0,1,1,1,1,1,0,0,1,0},
        //    {0,1,0,0,0,1,0,0,0,0,0,1,0,1,1,1},
        //    {0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,0},
        //    {0,1,0,1,0,0,1,0,0,0,1,0,0,1,0,0},
        //    {0,0,0,1,0,0,1,0,0,0,1,1,1,1,1,0},
        //    {0,1,1,1,1,1,1,0,0,0,0,0,1,0,1,1},
        //    {0,0,0,0,0,0,1,0,1,1,1,0,0,0,0,0},
        //    {2,1,1,1,1,1,1,0,1,0,1,1,1,1,1,2},
        //    {0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0},
        //    {1,1,0,0,0,1,0,1,0,1,1,1,1,1,1,1},
        //    {0,1,1,1,1,1,0,1,0,0,0,0,0,0,0,1},
        //    {0,0,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
        //    {0,1,1,1,0,1,1,1,0,0,0,1,0,0,0,1},
        //    {0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1},
        //    {0,1,0,0,0,1,0,0,0,0,0,1,0,1,0,0},
        //    {0,1,1,1,1,1,1,1,1,1,0,1,0,1,1,1}
        //};
    }

    //Update mesh code
    void UpdateMesh(Vector3[] mazeVertices, int[] mazeTriangles, int meshIndex) // TODO add one mesh per maze
    {
        Maze newMaze = GameObject.Instantiate<Maze>(singleton.mazePrefab);
        newMaze.transform.parent = parentMaze.transform;
        //newMaze = newMaze.GetComponent<Maze>();
        newMaze.mesh.Clear();

        newMaze.mesh.SetVertices(mazeVertices);
        newMaze.mesh.SetTriangles(mazeTriangles, 0); //submesh 0

        newMaze.mesh.RecalculateBounds();
        newMaze.mesh.RecalculateNormals();
    }


    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        bool mazeNeedsUpdate = singleton.mazeNeedsUpdate;// Input.GetKeyDown(KeyCode.Space);

        if (mazeNeedsUpdate)
        {
            //size of array is maximum possible size of maze (256 tubes)
            NativeArray<int> numberOfVertsArray = new NativeArray<int>(256 * singleton.numberOfMazes, Allocator.TempJob);
            NativeArray<int> numberOfTrisArray = new NativeArray<int>(256 * singleton.numberOfMazes, Allocator.TempJob);

            //// change 1s to 0s on maze update
            //for (int y = 0; y < 16; y++)
            //{
            //    for (int x = 0; x < 16; x++)
            //    {
            //        if (mazeArray2d[x, y] == 1)
            //        {
            //            mazeArray2d[x, y] = 0;
            //        }
            //        else if (mazeArray2d[x, y] == 0)
            //        {
            //            mazeArray2d[x, y] = 1;
            //        }
            //    }
            //}


            //NativeArray<PipeConnections> caseArray = new NativeArray<PipeConnections>(256, Allocator.TempJob);

            
            // TODO add new buffer to contain the actual preconverted case array "ref DynamicBuffer<CaseArrayElement> caseArray"
            // TODO add bool mazeNeedsUpdate as a component that we can set outside and then reset to false once the Update is completed.
            // assigns mesh data to each entity
            Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices,
                ref Translation translation, ref PipeCaseComponent pipeCaseComponent, ref ToCreate toCreate) =>
            {
                PipeConnections thisCase = (PipeConnections) pipeCaseComponent.Value;
                                
                if (toCreate.Value && (uint)thisCase %2 == 1) //has flag PipeConnections.Exists (1)
                {
                    int positionOnMaze = entityInQueryIndex % 256;

                    //if (toCreate.Value)
                    //{
                        float3 newTranslation = new float3(0f, 0f, 0f);

                        newTranslation.x = squareSize * (positionOnMaze % 16);
                        newTranslation.y = 0f;
                        newTranslation.z = squareSize * (positionOnMaze / 16);

                        translation.Value += newTranslation;

                    //}
                    // translation.Value *= squareSize; //new Vector3((new Unity.Mathematics.Random((uint)(entityInQueryIndex+ (randSeed[0]++)))).NextFloat(-100f, 100f), 0, (new Unity.Mathematics.Random((uint)(entityInQueryIndex + (randSeed[1]++)))).NextFloat(-100f, 100f));

                    MeshData meshData = new MeshData();

                    switch (thisCase)
                    {
                        // Straight cases
                        case PipeConnections.Exists | PipeConnections.Left | PipeConnections.Right:
                            meshData = GetRotatedMeshDataByCase(PipeCase.Straight, 0);
                            break;
                        case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Down:
                            meshData = GetRotatedMeshDataByCase(PipeCase.Straight, 90);
                            break;
                        // L cases
                        case PipeConnections.Exists | PipeConnections.Down | PipeConnections.Right:
                            meshData = GetRotatedMeshDataByCase(PipeCase.L, 0);
                            break;
                        case PipeConnections.Exists | PipeConnections.Down | PipeConnections.Left:
                            meshData = GetRotatedMeshDataByCase(PipeCase.L, 90);
                            break;
                        case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Left:
                            meshData = GetRotatedMeshDataByCase(PipeCase.L, 180);
                            break;
                        case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Right:
                            meshData = GetRotatedMeshDataByCase(PipeCase.L, 270);
                            break;
                        // T cases
                        case PipeConnections.Exists | PipeConnections.Down | PipeConnections.Up | PipeConnections.Right:
                            meshData = GetRotatedMeshDataByCase(PipeCase.T, 0);
                            break;
                        case PipeConnections.Exists | PipeConnections.Left | PipeConnections.Right | PipeConnections.Down:
                            meshData = GetRotatedMeshDataByCase(PipeCase.T, 90);
                            break;
                        case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Down | PipeConnections.Left:
                            meshData = GetRotatedMeshDataByCase(PipeCase.T, 180);
                            break;
                        case PipeConnections.Exists | PipeConnections.Left | PipeConnections.Right | PipeConnections.Up:
                            meshData = GetRotatedMeshDataByCase(PipeCase.T, 270);
                            break;
                        // DeadEnd cases
                        case PipeConnections.Exists | PipeConnections.Left:
                            meshData = GetRotatedMeshDataByCase(PipeCase.DeadEnd, 0);
                            break;
                        case PipeConnections.Exists | PipeConnections.Up:
                            meshData = GetRotatedMeshDataByCase(PipeCase.DeadEnd, 90);
                            break;
                        case PipeConnections.Exists | PipeConnections.Right:
                            meshData = GetRotatedMeshDataByCase(PipeCase.DeadEnd, 180);
                            break;
                        case PipeConnections.Exists | PipeConnections.Down:
                            meshData = GetRotatedMeshDataByCase(PipeCase.DeadEnd, 270);
                            break;
                        // Cross case
                        case PipeConnections.Exists | PipeConnections.Up | PipeConnections.Down | PipeConnections.Left | PipeConnections.Right:
                            meshData = GetRotatedMeshDataByCase(PipeCase.Cross, 0);
                            break;
                        //exit case
                        case PipeConnections.Exists:
                            meshData = GetRotatedMeshDataByCase(PipeCase.Cross, 0);
                            break;

                        //theoretically the program should not reach this
                        //default:
                        //    meshData = GetRotatedMeshDataByCase(PipeCase.Cross, 0);
                        //    break;

                    }
                    if (meshData.vertices != null)
                    {

                        float3[] vertices = meshData.vertices;
                        int[] triangles = meshData.triangles;


                        //populate vertex buffer with empty vertices
                        if (Vertices.Length < vertices.Length)
                        {
                            for (int j = 0; j < vertices.Length; j++)
                            {
                                if (Vertices.Length < vertices.Length)
                                {
                                    Vertices.Add(new Float3BufferElement { Value = new float3(0f, 0f, 1f) });
                                }
                            }
                        }
                        //fill in vertices
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            var vertex = Vertices[i];
                            vertex.Value = vertices[i] + translation.Value;
                            Vertices[i] = vertex;
                            numberOfVertsArray[entityInQueryIndex]++;
                        }

                        //populate triangles buffer with empty triangles
                        if (Triangles.Length < triangles.Length)
                        {
                            for (int j = 0; j < triangles.Length; j++)
                            {
                                if (Triangles.Length < triangles.Length)
                                {
                                    Triangles.Add(new IntBufferElement { Value = 0 });
                                }
                            }
                        }
                        //fill in triangles
                        for (int i = 0; i < triangles.Length; i++)
                        {
                            var triangle = Triangles[i];
                            triangle.Value = triangles[i];
                            Triangles[i] = triangle;
                        }

                        //export the number of verts/tris to outside the job
                        numberOfVertsArray[entityInQueryIndex] = vertices.Length;
                        numberOfTrisArray[entityInQueryIndex] = triangles.Length;

                    }
                    toCreate.Value = false;
                }
            }).ScheduleParallel();
            CompleteDependency();

            //caseArray.Dispose();
            //randSeed.Dispose();

            int numVerts = 0;
            int numTris = 0;

            for (int i = 0; i < numberOfVertsArray.Length; i++)
            {
                numVerts += numberOfVertsArray[i];
            }

            for (int i = 0; i < numberOfTrisArray.Length; i++)
            {
                numTris += numberOfTrisArray[i];
            }

            NativeArray<bool> meshUpdated = new NativeArray<bool>(1, Allocator.TempJob);
            NativeArray<Vector3> mazeVertices = new NativeArray<Vector3>(numVerts, Allocator.TempJob);
            NativeArray<int> mazeTriangles = new NativeArray<int>(numTris, Allocator.TempJob);

            meshUpdated[0] = false;
            // Stitch together all meshes
            Entities.ForEach((int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices, ref NeedsMeshUpdate needsMeshUpdate) =>
            {
                if (needsMeshUpdate.Value)
                {
                    int cumulativeNumVerts = 0;

                    for (int i = 0; i < entityInQueryIndex; i++)
                    {
                        cumulativeNumVerts += numberOfVertsArray[i];
                    }

                    for (int j = 0; j < Vertices.Length + cumulativeNumVerts; j++)
                    {
                        if (j < cumulativeNumVerts)
                        {
                            mazeVertices[j] = mazeVertices[j];
                        }
                        else
                        {
                            mazeVertices[j] = Vertices[j - cumulativeNumVerts].Value;
                        }
                    }

                    int cumulativeNumTris = 0;
                    for (int i = 0; i < entityInQueryIndex; i++)
                    {
                        cumulativeNumTris += numberOfTrisArray[i];
                    }

                    for (int j = 0; j < Triangles.Length + cumulativeNumTris; j++)
                    {
                        if (j < cumulativeNumTris)
                        {
                            mazeTriangles[j] = mazeTriangles[j];
                        }
                        else
                        {
                            mazeTriangles[j] = Triangles[j - cumulativeNumTris].Value + cumulativeNumVerts;
                        }
                    }
                    meshUpdated[0] = true;
                    needsMeshUpdate.Value = false;
                }
            }).Schedule();

            CompleteDependency();
            if (meshUpdated[0])
            {
                UpdateMesh(mazeVertices.ToArray(), mazeTriangles.ToArray(), singleton.numberOfMazes-1);
                singleton.mazeNeedsUpdate = false;
            }

            meshUpdated.Dispose();
            numberOfVertsArray.Dispose();
            numberOfTrisArray.Dispose();

            mazeVertices.Dispose();
            mazeTriangles.Dispose();

        }
    }
}


// Code for generating random numbers inside job
//NativeArray<int> randSeed = new NativeArray<int>(1, Allocator.TempJob);
//randSeed[0] = UnityEngine.Random.Range(0,177); //arbitrary random seed
//var random = new Unity.Mathematics.Random((uint)(entity.Index + entityInQueryIndex + randSeed[0] + 1) * 0x9F6ABC1);
//var randomVector = math.normalizesafe(random.NextFloat3() - new float3(0.5f, 0.5f, 0.5f));
//randomVector.y = 0;

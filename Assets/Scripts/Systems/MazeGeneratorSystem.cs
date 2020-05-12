﻿using UnityEngine;
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

public class MazeGeneratorSystem : SystemBase
{
    // On start create 2 native arrays: verts and tris

    // On update fill the entities with new mesh values if key is pressed
    // Stitch the meshes together into the native arrays
    // Update the maze's mesh with this one stitched mesh (mesh update is expensive)

    public struct MeshData
    {
        public float3[] vertices { get; set; }
        public int[] triangles { get; set; }
    }

    public enum PipeCase
    {
        Invalid = 0,
        DeadEnd = 1,
        L = 2,
        T = 3,
        Cross = 4,
        Straight = 5
    }

    [System.Flags]
    public enum PipeCaseFlags // enum representing flags for all possibilities of pipe connections
    {
        Empty = 0,
        Exists = 1,
        Up = 2,
        Right = 4,
        Down = 8,
        Left = 16
    }

    public static readonly float torusRadius = 10f;
    public static readonly float pipeRadius = 3f;
    public static readonly int pipeSegments = 16;
    public static readonly float squareSize = math.sqrt((2 * torusRadius * torusRadius));
    protected override void OnCreate()
    {
    }
    static Vector3 GetPoint(float u, float v)
    {

        Vector3 point;

        point.x = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Cos(u);
        point.y = pipeRadius * Mathf.Sin(v);
        point.z = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Sin(u);

        return point;
    }
    //Update mesh code
    void UpdateMesh(Vector3[] mazeVertices, int[] mazeTriangles)
    {
        Maze maze = GameObject.Find("Maze").GetComponent<Maze>();

        maze.mesh.Clear();

        maze.mesh.SetVertices(mazeVertices);
        maze.mesh.SetTriangles(mazeTriangles, 0); //submesh 0

        maze.mesh.RecalculateBounds();
        maze.mesh.RecalculateNormals();
    }

    public static PipeCaseFlags[,] GetCaseArray(int[,] mazeArray)
    {
        PipeCaseFlags[,] pipeCaseArray = new PipeCaseFlags[16, 16]; // TODO un-hardcode this stuff lol

        for (int x = 0; x < 16;x++)
        {
            for (int y = 0; y < 16; y++)
            {
                if (mazeArray[x, y] == 0)
                {
                    pipeCaseArray[x, y] = PipeCaseFlags.Empty;
                    continue;
                }
                pipeCaseArray[x, y] = PipeCaseFlags.Exists;

                if (x - 1 >= 0)
                    if (mazeArray[x - 1, y] == 1)
                        pipeCaseArray[x, y] |= PipeCaseFlags.Down;

                if (x + 1 < 16)
                    if (mazeArray[x + 1, y] == 1)
                        pipeCaseArray[x, y] |= PipeCaseFlags.Up;

                if (y - 1 >= 0)
                    if (mazeArray[x, y - 1] == 1)
                        pipeCaseArray[x, y] |= PipeCaseFlags.Right;

                if (y + 1 < 16)
                    if (mazeArray[x, y + 1] == 1)
                        pipeCaseArray[x, y] |= PipeCaseFlags.Left;
            }
        }

        return pipeCaseArray;
    }

    public static MeshData GetStraightMeshData()
    {
        List<float3> Vertices = new List<float3>();
        List<int> Triangles = new List<int>();
        //populate vertices list
        if (Vertices.Count < 2 * (pipeSegments - 1))
        {
            for (int j = 0; j < 2 * (pipeSegments - 1); j++)
            {
                if (Vertices.Count < 2 * (pipeSegments - 1))
                {
                    Vertices.Add(new float3(0f, 0f, 1f));
                }
            }
        }

        // create vertices list
        float v = 0;
        for (int i = 0; i < (pipeSegments - 1); i++)
        {
            Vector3 point1 = GetPoint(0, v);
            Vector3 point2 = GetPoint(0, v) + squareSize * new Vector3(0f, 0f, 1f);

            v += (2f * Mathf.PI) / (pipeSegments - 1);

            //vertex = Vertices[2 * i];
            //vertex.Value = point1;
            Vertices[2 * i] = point1;

            //vertex = Vertices[2 * i + 1];
            //vertex.Value = point2;
            Vertices[2 * i + 1] = point2;
        }

        for (int i = 0; i < Vertices.Count; i++)
        {
            Vertices[i] -= new float3(torusRadius, 0, 0);
        }

        //populate triangles list

        int maxVertex = (2 * (pipeSegments - 1)) - 1;

        if (Triangles.Count < (maxVertex + 1) * 3)
        {
            for (int j = 0; j < (maxVertex + 1) * 3; j++)
            {
                if (Triangles.Count < (maxVertex + 1) * 3)
                {
                    Triangles.Add(0);
                }
            }
        }

        //create triangles list
        for (int i = 0; i < maxVertex + 1; i++)
        {
            if (i % 2 == 1)
            {
                //triangle = Triangles[3 * i];
                //triangle.Value = (i + 2) % (maxVertex + 1);
                Triangles[3 * i] = (i + 2) % (maxVertex + 1);

                //triangle = Triangles[3 * i + 1];
                //triangle.Value = (i + 1) % (maxVertex + 1);
                Triangles[3 * i + 1] = (i + 1) % (maxVertex + 1);

                //triangle = Triangles[3 * i + 2];
                //triangle.Value = i;
                Triangles[3 * i + 2] = i;

            }
            else
            {
                //triangle = Triangles[3 * i];
                //triangle.Value = i;
                Triangles[3 * i] = i;

                //triangle = Triangles[3 * i + 1];
                //triangle.Value = (i + 1) % (maxVertex + 1);
                Triangles[3 * i + 1] = (i + 1) % (maxVertex + 1);

                //triangle = Triangles[3 * i + 2];
                //triangle.Value = (i + 2) % (maxVertex + 1);
                Triangles[3 * i + 2] = (i + 2) % (maxVertex + 1);
            }
        }

        return new MeshData() { vertices = Vertices.ToArray(), triangles = Triangles.ToArray() };
    }

    public static MeshData GetMeshDataByCase(PipeCase mazeCase, int eulerAngle) 
    {
        
        //List<float3> Vertices = new List<float3>();
        //List<int> Triangles = new List<int>();

        MeshData meshData = new MeshData();

        switch (mazeCase)
        {
            case PipeCase.Invalid: //invalid
                break;
            case PipeCase.DeadEnd: // dead end case
                break;
            case PipeCase.L: // L case
                break;
            case PipeCase.T: // T-Junction case
                break;
            case PipeCase.Cross: // crossroads case
                break;
            case PipeCase.Straight: // straight pipe case

                meshData = GetStraightMeshData();
                // create 
                break;
            default: //invalid
                break;
        }

        //rotate all vertices around pivot (centre)
        Quaternion rotationQuaternion = new Quaternion();
        rotationQuaternion.eulerAngles = new Vector3(0, eulerAngle, 0);

        Vector3 centre = new Vector3(0, 0, squareSize/2);

        for (int i = 0; i < meshData.vertices.Length; i++)
        {
            meshData.vertices[i] = rotationQuaternion * ((Vector3)meshData.vertices[i] - centre) + centre;
        }

        return meshData; // new MeshData() { vertices = Vertices.ToArray(), triangles = Triangles.ToArray() };
    }

    protected override void OnUpdate()
    {



        float deltaTime = Time.DeltaTime;

        bool mazeNeedsUpdate = Input.GetKeyDown(KeyCode.Space);

        //size of array is maximum possible size of maze (256 tubes)
        NativeArray<int> numberOfVertsArray = new NativeArray<int>(256, Allocator.TempJob);
        NativeArray<int> numberOfTrisArray = new NativeArray<int>(256, Allocator.TempJob);

        //NativeArray<int> randSeed = new NativeArray<int>(1, Allocator.TempJob);
        //randSeed[0] = UnityEngine.Random.Range(0,177); //arbitrary random seed


        int[,] mazeArray2d = new int[,]
        {
            {0,1,1,1,1,1,0,1,1,1,1,1,0,0,1,0},
            {0,1,0,0,0,1,0,0,0,0,0,1,0,1,1,1},
            {0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,0},
            {0,1,0,1,0,0,1,0,0,0,1,0,0,1,0,0},
            {0,0,0,1,0,0,1,0,0,0,1,1,1,1,1,0},
            {0,1,1,1,1,1,1,0,0,0,0,0,1,0,1,1},
            {0,0,0,0,0,0,1,0,1,1,1,0,0,0,0,0},
            {1,1,1,1,1,1,1,0,1,0,1,1,1,1,1,1},
            {0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0},
            {1,1,0,0,0,1,0,1,0,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,0,1,0,0,0,0,0,0,0,1},
            {0,0,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
            {0,1,1,1,0,1,1,1,0,0,0,1,0,0,0,1},
            {0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1},
            {0,1,0,0,0,1,0,0,0,0,0,1,0,1,0,0},
            {0,1,1,1,1,1,1,1,1,1,0,1,0,1,1,1}
        };

        // TODO we want to convert the array above into an array of Enums showing us which directions have pipes attached

        PipeCaseFlags[,] caseArray2d = GetCaseArray(mazeArray2d);

        NativeArray<PipeCaseFlags> caseArray = new NativeArray<PipeCaseFlags>(256, Allocator.TempJob);

        //convert 2D array into 1D array
        int index = 0;
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                caseArray[index] = caseArray2d[x, y];
                index++;
            }
        }

        // assigns mesh data to each entity (currently just a placeholder mesh)
        Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices,
            ref Translation translation, ref Seed seed) =>
        {

            //entities[entityInQueryIndex] = entity;

            if (mazeNeedsUpdate && caseArray[entityInQueryIndex].HasFlag(PipeCaseFlags.Exists))
            {
                //var random = new Unity.Mathematics.Random((uint)(entity.Index + entityInQueryIndex + randSeed[0] + 1) * 0x9F6ABC1);
                //var randomVector = math.normalizesafe(random.NextFloat3() - new float3(0.5f, 0.5f, 0.5f));
                //randomVector.y = 0;

                float3 newTranslation = new float3(0f, 0f, 0f);

                newTranslation.x = squareSize * (entityInQueryIndex % 16);
                newTranslation.y = 0f;
                newTranslation.z = squareSize * (entityInQueryIndex / 16);

                translation.Value = newTranslation;

               // translation.Value *= squareSize; //new Vector3((new Unity.Mathematics.Random((uint)(entityInQueryIndex+ (randSeed[0]++)))).NextFloat(-100f, 100f), 0, (new Unity.Mathematics.Random((uint)(entityInQueryIndex + (randSeed[1]++)))).NextFloat(-100f, 100f));

                MeshData meshData = new MeshData();

                // TODO choose case based on array of cases
                
                if (caseArray[entityInQueryIndex] == (PipeCaseFlags.Exists | PipeCaseFlags.Up | PipeCaseFlags.Down))
                {
                    meshData = GetMeshDataByCase(PipeCase.Straight, 90);
                }
                else if (caseArray[entityInQueryIndex] == (PipeCaseFlags.Exists | PipeCaseFlags.Left | PipeCaseFlags.Right))
                {
                    meshData = GetMeshDataByCase(PipeCase.Straight, 0);
                }
                //else if (caseArray[entityInQueryIndex] == (PipeCaseFlags.Exists | PipeCaseFlags.Down | PipeCaseFlags.Right))
                //{
                //    meshData = GetMeshDataByCase(PipeCase.Straight, 135);
                ////}
                //else if (caseArray[entityInQueryIndex].HasFlag(PipeCaseFlags.Exists)) //== (PipeCase.Exists | PipeCase.Down| PipeCase.Up) )
                //{
                //    meshData = GetMeshDataByCase(PipeCase.Straight, 0);
                //}




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

                // START OF PLACEHOLDER CODE

                //if (Vertices.Length < 2 * (pipeSegments - 1))
                //{
                //    //populate vertex buffer
                //    for (int j = 0; j < 2 * (pipeSegments - 1); j++)
                //    {
                //        if (Vertices.Length < 2 * (pipeSegments - 1))
                //        {
                //            Vertices.Add(new Float3BufferElement { Value = new float3(0f, 0f, 1f) });
                //        }
                //    }
                //}

                //float v = 0;
                ////used to generate the array of vertices as required 
                //Float3BufferElement vertex;
                //for (int i = 0; i < (pipeSegments - 1); i++)
                //{
                //    Vector3 point1 = (Vector3)translation.Value + GetPoint(0, v);
                //    Vector3 point2 = (Vector3)translation.Value + GetPoint(0, v) + torusRadius / 2 * new Vector3(0f, 0f, 2f);

                //    v += (2f * Mathf.PI) / (pipeSegments - 1);

                //    vertex = Vertices[2 * i];
                //    vertex.Value = point1;
                //    Vertices[2 * i] = vertex;

                //    vertex = Vertices[2 * i + 1];
                //    vertex.Value = point2;
                //    Vertices[2 * i + 1] = vertex;

                //    numberOfVertsArray[entityInQueryIndex] += 2;
                //}


                //int maxVertex = (2 * (pipeSegments - 1)) - 1;

                ////populate triangles buffer
                //if (Triangles.Length < (maxVertex + 1) * 3)
                //{
                //    for (int j = 0; j < (maxVertex + 1) * 3; j++)
                //    {
                //        if (Triangles.Length < (maxVertex + 1) * 3)
                //        {
                //            Triangles.Add(new IntBufferElement { Value = 0 });
                //        }
                //    }
                //}

                ////used to generate the array of triangles as required 
                //IntBufferElement triangle;
                //for (int i = 0; i < maxVertex + 1; i++)
                //{
                //    if (i % 2 == 1)
                //    {
                //        triangle = Triangles[3 * i];
                //        triangle.Value = (i + 2) % (maxVertex + 1);
                //        Triangles[3 * i] = triangle;

                //        triangle = Triangles[3 * i + 1];
                //        triangle.Value = (i + 1) % (maxVertex + 1);
                //        Triangles[3 * i + 1] = triangle;

                //        triangle = Triangles[3 * i + 2];
                //        triangle.Value = i;
                //        Triangles[3 * i + 2] = triangle;

                //    }
                //    else
                //    {
                //        triangle = Triangles[3 * i];
                //        triangle.Value = i;
                //        Triangles[3 * i] = triangle;

                //        triangle = Triangles[3 * i + 1];
                //        triangle.Value = (i + 1) % (maxVertex + 1);
                //        Triangles[3 * i + 1] = triangle;

                //        triangle = Triangles[3 * i + 2];
                //        triangle.Value = (i + 2) % (maxVertex + 1);
                //        Triangles[3 * i + 2] = triangle;
                //    }

                //    numberOfTrisArray[entityInQueryIndex] += 3;
                //}


                // END OF PLACEHOLDER CODE


            }
        }).ScheduleParallel();
        CompleteDependency();

        caseArray.Dispose();
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





        NativeArray<Vector3> mazeVertices = new NativeArray<Vector3>(numVerts, Allocator.TempJob);
        NativeArray<int> mazeTriangles = new NativeArray<int>(numTris, Allocator.TempJob);

        // Stitch together meshes
        Entities.ForEach((int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices) =>
        {
            if (mazeNeedsUpdate)
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
            }
        }).Schedule();

        CompleteDependency();

        if (mazeNeedsUpdate)
        {
            UpdateMesh(mazeVertices.ToArray(), mazeTriangles.ToArray());
        }

        numberOfVertsArray.Dispose();

        numberOfTrisArray.Dispose();

        mazeVertices.Dispose();
        mazeTriangles.Dispose();
    }
}






// TODO check if any of this is still useful

//    for (int i = 0; i < entities.Length; i++)
//    {

//        Vector3[] originalVertices = maze.mesh.vertices;
//        int[] originalTriangles = maze.mesh.triangles;

//        Vector3[] verticesToWrite = new Vector3[vertices.Length + originalVertices.Length];
//        int[] trianglesToWrite = new int[triangles.Length + originalTriangles.Length];

//        for (int j = 0; j < vertices.Length + originalVertices.Length; j++)
//        {
//            if (j < originalVertices.Length)
//            {
//                verticesToWrite[j] = originalVertices[j];
//            }
//            else
//            {
//                verticesToWrite[j] = vertices[j - originalVertices.Length].Value;
//            }
//        }

//        for (int j = 0; j < triangles.Length + originalTriangles.Length; j++)
//        {
//            if (j < originalTriangles.Length)
//            {
//                trianglesToWrite[j] = originalTriangles[j];
//            }
//            else
//            {
//                trianglesToWrite[j] = triangles[j - originalTriangles.Length].Value + originalVertices.Length;
//            }
//        }


//    }

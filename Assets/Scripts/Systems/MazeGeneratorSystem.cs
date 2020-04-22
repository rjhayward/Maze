﻿using UnityEngine;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Mathematics;
using System.Collections.Generic;

public class MazeGeneratorSystem : SystemBase
{
    // On start create 2 native arrays: verts and tris

    // On update fill the entities with new mesh values if key is pressed
    // Stitch the meshes together into the native arrays
    // Update the maze's mesh with this one stitched mesh (mesh update is expensive)



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

    protected override void OnUpdate()
    {
        float torusRadius = 10f;
        float pipeRadius = 5f;
        int pipeSegments = 10;

        Vector3 GetPoint(float u, float v)
        {

            Vector3 point;

            point.x = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Cos(u);
            point.y = (torusRadius + pipeRadius * Mathf.Cos(v)) * Mathf.Sin(u);
            point.z = pipeRadius * Mathf.Sin(v);

            return point;
        }

        float deltaTime = Time.DeltaTime;

        bool mazeNeedsUpdate = Input.GetKeyDown(KeyCode.Space);

        NativeArray<int> numberOfVertsArray = new NativeArray<int>(1000, Allocator.TempJob);
        NativeArray<int> numberOfTrisArray = new NativeArray<int>(1000, Allocator.TempJob);

        NativeArray<int> randSeed = new NativeArray<int>(1, Allocator.TempJob);
        randSeed[0] = UnityEngine.Random.Range(0,177); //arbitrary random seed

        // assigns mesh data to each entity (currently just a placeholder mesh)
        Entities.ForEach((Entity entity, int entityInQueryIndex, DynamicBuffer < IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices, 
            ref Translation translation, ref Seed seed) =>
        {
            float v = 0;

            //entities[entityInQueryIndex] = entity;


            if (mazeNeedsUpdate)
            {
                var random = new Unity.Mathematics.Random((uint)(entity.Index + entityInQueryIndex + randSeed[0] + 1) * 0x9F6ABC1);
                var randomVector = math.normalizesafe(random.NextFloat3() - new float3(0.5f, 0.5f, 0.5f));
                randomVector.y = 0;
                translation.Value = randomVector * 100; //new Vector3((new Unity.Mathematics.Random((uint)(entityInQueryIndex+ (randSeed[0]++)))).NextFloat(-100f, 100f), 0, (new Unity.Mathematics.Random((uint)(entityInQueryIndex + (randSeed[1]++)))).NextFloat(-100f, 100f));

                if (Vertices.Length < 2 * (pipeSegments - 1))
                {
                    //populate vertex buffer
                    for (int j = 0; j < 2 * (pipeSegments - 1); j++)
                    {
                        if (Vertices.Length < 2 * (pipeSegments - 1))
                        {
                            Vertices.Add(new Float3BufferElement { Value = new float3(0f, 1f, 0f) });
                        }
                    }
                }

                //used to generate the array of vertices as required 
                Float3BufferElement vertex;
                for (int i = 0; i < (pipeSegments - 1); i++)
                {
                    Vector3 point1 = (Vector3)translation.Value + GetPoint(0, v);
                    Vector3 point2 = (Vector3)translation.Value + GetPoint(0, v) + torusRadius / 2 * new Vector3(0f, 1f, 0f);

                    v += (2f * Mathf.PI) / (pipeSegments - 1);

                    vertex = Vertices[2 * i];
                    vertex.Value = point1;
                    Vertices[2 * i] = vertex;

                    vertex = Vertices[2 * i + 1];
                    vertex.Value = point2;
                    Vertices[2 * i + 1] = vertex;

                    numberOfVertsArray[entityInQueryIndex] += 2;
                }

                //sets our mesh's vertex array to equal the array we have just created


                int maxVertex = (2 * (pipeSegments - 1)) - 1;

                //populate triangles buffer
                if (Triangles.Length < (maxVertex + 1) * 3)
                {
                    for (int j = 0; j < (maxVertex + 1) * 3; j++)
                    {
                        if (Triangles.Length < (maxVertex + 1 ) * 3)
                        {
                            Triangles.Add(new IntBufferElement { Value = 0 });
                        }
                    }
                }

                //used to generate the array of triangles as required 
                IntBufferElement triangle;
                for (int i = 0; i < maxVertex + 1; i++)
                {
                    if (i % 2 == 1)
                    {
                        triangle = Triangles[3 * i];
                        triangle.Value = (i + 2) % (maxVertex + 1);
                        Triangles[3 * i] = triangle;

                        triangle = Triangles[3 * i + 1];
                        triangle.Value = (i + 1) % (maxVertex + 1);
                        Triangles[3 * i + 1] = triangle;

                        triangle = Triangles[3 * i + 2];
                        triangle.Value = i;
                        Triangles[3 * i + 2] = triangle;

                    }
                    else
                    {
                        triangle = Triangles[3 * i];
                        triangle.Value = i;
                        Triangles[3 * i] = triangle;

                        triangle = Triangles[3 * i + 1];
                        triangle.Value = (i + 1) % (maxVertex + 1);
                        Triangles[3 * i + 1] = triangle;

                        triangle = Triangles[3 * i + 2];
                        triangle.Value = (i + 2) % (maxVertex + 1);
                        Triangles[3 * i + 2] = triangle;
                    }

                    numberOfTrisArray[entityInQueryIndex] += 3;
                }
            }
        }).ScheduleParallel();
        CompleteDependency();

        randSeed.Dispose();

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

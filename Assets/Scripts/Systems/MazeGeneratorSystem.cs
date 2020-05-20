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
using System.Threading;

public class MazeGeneratorSystem : SystemBase
{
    // Whenever we need to render a maze's tubes:
    // We will stitch the meshes together into one set of vertices and triangles.
    // Update the maze's mesh with this one stitched mesh (mesh update is expensive, so we only do it once per new maze)

    Singleton singleton;
    GameObject parentMaze;
    //PipeCases pipeCasesField;
    //public static NativeArray<MeshDataNative> meshDataArray;

    protected override void OnStartRunning()
    {
        parentMaze = GameObject.Find("Maze");

        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();
    }


    protected override void OnDestroy()
    {
        //meshDataArray.Dispose();
    }


    //Update mesh code
    void UpdateMesh(Vector3[] mazeVertices, int[] mazeTriangles, int meshIndex)
    {
        Maze newMaze = GameObject.Instantiate<Maze>(singleton.mazePrefab);
        newMaze.transform.parent = parentMaze.transform;
        newMaze.material0.color = new Color(UnityEngine.Random.Range(0.3f, 1f), UnityEngine.Random.Range(0.3f, 1f), UnityEngine.Random.Range(0.3f, 1f));

        newMaze.mesh.Clear();

        newMaze.mesh.SetVertices(mazeVertices);
        newMaze.mesh.SetTriangles(mazeTriangles, 0); //submesh 0

        newMaze.mesh.RecalculateBounds();
        newMaze.mesh.RecalculateNormals();

        newMaze.GetComponent<MeshCollider>().sharedMesh = newMaze.mesh;
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

            // For each entity, we will translate all of the vertices based on whereabouts in the query it is (otherwise we will just render pipes on top of each other)
            Entities.ForEach((int entityInQueryIndex, DynamicBuffer<IntBufferElement> Triangles, DynamicBuffer<Float3BufferElement> Vertices,
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

                    //Translate vertices
                    for (int i = 0; i < Vertices.Length; i++)
                    {
                        var vertex = Vertices[i];
                        vertex.Value += translation.Value;
                        Vertices[i] = vertex;
                        numberOfVertsArray[entityInQueryIndex]++;
                    }

                    //export the number of verts/tris to outside the job
                    numberOfVertsArray[entityInQueryIndex] = Vertices.Length;
                    numberOfTrisArray[entityInQueryIndex] = Triangles.Length;


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

            NativeArray<Vector3> mazeVertices = new NativeArray<Vector3>(numVerts, Allocator.TempJob);
            NativeArray<int> mazeTriangles = new NativeArray<int>(numTris, Allocator.TempJob);

            // bool to see whether we have updated any of the verts/tris this frame
            NativeArray<bool> meshUpdated = new NativeArray<bool>(1, Allocator.TempJob);
            meshUpdated[0] = false;

            // Stitch together all verts and tris
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

            // if we have updated (and stitched) the verts and tris, use them to set the mesh of the current maze
            if (meshUpdated[0])
            {
                UpdateMesh(mazeVertices.ToArray(), mazeTriangles.ToArray(), singleton.numberOfMazes-1);

                //flag that the maze has been updated
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

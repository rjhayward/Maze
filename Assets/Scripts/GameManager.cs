using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Mathematics;

public class GameManager : MonoBehaviour
{

    //[SerializeField] private Mesh Mesh;
    //[SerializeField] private Material Material;

    public Maze maze;

    EntityManager entityManager;
    NativeArray<Entity> entities;
    EntityArchetype mazeArchetype;

    // Start is called before the first frame update
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // create entity archetypes

        EntityArchetype shipArchetype = entityManager.CreateArchetype(

            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(IsShip),
            typeof(Direction),
            typeof(Translation)
        );

        EntityArchetype projectileArchetype = entityManager.CreateArchetype(

            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(Direction),
            typeof(Translation)
        );

        EntityArchetype boidArchetype = entityManager.CreateArchetype(

            typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            typeof(Direction),
            typeof(Translation),
            typeof(ViewAngle),
            typeof(IsAlive)
        );

        mazeArchetype = entityManager.CreateArchetype(

            //typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), // required components to render
            //typeof(MeshData),
            typeof(IntBufferElement),
            typeof(Float3BufferElement),
            typeof(Translation),
            typeof(Seed)
        );

        entities = new NativeArray<Entity>(1000, Allocator.Persistent);

        entityManager.CreateEntity(mazeArchetype, entities);

        for (int i = 0; i < entities.Length; i++)
        {
            entityManager.SetComponentData(entities[i], new Translation
            {
                Value = new Vector3(UnityEngine.Random.Range(-100f, 100f), 0, UnityEngine.Random.Range(-100f, 100f))
            });

            entityManager.AddBuffer<IntBufferElement>(entities[i]);

            entityManager.AddBuffer<Float3BufferElement>(entities[i]);

            //DynamicBuffer<Float3BufferElement> vertices = entityManager.GetBuffer<Float3BufferElement>(entities[i]);
            ////vertices.Capacity = 1000;

            //DynamicBuffer<IntBufferElement> triangles = entityManager.GetBuffer<IntBufferElement>(entities[i]);
            ////triangles.Capacity = 1000;0



        }

        entities.Dispose();

    }



    // Update is called once per frame
    void Update()
    {
        //if (true) // if updateMaze 
        //{

        //    //entities = new NativeArray<Entity>(10, Allocator.Temp);

        //    //entityManager.CreateEntity(mazeArchetype, entities);

        //    //for (int i = 0; i < entities.Length; i++)
        //    //{
        //    //    entityManager.DestroyEntity(entities[i]);
        //    //}

        //    maze.mesh.Clear();


        //    entities = new NativeArray<Entity>(10, Allocator.Persistent);

        //    //entityManager.CreateEntity(mazeArchetype, entities);


        //    // EntityQuery

        //    //EntityQuery entityQuery = CreateEntityQuery(typeof(Seed));

        //    //NativeArray <Entity> ents = ToEntityArray(Allocator.TempJob);


        //    for (int i = 0; i < entities.Length; i++)
        //    {


        //        Vector3[] originalVertices = maze.mesh.vertices;
        //        int[] originalTriangles = maze.mesh.triangles;
        //        DynamicBuffer<Float3BufferElement> vertices = entityManager.GetBuffer<Float3BufferElement>(entities[i]);

        //        DynamicBuffer<IntBufferElement> triangles = entityManager.GetBuffer<IntBufferElement>(entities[i]);

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

        //        maze.mesh.vertices = verticesToWrite;
        //        maze.mesh.triangles = trianglesToWrite;


        //        entityManager.SetComponentData(entities[i], new Translation
        //        {
        //            Value = new Vector3(UnityEngine.Random.Range(-100f, 100f), 0, UnityEngine.Random.Range(-100f, 100f))
        //        });

        //        entityManager.AddBuffer<IntBufferElement>(entities[i]);

        //        entityManager.AddBuffer<Float3BufferElement>(entities[i]);

        //    }


        //    maze.mesh.RecalculateBounds();
        //    maze.mesh.RecalculateNormals();

        //}

    }
}

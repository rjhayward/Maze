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

    [SerializeField] private Mesh shipMesh;
    [SerializeField] private Material shipMaterial;

    Singleton singleton;

    EntityManager entityManager;
    PipeCases.PipeConnections[] caseArray;
    EntityArchetype mazeArchetype;
    int mazeIndex = 0;
    // Start is called before the first frame update
    void Start()
    {
        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();

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

        
        // TODO add caseArray as a dynamic buffer, this can then be used to generate maze procedurally in here

        // max amount of mazeEntities (16*16 = 256)
        //NativeArray<Entity> mazeEntities = new NativeArray<Entity>(256, Allocator.Temp);

        //entityManager.CreateEntity(mazeArchetype, mazeEntities);

        //for (int i = 0; i < mazeEntities.Length; i++)
        //{
        //    entityManager.SetComponentData(mazeEntities[i], new Translation
        //    {
        //        Value = new Vector3(0, 0, 0)
        //    });

        //    entityManager.SetComponentData(mazeEntities[i], new NeedsMeshUpdate
        //    {
        //        Value = false
        //    });

        //    entityManager.AddBuffer<IntBufferElement>(mazeEntities[i]);

        //    entityManager.AddBuffer<Float3BufferElement>(mazeEntities[i]);

        //    entityManager.SetComponentData(mazeEntities[i], new PipeCaseComponent
        //    {
        //        Value = (int) caseArray[i]
        //    });

        //}

        //mazeEntities.Dispose();

        //// max amount of mazeEntities (16*16 = 256)
        //NativeArray<Entity> mazeEntities2 = new NativeArray<Entity>(256, Allocator.Temp);

        //entityManager.CreateEntity(mazeArchetype, mazeEntities2);

        //for (int i = 0; i < mazeEntities2.Length; i++)
        //{
        //    entityManager.SetComponentData(mazeEntities2[i], new Translation
        //    {
        //        Value = new Vector3(0, 0, PipeCases.torusRadius*32)
        //    });
        //    entityManager.SetComponentData(mazeEntities2[i], new NeedsMeshUpdate
        //    {
        //        Value = false
        //    });

        //    entityManager.AddBuffer<IntBufferElement>(mazeEntities2[i]);

        //    entityManager.AddBuffer<Float3BufferElement>(mazeEntities2[i]);

        //    entityManager.SetComponentData(mazeEntities2[i], new PipeCaseComponent
        //    {
        //        Value = (int)caseArray[i]
        //    });
        //}

        //mazeEntities2.Dispose();


        //// max amount of mazeEntities (16*16 = 256)
        //NativeArray<Entity> mazeEntities3 = new NativeArray<Entity>(256, Allocator.Temp);

        //entityManager.CreateEntity(mazeArchetype, mazeEntities3);

        //for (int i = 0; i < mazeEntities3.Length; i++)
        //{
        //    entityManager.SetComponentData(mazeEntities3[i], new Translation
        //    {
        //        Value = new Vector3(0, 0, PipeCases.torusRadius * 64)
        //    });
        //    entityManager.SetComponentData(mazeEntities3[i], new NeedsMeshUpdate
        //    {
        //        Value = false
        //    });

        //    entityManager.AddBuffer<IntBufferElement>(mazeEntities3[i]);

        //    entityManager.AddBuffer<Float3BufferElement>(mazeEntities3[i]);

        //    entityManager.SetComponentData(mazeEntities3[i], new PipeCaseComponent
        //    {
        //        Value = (int)caseArray[i]
        //    });

        //}

        //mazeEntities3.Dispose();
    }

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            int[,] mazeArray2d = new int[,]
            {
            {0,1,1,1,1,1,0,1,1,1,1,1,0,0,1,0},
            {0,1,0,0,0,1,0,0,0,0,0,1,0,1,1,1},
            {0,1,0,1,1,1,1,1,1,1,1,1,1,1,0,0},
            {0,1,0,1,0,0,1,0,0,0,1,0,0,1,0,0},
            {0,0,0,1,0,0,1,0,0,0,1,1,1,1,1,0},
            {0,1,1,1,1,1,1,0,0,0,0,0,1,0,1,1},
            {0,0,0,0,0,0,1,0,1,1,1,0,0,0,0,0},
            {2,1,1,1,1,1,1,0,1,0,1,1,1,1,1,2},
            {0,0,0,0,0,1,0,0,0,0,1,0,0,0,0,0},
            {1,1,0,0,0,1,0,1,0,1,1,1,1,1,1,1},
            {0,1,1,1,1,1,0,1,0,0,0,0,0,0,0,1},
            {0,0,0,1,0,1,0,1,1,1,1,1,1,1,0,1},
            {0,1,1,1,0,1,1,1,0,0,0,1,0,0,0,1},
            {0,1,0,1,0,1,0,1,1,1,1,1,1,1,1,1},
            {0,1,0,0,0,1,0,0,0,0,0,1,0,1,0,0},
            {0,1,1,1,1,1,1,1,1,1,0,1,0,1,1,1}
            };

            PipeCases.PipeConnections[,] caseArray2d = PipeCases.GetCaseArray(mazeArray2d);

            caseArray = new PipeCases.PipeConnections[256];

            //int index = 0;
            //for (int y = 0 + mazeIndex; y < 16 + mazeIndex; y++)
            //{
            //    for (int x = 0 + mazeIndex; x < 16 + mazeIndex; x++)
            //    {
            //        caseArray[index] = caseArray2d[x % 16, y % 16];
            //        index++;
            //    }
            //}
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
                entityManager.SetComponentData(mazeEntities[i], new Translation
                {
                    Value = new Vector3(0, 0, PipeCases.torusRadius * 32 * mazeIndex)
                });

                entityManager.SetComponentData(mazeEntities[i], new ToCreate
                {
                    Value = true
                });

                entityManager.SetComponentData(mazeEntities[i], new NeedsMeshUpdate
                {
                    Value = true
                });

                entityManager.AddBuffer<IntBufferElement>(mazeEntities[i]);

                entityManager.AddBuffer<Float3BufferElement>(mazeEntities[i]);

                entityManager.SetComponentData(mazeEntities[i], new PipeCaseComponent
                {
                    Value = (int)caseArray[i]
                });

            }
            mazeEntities.Dispose();
            mazeIndex++;

            singleton.numberOfMazes = mazeIndex;
            singleton.mazeNeedsUpdate = true;
        }

    }
}

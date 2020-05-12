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

public class GameManager : MonoBehaviour
{

    [SerializeField] private Mesh shipMesh;
    [SerializeField] private Material shipMaterial;

    // Start is called before the first frame update
    void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

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
            Value = Quaternion.Euler(90,0,0)
        });





        // TODO split these into separate functions

        EntityArchetype mazeArchetype = entityManager.CreateArchetype(
            typeof(IntBufferElement), //Triangles
            typeof(Float3BufferElement), //Vertices
            typeof(Translation),
            typeof(Seed)
        );


        // max amount of mazeEntities (16*16 = 256)
        NativeArray<Entity> mazeEntities = new NativeArray<Entity>(256, Allocator.Temp);

        entityManager.CreateEntity(mazeArchetype, mazeEntities);

        for (int i = 0; i < mazeEntities.Length; i++)
        {
            entityManager.SetComponentData(mazeEntities[i], new Translation
            {
                Value = new Vector3(0, 0, 0)
            });

            entityManager.AddBuffer<IntBufferElement>(mazeEntities[i]);

            entityManager.AddBuffer<Float3BufferElement>(mazeEntities[i]);
        }

        mazeEntities.Dispose();

    }



    // Update is called once per frame
    void Update()
    {
       
    }
}

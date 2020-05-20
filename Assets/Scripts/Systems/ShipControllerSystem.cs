using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class ShipControllerSystem : SystemBase
{
    Singleton singleton;
    Camera camera;

    protected override void OnStartRunning()
    {
        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();
        camera = GameObject.Find("Main Camera").GetComponent<Camera>();

        singleton.shipLocation = new Vector3();
    }

    protected override void OnUpdate()
    {
        if (singleton.gameState == Singleton.GameState.InGame || singleton.gameState == Singleton.GameState.PreGame)
        {
            float deltaTime = Time.DeltaTime;

            float turnAmount = 200f * Input.GetAxis("Horizontal");
            float boostAmount = 30f * Input.GetAxis("Boost");

            if (singleton.gameState == Singleton.GameState.PreGame) boostAmount = 50f;
            
            // apply rotation to all ships based on user input
            Entities.ForEach((ref Rotation rotation, in IsShip isShip) =>
            {
                rotation.Value *= Quaternion.Euler(new Vector3(0f, turnAmount * deltaTime, 0f));

            }).ScheduleParallel();

            CompleteDependency();

            NativeArray<Vector3> shipDirection = new NativeArray<Vector3>(1, Allocator.TempJob);
            NativeArray<Vector3> shipUp = new NativeArray<Vector3>(1, Allocator.TempJob);
            NativeArray<Translation> shipTranslation = new NativeArray<Translation>(1, Allocator.TempJob);
            NativeArray<Quaternion> shipRotation = new NativeArray<Quaternion>(1, Allocator.TempJob);

            int level = singleton.mazeIndex;

            // get position of all ships (we are only using one but it can be used for many if required in the future)
            Entities.ForEach((int entityInQueryIndex, ref LocalToWorld localToWorld, ref Translation translation, in IsShip isShip) =>
            {
                // function to increase speed and thus difficulty over time.
                float speedFunction = (1 + 2 * (math.log10((level / 2) + 1) / 3));

                translation.Value += localToWorld.Forward * (7 + boostAmount) * speedFunction * deltaTime;

                shipTranslation[0 + entityInQueryIndex] = translation;
                shipDirection[0 + entityInQueryIndex] = localToWorld.Forward;
                shipUp[0 + entityInQueryIndex] = localToWorld.Up;
                shipRotation[0 + entityInQueryIndex] = localToWorld.Rotation;

            }).ScheduleParallel();

            CompleteDependency();

            Vector3 shipPos = shipTranslation[0].Value;

            singleton.shipLocation.x = shipPos.x;
            singleton.shipLocation.y = shipPos.y;
            singleton.shipLocation.z = shipPos.z;

            singleton.ship.transform.position = shipPos;

            // if our ship has arrived at the next maze, flag to add another maze
            if (shipPos.z > PipeCases.torusRadius * 32 * (singleton.mazeIndex - 1))
            {
                singleton.toAddNewMaze = true;
            }

            Vector3 shipDir = shipDirection[0];
            Quaternion shipRot = shipRotation[0];


            // set the camera to a position behind the ship parallel to the ship's forward vector 
            Vector3 cameraPos = shipPos + (-10) * shipDir;

            // move the camera up above the ship slightly
            cameraPos = cameraPos + 1f * shipUp[0];

            camera.transform.position = cameraPos;
            //camera.transform.forward = shipDir;

            //set the camera's rotation to the same as the ship
            camera.transform.rotation = shipRot;

            shipUp.Dispose();
            shipTranslation.Dispose();
            shipDirection.Dispose();
            shipRotation.Dispose();
        }
    }
}

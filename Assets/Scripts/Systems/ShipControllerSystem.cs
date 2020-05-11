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

public class ShipControllerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        // TODO simplify using unity's input
        bool isDownPressed = Input.GetKey(KeyCode.S);
        bool isUpPressed = Input.GetKey(KeyCode.W);
        bool isLeftPressed = Input.GetKey(KeyCode.A);
        bool isRightPressed = Input.GetKey(KeyCode.D);

        // apply rotation to all ships based on user input
        Entities.ForEach((ref Rotation rotation, in IsShip isShip) =>
        {
            if (isDownPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(-0.3f, 0f, 0f));
            }
            if (isUpPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(0.3f, 0f, 0f));
            }
            if (isLeftPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(0f, 0f, 0.5f));
            }
            if (isRightPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(0f, 0f, -0.5f));
            }
        }).ScheduleParallel();

        CompleteDependency();

        NativeArray<Vector3> shipDirection = new NativeArray<Vector3>(1, Allocator.TempJob);
        NativeArray<Translation> shipTranslation = new NativeArray<Translation>(1, Allocator.TempJob);
        NativeArray<Quaternion> shipRotation = new NativeArray<Quaternion>(1, Allocator.TempJob);

        // get position of all ships (we are only using one but it can be used for many if required in the future)
        Entities.ForEach((int entityInQueryIndex, ref LocalToWorld localToWorld, in IsShip isShip, in Translation translation) =>
        {
            shipTranslation[0 + entityInQueryIndex] = translation;
            shipDirection[0 + entityInQueryIndex] = localToWorld.Forward;
            shipRotation[0 + entityInQueryIndex] = localToWorld.Rotation;

        }).ScheduleParallel();

        CompleteDependency();

        Vector3 shipPos = shipTranslation[0].Value;

        Vector3 shipDir = shipDirection[0];

        Quaternion shipRot = shipRotation[0];

        // TODO figure out correct way of doing this
        Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();

        // set the camera to a position behind the ship parallel to the ship's forward vector 
        Vector3 cameraPos = shipPos + (-10) * shipDir;
        camera.transform.position = cameraPos;
        //camera.transform.forward = shipDir;
        
        //set the camera's rotation to the same as the ship
        camera.transform.rotation = shipRot;

        shipTranslation.Dispose();
        shipDirection.Dispose();
        shipRotation.Dispose();
    }
}

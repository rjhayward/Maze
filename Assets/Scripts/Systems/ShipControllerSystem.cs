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
                rotation.Value *= Quaternion.Euler(new Vector3(0.3f,0f,0f));
            }
            if (isUpPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(-0.3f, 0f, 0f));
            }
            if (isLeftPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(0f, 0f, 0.3f));
            }
            if (isRightPressed)
            {
                rotation.Value *= Quaternion.Euler(new Vector3(0f, 0f, -0.3f));
            }
        }).ScheduleParallel();

        CompleteDependency();

        NativeArray<Vector3> shipPositionDirection = new NativeArray<Vector3>(2, Allocator.TempJob);
        NativeArray<Quaternion> shipRotation = new NativeArray<Quaternion>(1, Allocator.TempJob);
        NativeArray<Translation> shipTranslation = new NativeArray<Translation>(1, Allocator.TempJob);
        // get position of all ships (we are only using one but it can be used for many if required in the future)
        Entities.ForEach((int entityInQueryIndex, ref LocalToWorld localToWorld, in IsShip isShip, in Translation translation) =>
        {
            shipTranslation[0 + entityInQueryIndex] = translation;
            shipPositionDirection[1 + entityInQueryIndex] = localToWorld.Up;
            shipRotation[0 + entityInQueryIndex] = localToWorld.Rotation;

        }).ScheduleParallel();

        CompleteDependency();



        // TODO figure out correct way of doing this
        Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();

        // get first ship's position and apply the camera's position to be behind it
        Vector3 cameraPosition = shipTranslation[0].Value;
        cameraPosition.x -= 3f;
        cameraPosition.y -= 10f;
        camera.transform.position = cameraPosition;

        //// get first ship's rotation and apply same rotation to camera
        //shipRotation[0].ToAngleAxis(out float angle, out Vector3 axis);
        //camera.transform.RotateAround(shipPositionDirection[0], axis, angle);
        //camera.transform.LookAt(shipPositionDirection[0]);
        //camera.transform.forward = shipPositionDirection[1];

        float3 lookVector = shipTranslation[0].Value - (float3) camera.transform.position;  
        Quaternion rot = Quaternion.LookRotation(lookVector);
        camera.transform.rotation = rot;


        shipTranslation.Dispose();
        shipPositionDirection.Dispose();
        shipRotation.Dispose();
    }
}

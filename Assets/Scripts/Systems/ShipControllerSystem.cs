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
using System;

public class ShipControllerSystem : SystemBase
{
    Singleton singleton;
    Vector3 startAcceleration;
    float pitchField = 0f;
    float rollField = 0f;
    protected override void OnStartRunning()
    {
        Input.gyro.enabled = true;
        singleton = GameObject.Find("Singleton").GetComponent<Singleton>();

        startAcceleration = Input.acceleration;

        singleton.shipLocation = new Vector3();

    }
    private static Quaternion GyroToUnity(Quaternion q)
    {
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }

    protected override void OnUpdate()
    {
        if (singleton.gameState == Singleton.GameState.InGame || singleton.gameState == Singleton.GameState.PreGame)
        {

            bool toMove = false;
            if (Input.touches.Length > 0 || Input.GetKey(KeyCode.Space))
            {
                toMove = true;
                //if (Input.GetTouch(0).phase == TouchPhase.Began)
                //{

                //}
            }

            float deltaTime = Time.DeltaTime;

            // TODO simplify using unity's input
            bool isDownPressed = Input.GetKey(KeyCode.S);
            bool isUpPressed = Input.GetKey(KeyCode.W);
            bool isLeftPressed = Input.GetKey(KeyCode.A);
            bool isRightPressed = Input.GetKey(KeyCode.D);

            //Vector3 acceleration = Input.acceleration;

            //Vector3 normalisedRotation = (Input.gyro.rotationRateUnbiased - startGyro)*Input.gyro.updateInterval;
            Vector3 acceleration = Input.acceleration;
            acceleration /= acceleration.magnitude;
            acceleration -= startAcceleration;

            float pitch = pitchField;
            float roll = rollField;



            pitch = -400 * Mathf.Clamp(acceleration.z, -0.2f, 0.2f);
            roll = -500 * Mathf.Clamp(acceleration.x, -0.15f, 0.15f);

            float absPitch = 5 * (pitch / -400); //makes it between -1 and 1
            float absRoll = (1 / 0.15f) * (roll / -500); //makes it between -1 and 1
            //float absPitch = ChangeInPitch;
            //float absRoll = ChangeInRoll;

            //ChangeInPitch -= 5 * (pitch / -400);
            //ChangeInRoll -= (1 / 0.15f) * (roll / -500);

            float pitchMultiplier = Mathf.Pow(3 * absPitch / 4, 2) + 0.75f;
            float rollMultiplier = Mathf.Pow(3 * absRoll / 4, 2) + 0.75f;

            //float gyroRotationX = normalisedRotation.x;
            //float gyroRotationY = normalisedRotation.y;
            ////float gyroRotationZ = normalisedRotation.z;

            //float pitch = gyroRotationY;

            //pitch -= 90;

            //if (pitch < 90) pitch = 270;
            //if (pitch > 270) pitch = 270;


            //pitch = 360*(((pitch - 90) / 180) - 0.5f);
            //float yaw = Mathf.Clamp(gyroRotationX, 0f, 180f);
            //pitch -= 90;

            float turnAmount = 200f * Input.GetAxis("Horizontal");
            float boostAmount = 30f * Input.GetAxis("Boost");

            if (singleton.gameState == Singleton.GameState.PreGame) boostAmount = 50f;
            
            // apply rotation to all ships based on user input
            Entities.ForEach((ref Rotation rotation, in IsShip isShip) =>
            {
                //rotation.Value *= Quaternion.Euler(new Vector3(gyroRotX, gyroRotY, 0f));
                if (false)
                {
                    Vector3 newRotation = new Vector3(pitch * pitchMultiplier * deltaTime, 0f, roll * rollMultiplier * deltaTime);//new Vector3(-pitch * deltaTime, -0f * deltaTime, 0f * deltaTime);

                    rotation.Value *= Quaternion.Euler(newRotation);// - oldRotation);
                }
                rotation.Value *= Quaternion.Euler(new Vector3(0f, turnAmount * deltaTime, 0f));
                //if (isDownPressed)
                //{
                //    rotation.Value *= Quaternion.Euler(new Vector3(-0.04f, 0f, 0f));
                //}
                //if (isUpPressed)
                //{
                //    rotation.Value *= Quaternion.Euler(new Vector3(0.04f, 0f, 0f));
                //}
                //if (isLeftPressed)
                //{
                //    rotation.Value *= Quaternion.Euler(new Vector3(0f, -0.06f, 0f));
                //}
                //if (isRightPressed)
                //{
                //    rotation.Value *= Quaternion.Euler(new Vector3(0f, 0.06f, 0f));
                //}
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
                //if (toMove)
                //{
                //    translation.Value += localToWorld.Forward * 20 * deltaTime;
                //}
                //else
                //{
                //    translation.Value += localToWorld.Forward * 2 * deltaTime;
                //}
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

            if (shipPos.z > PipeCases.torusRadius * 32 * (singleton.mazeIndex - 1))
            {
                singleton.toAddNewMaze = true;
            }

            Vector3 shipDir = shipDirection[0];


            Quaternion shipRot = shipRotation[0];

            // TODO figure out correct way of doing this
            Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();

            // set the camera to a position behind the ship parallel to the ship's forward vector 
            Vector3 cameraPos = shipPos + (-10) * shipDir;

            cameraPos = cameraPos + 1f * shipUp[0];

            camera.transform.position = cameraPos;
            //camera.transform.forward = shipDir;

            //set the camera's rotation to the same as the ship
            camera.transform.rotation = shipRot;

            shipTranslation.Dispose();
            shipDirection.Dispose();
            shipRotation.Dispose();
        }
    }
}

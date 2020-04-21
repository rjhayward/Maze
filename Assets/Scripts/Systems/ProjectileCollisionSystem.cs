using UnityEngine;
using UnityEngine.Jobs;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Jobs;
using Unity.Mathematics;

public class ProjectileCollisionSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        Entities.WithNone<IsShip>() //exclude IsShip
            .ForEach((ref Translation translation, ref Rotation rotation) =>
        {

        }).ScheduleParallel();

    }
}
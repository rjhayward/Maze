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

public class SimpleMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        // system just moves any object with these filters forward by a fixed amount each frame
        Entities.WithNone<IsAlive>() //exclude IsAlive
            .ForEach((ref LocalToWorld localToWorld, ref Translation translation) =>
        {
            translation.Value += localToWorld.Forward * 2 * deltaTime;
        }).ScheduleParallel();

    }
}

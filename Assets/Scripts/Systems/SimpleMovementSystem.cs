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

        Entities.WithNone<IsAlive>() //exclude IsAlive
            .ForEach((ref Translation translation, ref Direction direction) =>
        {
            translation.Value += direction.Value * deltaTime; 
        }).ScheduleParallel();

    }
}

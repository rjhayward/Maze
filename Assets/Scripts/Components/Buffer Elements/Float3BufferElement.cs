using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

//[InternalBufferCapacity(8)]
public struct Float3BufferElement : IBufferElementData
{
    public float3 Value;
}

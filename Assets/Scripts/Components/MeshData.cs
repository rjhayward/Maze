using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
public struct MeshData : IComponentData
{
    public DynamicBuffer<float3> Vertices;
    public DynamicBuffer<IntBufferElement> Triangles;
}

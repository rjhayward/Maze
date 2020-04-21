using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

//[InternalBufferCapacity(8)]
public struct IntBufferElement : IBufferElementData
{
    public int Value;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public struct Seed : IComponentData
{
    public NativeString64 Value;
}

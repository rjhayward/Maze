using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;

public struct NeedsMeshUpdate : IComponentData
{
    public bool Value;
}

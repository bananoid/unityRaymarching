using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct RoomObjectComponent : IComponentData
{
    public float weight;
    public float3 up;
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class GlobalUpSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref RoomComponent roomComp) =>
        {
            roomComp.size += 1f * Time.DeltaTime;
        });
    }
}

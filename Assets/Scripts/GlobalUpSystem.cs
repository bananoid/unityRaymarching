using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class GlobalUpSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        Debug.Log("cacca");

        Entities.ForEach((ref RoomComponent roomComp) =>
        {
            roomComp.size += 1f * Time.DeltaTime;
            Debug.Log(roomComp.size);
        });
    }
}

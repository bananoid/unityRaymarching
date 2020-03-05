using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using System.Numerics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Rendering;
using UnityEngine.U2D;

public class RoomsSystem : ComponentSystem
{
    private float spawnTimer;
    private Random random;

    protected override void OnCreate()
    {
        random = new Random(56);    
    }

    protected override void OnUpdate()
    {

        spawnTimer -= Time.DeltaTime;

        if (spawnTimer < 0)
        {
            spawnTimer = 10.0f;

            Entities.ForEach((
                ref RoomsDescriptionComponent roomsDesc
            ) =>
            {
                SpownObjects(roomsDesc.objPrefab, 200);
            });
        }
    }

    void SpownObjects(Entity objPrefab, int count)
    {
        NativeArray<Entity> objs = new NativeArray<Entity>(count, Allocator.Temp);
        EntityManager.Instantiate(objPrefab, objs);

        float spawnRadius = 5;
        foreach(var obj in objs)
        {

            EntityManager.SetComponentData(obj,
                new RoomObjectComponent
                {
                    weight = random.NextFloat(0.3f, 2f),
                    up = math.up()
                }
            );

            var pos = random.NextFloat3(-spawnRadius, spawnRadius);
            EntityManager.SetComponentData(obj,
                new Translation { Value = pos }
            );

            if(random.NextFloat() > 0.8) { 
                float startVal = random.NextFloat(5.0f, 6.0f);
                float endVal = random.NextFloat(2.0f, 4.0f);

                EntityManager.AddComponentData(obj, new ImpulseData
                {
                    Start = startVal,
                    End = endVal,
                    Time = 0f,
                    Speed = 0.02f
                });


                EntityManager.AddComponentData(obj, new Scale
                {
                    Value = startVal * 2f,
                });

            }

        }

        objs.Dispose();
    }
}

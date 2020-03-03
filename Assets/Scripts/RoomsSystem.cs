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
            spawnTimer = 1.0f;

            Entities.ForEach((
                ref RoomsDescriptionComponent roomsDesc
            ) =>
            {
                SpownObjects(roomsDesc.objPrefab, 10);
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
            var pos = random.NextFloat3(-spawnRadius, spawnRadius);

            var s = random.NextFloat(0.1f, 1.0f); 
            var scale = EntityManager
                .GetComponentData<CompositeScale>(objPrefab)
                .Value;
            scale *= s;
            scale[3].w = 1;

            EntityManager.SetComponentData(obj,
                new Translation { Value = pos }
            );

            var renderBounds = EntityManager
                .GetComponentData<RenderBounds>(objPrefab)
                .Value;
            renderBounds.Extents = new float3(scale[0].x);
            EntityManager.SetComponentData(obj,
                new RenderBounds { Value = renderBounds }
            );


            EntityManager.SetComponentData(obj,
                new RoomObjectComponent {
                    weight = random.NextFloat(0.3f, 2f  ),
                    up = math.up()
                }
            ); ;


            var pc = EntityManager
                .GetComponentData<PhysicsCollider>(objPrefab);

            //if (pc.ColliderPtr->Type != ColliderType.Sphere) return;

            //SphereCollider* scPtr = (SphereCollider*)pc.ColliderPtr;
            //scPtr->Geometry.Radius = s;
        }

        objs.Dispose();
    }
}

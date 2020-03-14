using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
// using System.Numerics;
// using Unity.Physics;
// using Unity.Physics.Authoring;
// using Unity.Rendering;

public class RoomsSystem : ComponentSystem
{
    private Random random;

    protected override void OnCreate()
    {
        random = new Random(56);    
    }

    protected override void OnUpdate()
    {
        
    }

    public void GenerateRooms(List<RoomData> roomsData){
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entities.ForEach((
            Entity entity, 
            ref RoomObjectComponent roomsDesc
        ) =>
        {
            entityManager.DestroyEntity(entity);
        });

        int count = random.NextInt(20,100);
        SpownObjects(RoomsPrefabConverter.roomBoxEntity, count);           
    } 

    void SpownObjects(Entity objPrefab, int count)
    {
        NativeArray<Entity> objs = new NativeArray<Entity>(count, Allocator.Temp);
        EntityManager.Instantiate(objPrefab, objs);

        float spawnRadius = 2f;
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

            if(random.NextFloat() > 0) { 
                float startVal = random.NextFloat(1.0f, 10.0f) * 5/count;
                startVal = 0.3f;
                float endVal = startVal * 0.6f;
                // endVal = startVal;
                 

                EntityManager.AddComponentData(obj, new ImpulseData
                {
                    Start = startVal,
                    End = endVal,
                    Time = 0f,
                    Speed = 2f
                });


                EntityManager.AddComponentData(obj, new Scale
                {
                    Value = startVal,
                });

                // EntityManager.AddSharedComponentData(obj, new TimeData)

            }

        }

        objs.Dispose();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.Physics;

public class RoomsController : MonoBehaviour
{
    [SerializeField] private Mesh mesh;
    [SerializeField] private UnityEngine.Material material;

    // Start is called before the first frame update
    void Start()
    {
        EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

 
        EntityArchetype entityArchetype = entityManager.CreateArchetype(
            typeof(RoomComponent),

            typeof(LocalToWorld),
            typeof(Rotation),
            typeof(Translation),
            typeof(NonUniformScale),
            typeof(RenderBounds),
            //typeof(Phys)

            typeof(RenderMesh)

        );

        NativeArray<Entity> entities = new NativeArray<Entity>(10, Allocator.Temp);
        entityManager.CreateEntity(entityArchetype, entities);
        foreach (Entity entity in entities)
        {

            entityManager.SetComponentData(entity, new RoomComponent
            {
                size = 1
            });

            entityManager.SetComponentData(entity, new LocalToWorld
            {
                Value = Matrix4x4.identity
            });

            entityManager.SetComponentData(entity, new RenderBounds
            {
                Value = new AABB
                {
                    Center = new Vector3(0, 0, 0),
                    Extents = new Vector3(4, 4, 4)
                }
            });

            entityManager.SetComponentData(entity, new NonUniformScale
            {
                Value = new Vector3(2,2,2)
            });


            entityManager.SetSharedComponentData(entity, new RenderMesh
            {
                mesh = mesh,
                material = material,
            });
        }

        entities.Dispose();
    }
}

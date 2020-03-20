using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Rendering;

public class RoomsSystem : ComponentSystem
{
    private Random random;

    public float4 lightDesc = new float4(0,0,0,100);

    protected override void OnCreate()
    {
        random = new Random(56);    
    }

    protected override void OnUpdate()
    {
        Entities.ForEach( (Entity entity, ref RoomObjectComponent roomObject) =>
       {    
            var renderMesh = EntityManager.GetSharedComponentData<RenderMesh>(entity); 
            renderMesh.material.SetVector("lightDesc", lightDesc);
       });

    }


}

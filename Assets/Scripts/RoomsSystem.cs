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
}

using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using SphereCollider = Unity.Physics.SphereCollider;

public struct ImpulseData : IComponentData
{
    public float Start;
    public float End;
    public float Time;
    public float Speed;
    public float Value;
}

[UpdateBefore(typeof(BuildPhysicsWorld))]
public class ImpulseSystem : JobComponentSystem
{
    private struct ImpulseJob : IJobForEach<PhysicsCollider, ImpulseData, Scale>
    {

        float Out(float k)
        {
            return 1f + ((k -= 1f) * k * k * k * k);
        }

        float QuaImpulse(float k, float x)
        {
            return 2.0f * math.sqrt(k) * x / (1.0f + k * x * x);
        }

        float Sinc(float x, float k)
        {
            float a = math.PI * (k * x - 1.0f);
            return math.sin(a) / a;
        }

        public unsafe void Execute(
            ref PhysicsCollider collider,
            ref ImpulseData radius,
            ref Scale scale)
        {
            // make sure we are dealing with spheres
            if (collider.ColliderPtr->Type != ColliderType.Sphere) return;

            SphereCollider* scPtr = (SphereCollider*)collider.ColliderPtr;

            radius.Time += radius.Speed;

            float t = QuaImpulse(100f, radius.Time);
            radius.Value = math.lerp( radius.End, radius.Start, t);

            radius.Time = radius.Time % 1f;

            // update the collider geometry
            var sphereGeometry = scPtr->Geometry;
            sphereGeometry.Radius = radius.Value;
            scPtr->Geometry = sphereGeometry;

            scale = new Scale() { Value = radius.Value * 1f };
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle job = new ImpulseJob().Schedule(this, inputDeps);

        return job;
    }
}

public class ImpulseTriggerSystem : ComponentSystem
{
    protected override void OnUpdate()
    {
        
    }
}

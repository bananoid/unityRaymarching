using System;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
// using UnityEngine;
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
        public float deltaTime;
        public unsafe void Execute(
            ref PhysicsCollider collider,
            ref ImpulseData scaleData,
            ref Scale scale)
        {
            // make sure we are dealing with spheres


            scaleData.Time += scaleData.Speed * deltaTime;

            float t = QuaImpulse(100f, scaleData.Time);
            scaleData.Value = math.lerp( scaleData.End, scaleData.Start, t);

            scaleData.Time = scaleData.Time % 1f;

            scale = new Scale() { Value = scaleData.Value * 2f };
            
            if (collider.ColliderPtr->Type == ColliderType.Sphere) {
                SphereCollider* scPtr = (SphereCollider*)collider.ColliderPtr;
                var geometry = scPtr->Geometry;
                geometry.Radius = scaleData.Value;
                scPtr->Geometry = geometry;
            }
            
            if (collider.ColliderPtr->Type == ColliderType.Box) {
                BoxCollider* scPtr = (BoxCollider*)collider.ColliderPtr;
                var geometry = scPtr->Geometry;
                geometry.Size = scaleData.Value*4;
                scPtr->Geometry = geometry;
            }

            if (collider.ColliderPtr->Type == ColliderType.Cylinder) {
                CylinderCollider* scPtr = (CylinderCollider*)collider.ColliderPtr;
                var geometry = scPtr->Geometry;
                geometry.Height = scaleData.Value;
                geometry.Radius = scaleData.Value * 0.2f;
                scPtr->Geometry = geometry;
            }

        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {       
        JobHandle job = new ImpulseJob { 
            deltaTime = UnityEngine.Time.deltaTime
        }.Schedule(this, inputDeps);
    
        return job;
    }
}

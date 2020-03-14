using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Jobs;

//[AlwaysSynchronizeSystem]
public class AttractSystem : JobComponentSystem
{
    public struct Parameters {
        public float3 center;
        public float maxDistanceSqrd;
        public float minDistanceSqrd;
        public float strength;
        public float vortexStrength;
    }

    public Parameters parameters;

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        var par = this.parameters;

        var job = Entities.ForEach(
        (
            ref RoomObjectComponent obj,
            ref PhysicsVelocity velocity,
            ref Translation position,
            ref Rotation rotation) =>
        {

            float3 diff = par.center - position.Value;
            float3 vortexForce = math.cross(obj.up, diff);
            
            float distSqrd = math.lengthsq(diff);
            // float deltaTime = UnityEngine.Time.deltaTime;

            if (distSqrd < par.maxDistanceSqrd && distSqrd > par.minDistanceSqrd)
            {
                // Alter linear velocity
                velocity.Linear +=
                    obj.weight * par.vortexStrength * vortexForce * (1 / distSqrd)
                    +
                    obj.weight * par.strength * (diff / math.sqrt(distSqrd));
                float magnitudo =  math.length(velocity.Linear);
                magnitudo = math.min(magnitudo,2);
                velocity.Linear = math.normalize(velocity.Linear) * magnitudo;
            }
        }).Schedule(inputDeps);

        return job;
    }
};
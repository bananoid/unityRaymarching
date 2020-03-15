using Unity.Entities;
using UnityEngine;

public class AttractController : MonoBehaviour
{
    public float maxDistance = 3;
    public float minDistance = 0.1f;
    public float strength = 1;
    public float vortexStrength = 1;
    public float maxSpeed = 2;

    void Update()
    {
        var vortex = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<AttractSystem>();

        vortex.parameters.center = transform.position;
        vortex.parameters.maxDistanceSqrd = maxDistance * maxDistance;
        vortex.parameters.minDistanceSqrd = minDistance * minDistance;
        vortex.parameters.strength = strength;
        vortex.parameters.vortexStrength = vortexStrength;
        vortex.parameters.maxSpeed = maxSpeed;
    }
}
  
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class RoomsPrefabConverter : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
{
    public GameObject roomBox;
    public static Entity roomBoxEntity;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(roomBox);
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        RoomsPrefabConverter.roomBoxEntity = conversionSystem.GetPrimaryEntity(roomBox);
    }

}

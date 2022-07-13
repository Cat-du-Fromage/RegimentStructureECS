using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace KaizerWald
{
    [DisallowMultipleComponent]
    public class Authoring_PlacementConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Tag_Placement>(entity);
            dstManager.AddComponent<DisableRendering>(entity);
        }
    }
}
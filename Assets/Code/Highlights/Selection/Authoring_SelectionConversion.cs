using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace KaizerWald
{
    [DisallowMultipleComponent]
    public class Authoring_SelectionConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Tag_Uninitialize>(entity);
            dstManager.AddComponent<Tag_Selection>(entity);
            dstManager.AddComponent<Flag_Selection>(entity);
            dstManager.AddComponent<Filter_Selection>(entity);
            dstManager.AddComponent<DisableRendering>(entity);
        }
    }
}
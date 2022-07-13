using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
namespace KaizerWald
{
    [DisallowMultipleComponent]
    public class Authoring_PreselectionConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponent<Tag_Uninitialize>(entity);
            dstManager.AddComponent<Tag_Preselection>(entity);
            dstManager.AddComponent<Flag_Preselection>(entity);
            dstManager.AddComponent<Filter_Preselection>(entity);
            dstManager.AddComponent<DisableRendering>(entity);
        }
    }
}
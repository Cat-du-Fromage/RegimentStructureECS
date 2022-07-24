using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    [DisallowMultipleComponent]
    public class Authoring_UnitConversion : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            //dstManager.AddComponent<Tag_Uninitialize>(entity);
            dstManager.AddComponent<Tag_Unit>(entity);
            //dstManager.AddComponent<Data_Regiment>(entity);
        }
    }
}
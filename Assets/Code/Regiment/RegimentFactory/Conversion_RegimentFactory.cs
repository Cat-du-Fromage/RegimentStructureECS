using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Hybrid.Internal;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    [Serializable]
    public struct RegimentOrders
    {
        public int number;
        public GameObject regimentPrefab;
    }
    
    public struct TempData_RegimentOrders : IBufferElementData
    {
        public int Number;
        public Entity RegimentPrefab;
    }
    
    [UpdateAfter(typeof(GameObjectAfterConversionGroup))]
    [DisallowMultipleComponent]
    public class Conversion_RegimentFactory : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        [SerializeField] private RegimentOrders[] Orders;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            DynamicBuffer<TempData_RegimentOrders> buffer = dstManager.AddBuffer<TempData_RegimentOrders>(entity);
            buffer.EnsureCapacity(Orders.Length);
            for (int i = 0; i < Orders.Length; i++)
            {
                buffer.Add(new TempData_RegimentOrders()
                {
                    Number = Orders[i].number,
                    RegimentPrefab = conversionSystem.GetPrimaryEntity(Orders[i].regimentPrefab)
                });
            }
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            for (int i = 0; i < Orders.Length; i++)
            {
                referencedPrefabs.Add(Orders[i].regimentPrefab);
            }
        }
    }
}
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
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
        public bool IsPlacer;
        public int Number;
        public Entity RegimentPrefab;
        public float3 SpawnStartPosition;
    }
    
    [UpdateAfter(typeof(GameObjectAfterConversionGroup))]
    [DisallowMultipleComponent]
    public class Conversion_RegimentFactory : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public bool IsPlayer;
        public RegimentOrders[] Orders;
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            DynamicBuffer<TempData_RegimentOrders> buffer = dstManager.AddBuffer<TempData_RegimentOrders>(entity);
            buffer.EnsureCapacity(Orders.Length);
            
            for (int i = 0; i < Orders.Length; i++)
            {
                buffer.Add(new TempData_RegimentOrders()
                {
                    IsPlacer = this.IsPlayer,
                    Number = Orders[i].number,
                    RegimentPrefab = conversionSystem.GetPrimaryEntity(Orders[i].regimentPrefab),
                    SpawnStartPosition = (float3)transform.position
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
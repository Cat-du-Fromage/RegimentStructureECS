using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Authoring;
using UnityEngine.Rendering;
using static Unity.Mathematics.math;
using float3 = Unity.Mathematics.float3;

namespace KaizerWald
{
    [Serializable]
    public struct RegimentSpawnerData
    {
        public int Number;
        public GameObject Unit;
        public GameObject Prefab_Placement;
    }
    
    [DisallowMultipleComponent]
    public class RegimentSpawner : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public List<RegimentSpawnerData> RegimentData;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            int realIndex = 0;
            for (int i = 0; i < RegimentData.Count; i++)
            {
                conversionSystem.DeclareDependency(gameObject, RegimentData[i].Unit);
                conversionSystem.DeclareDependency(gameObject, RegimentData[i].Prefab_Placement);
                //Unit
                Entity prefabEntity = conversionSystem.GetPrimaryEntity(RegimentData[i].Unit);
                Entity prefabPlacement = conversionSystem.GetPrimaryEntity(RegimentData[i].Prefab_Placement);
                RegimentSpawnerData data = RegimentData[i];
                CreateRegiments(dstManager, data, prefabEntity, prefabPlacement, realIndex);
                realIndex += data.Number;
            }
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            for (int i = 0; i < RegimentData.Count; i++)
            {
                referencedPrefabs.Add(RegimentData[i].Unit);
                referencedPrefabs.Add(RegimentData[i].Prefab_Placement);
            }
        }

        private void CreateRegiments(EntityManager em, RegimentSpawnerData regimentInfo, Entity unit, Entity placement, int index)
        {
            for (int i = 0; i < regimentInfo.Number; i++)
            {
                Entity newRegiment = em.CreateEntity();

                em.SetName(newRegiment, $"Regiment_{regimentInfo.Unit.name}_{newRegiment.Index}");

                RegimentData data = new RegimentData {Index = index, NumUnits = 120, UnitPrefab = unit, PlacementPrefab = placement};
                em.AddComponentData(newRegiment, data);

                //DynamicBuffer<Child> buffer = em.AddBuffer<Child>(newRegiment);
                DynamicBuffer<LinkedEntityGroup> buffer = em.AddBuffer<LinkedEntityGroup>(newRegiment);
                buffer.EnsureCapacity(data.NumUnits);

                em.AddComponent<Tag_Uninitialize>(newRegiment);
            }
        }
    }

}



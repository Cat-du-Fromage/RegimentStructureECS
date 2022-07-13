using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace KaizerWald
{
    public class MonoAuthoring_Regiment : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        [Header("Prefabs")]
        public GameObject UnitPrefab;
        public GameObject PrefabPlacement;

        [Header("UnitStats")]
        public float Speed = 1f;
        public Vector3 UnitSize = Vector3.one;

        [Header("RegimentStats")] 
        public int BaseNumUnits = 20;
        public int MinRow = 4;
        public int MaxRow = 10;
        
        public float OffsetBetweenUnits = 0.5f;
        
        public float SpaceBetweenUnitsX => UnitSize.x + OffsetBetweenUnits;
        public float SpaceBetweenUnitsZ => UnitSize.z + OffsetBetweenUnits;
        
        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(UnitPrefab);
            referencedPrefabs.Add(PrefabPlacement);
        }
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            Entity unit = conversionSystem.GetPrimaryEntity(UnitPrefab);
            Entity placement = conversionSystem.GetPrimaryEntity(PrefabPlacement);
            
            dstManager.AddComponent<Tag_Regiment>(entity);
            dstManager.AddComponentData(entity, new Data_PrefabsRegiment(){UnitPrefab = unit, PrefabPlacement = placement});
            
            dstManager.AddComponentData(entity, new Data_RegimentClass()
            {
                Speed = this.Speed,
                BaseNumUnits = this.BaseNumUnits,
                MaxRow = MaxRow,
                MinRow = MinRow,
                SpaceBetweenUnitsX = UnitSize.x + OffsetBetweenUnits,
                SpaceBetweenUnitsZ = UnitSize.z + OffsetBetweenUnits,
                UnitSize = UnitSize
            });
        }
    }
    
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public partial class RegimentConversionGoSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Debug.Log("Pass RegimentConversionGoSystem");
            /*
            // Iterate over all authoring components of type FooAuthoring
            Entities.ForEach((FooAuthoring input) =>
            {
                // Get the destination world entity associated with the authoring GameObject
                //var entity = GetPrimaryEntity(input);

                // Do the conversion and add the ECS component
                //DstEntityManager.AddComponentData(entity);
            });
            */
        }
    }
}

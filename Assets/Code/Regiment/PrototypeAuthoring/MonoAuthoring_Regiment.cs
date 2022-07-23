using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
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

            dstManager.AddComponentData(entity, new Data_NumUnits() {Value = BaseNumUnits});
            dstManager.AddComponentData(entity, new Data_MinLine() {Value = MinRow});
            dstManager.AddComponentData(entity, new Data_MaxLine() {Value = MaxRow});
            dstManager.AddComponentData(entity, new Data_UnitsPerLine() {Value = MinRow});
            dstManager.AddComponentData(entity, new Data_UnitSize() {Value = UnitSize});
            dstManager.AddComponentData(entity, new Data_SpaceBetweenUnitX() {Value = UnitSize.x + OffsetBetweenUnits});
            dstManager.AddComponentData(entity, new Data_SpaceBetweenUnitZ() {Value = UnitSize.z + OffsetBetweenUnits});
            
            dstManager.AddComponentData(entity, new NumberUnits() {Value = BaseNumUnits});
            dstManager.AddComponentData(entity, new MinLine() {Value = MinRow});
            dstManager.AddComponentData(entity, new MaxLine() {Value = MaxRow});
            dstManager.AddComponentData(entity, new UnitsPerLine() {Value = MinRow});
            dstManager.AddComponentData(entity, new UnitsLastLine() {Value = MinRow});
            dstManager.AddComponentData(entity, new NumberLine() {Value = (int)math.ceil(BaseNumUnits / (float)MinRow)});
            
            //float formationNumLine = BaseNumUnits / (float)MinRow;
            //int totalLine = (int)math.ceil(BaseNumUnits / (float)MinRow);
            dstManager.AddComponentData(entity, new Data_NumLine() {Value = (int)math.ceil(BaseNumUnits / (float)MinRow)});
        }
    }
}

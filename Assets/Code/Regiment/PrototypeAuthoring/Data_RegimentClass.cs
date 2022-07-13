using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Data_RegimentClass : IComponentData
    {
        public float Speed;
        public float3 UnitSize;
        
        public int BaseNumUnits;
        public int MinRow;
        public int MaxRow;

        public float SpaceBetweenUnitsX;
        public float SpaceBetweenUnitsZ;
    }

    public struct Data_PrefabsRegiment : IComponentData
    {
        public Entity UnitPrefab;
        public Entity PrefabPlacement;
    }
}
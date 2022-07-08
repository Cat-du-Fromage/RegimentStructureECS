using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    public struct RegimentData : IComponentData
    {
        public int Index;
        public int NumUnits;
        public Entity UnitPrefab;
    }
}
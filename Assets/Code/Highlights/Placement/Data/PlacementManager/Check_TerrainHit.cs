using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Check_TerrainHit : IComponentData
    {
        public bool IsValid;
    }
}
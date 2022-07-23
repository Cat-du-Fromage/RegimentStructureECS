using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    [WriteGroup(typeof(NumberUnits))]
    public struct UnitsPerLine : IComponentData
    {
        public int Value;
    }
}
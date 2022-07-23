using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    public struct NumberUnits : IComponentData
    {
        public int Value;
    }
}
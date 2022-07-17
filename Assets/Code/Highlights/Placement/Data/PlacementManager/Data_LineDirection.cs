using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Data_LineDirection : IComponentData
    {
        public float3 Value;
    }
}
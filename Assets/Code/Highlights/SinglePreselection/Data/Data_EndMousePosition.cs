using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Data_EndMousePosition : IComponentData
    {
        public float2 Value;
    }
}
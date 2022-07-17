using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Buffer_Destinations : IBufferElementData
    {
        public float3 Position;
    }
}
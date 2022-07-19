using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Buffer_Destinations : IBufferElementData
    {
        public float3 Position;
        
        public static implicit operator float3(Buffer_Destinations e)
        {
            return e.Position;
        }

        public static implicit operator Buffer_Destinations(float3 e)
        {
            return new Buffer_Destinations {Position = e};
        }
    }
}
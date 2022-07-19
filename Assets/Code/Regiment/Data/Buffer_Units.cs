using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Buffer_Units : IBufferElementData
    {
        public Entity Value;
        
        public static implicit operator Entity(Buffer_Units e)
        {
            return e.Value;
        }

        public static implicit operator Buffer_Units(Entity e)
        {
            return new Buffer_Units {Value = e};
        }
    }
}
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    public struct Buffer_Units : IBufferElementData
    {
        public Entity Unit;
    }
}
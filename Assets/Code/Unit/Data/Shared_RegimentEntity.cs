using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    public struct Shared_RegimentEntity : ISharedComponentData
    {
        public Entity Value;
    }
}
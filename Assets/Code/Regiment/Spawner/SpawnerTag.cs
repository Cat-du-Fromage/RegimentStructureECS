using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    public struct SpawnerTag : IComponentData { }
    
    public struct TUninitialize : IComponentData { }
}
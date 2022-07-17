using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Filter_PlacementUpdate : IComponentData
    {
        public bool DidChange;
    }
}
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Filter_Selection : IComponentData
    {
        public bool DidChange;
    }
}
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public struct Flag_Preselection : IComponentData
    {
        public bool IsActive;
    }
}
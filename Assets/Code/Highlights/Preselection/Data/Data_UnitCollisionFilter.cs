using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

namespace KaizerWald
{
    public struct Data_UnitCollisionFilter : IComponentData
    {
        public CollisionFilter Value;
    }
}
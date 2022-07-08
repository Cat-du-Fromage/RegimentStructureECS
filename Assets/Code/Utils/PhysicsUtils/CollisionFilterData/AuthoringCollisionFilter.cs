using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;

[Serializable]
public struct AuthoringCollisionFilter : IComponentData
{
    public CollisionFilter CollisionFilter;
}

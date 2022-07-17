using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    public static class SphereCastUtils
    {
        public static Entity SphereCastAllClosest(this CollisionWorld collisionWorld, in float3 origin, in float3 direction, float radius, float distance, int layerMask)
        {
            CollisionFilter filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << layerMask, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            };
            
            ClosestHitCollector<ColliderCastHit> hitCollector = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = collisionWorld.SphereCastCustom(origin, radius, direction, distance, ref hitCollector, filter);
            
            return !isHit ? Entity.Null : hitCollector.ClosestHit.Entity;
        }
        
        public static Entity SphereCastAllClosest(this CollisionWorld collisionWorld, in float3 origin, in float3 direction, float radius, float distance, CollisionFilter filter)
        {
            ClosestHitCollector<ColliderCastHit> hitCollector = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = collisionWorld.SphereCastCustom(origin, radius, direction, distance, ref hitCollector, filter);
            
            return !isHit ? Entity.Null : hitCollector.ClosestHit.Entity;
        }

        public static JobHandle SingleSphereCast(this CollisionWorld world, 
            in float3 origin, 
            float radius, 
            in float3 direction,
            NativeReference<Entity> hitResult,
            float distance, 
            in CollisionFilter filter, 
            JobHandle dependency = default)
        {

            JSingleSphereCast cast = new JSingleSphereCast
            {
                Radius = radius,
                Distance = distance,
                Origin = origin,
                Direction = direction,
                Filter = filter,
                World = world,
                RegimentHit = hitResult
            };
            return cast.Schedule(dependency);
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    public struct JSphereCast : IJobFor
    {
        [ReadOnly] public float radius;
        [ReadOnly] public float distance;
        [ReadOnly] public float3 origin;
        [ReadOnly] public float3 direction;
        [ReadOnly] public CollisionFilter filter;
        [ReadOnly] public CollisionWorld world;
        [WriteOnly] public NativeArray<Entity> results;

        public unsafe void Execute(int index)
        {
            ClosestHitCollector<ColliderCastHit> hitCollector = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = world.SphereCastCustom(origin, radius, direction, distance, ref hitCollector, filter);
            results[index] = hitCollector.ClosestHit.Entity;
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    public struct JSingleSphereCast : IJob
    {
        [ReadOnly] public float Radius;
        [ReadOnly] public float Distance;
        [ReadOnly] public float3 Origin;
        [ReadOnly] public float3 Direction;
        [ReadOnly] public CollisionFilter Filter;
        [ReadOnly] public CollisionWorld World;
        [WriteOnly] public NativeReference<Entity> RegimentHit;

        
        public unsafe void Execute()
        {
            ClosestHitCollector<ColliderCastHit> hitCollector = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = World.SphereCastCustom(Origin, Radius, Direction, Distance, ref hitCollector, Filter);
            RegimentHit.Value = hitCollector.ClosestHit.Entity;
        }
    }
}

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

namespace KaizerWald
{
    public static class SphereCastUtils
    {
        public static Entity SphereCastAllClosest(this CollisionWorld collisionWorld,  UnityEngine.Ray ray, float radius, float distance, int layerMask)
        {
            CollisionFilter filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << layerMask, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            };
            
            ClosestHitCollector<ColliderCastHit> test = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = collisionWorld.SphereCastCustom(ray.origin,radius,ray.direction, distance, ref test, filter);
            
            return !isHit ? Entity.Null : test.ClosestHit.Entity;
        }
        
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
            float3 origin, 
            float radius, 
            float3 direction,
            NativeReference<Entity> hitResult,
            float distance, 
            CollisionFilter filter, 
            JobHandle dependency = default)
        {

            JSingleSphereCast cast = new JSingleSphereCast
            {
                radius = radius,
                distance = distance,
                origin = origin,
                direction = direction,
                filter = filter,
                world = world,
                regimentHit = hitResult
            };
            return cast.Schedule(dependency);
        }
    }
    
    [BurstCompile]
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
            RaycastHit hit;
            ClosestHitCollector<ColliderCastHit> hitCollector = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = world.SphereCastCustom(origin, radius, direction, distance, ref hitCollector, filter);
            results[index] = hitCollector.ClosestHit.Entity;
        }
    }
    
    [BurstCompile]
    public struct JSingleSphereCast : IJob
    {
        [ReadOnly] public float radius;
        [ReadOnly] public float distance;
        [ReadOnly] public float3 origin;
        [ReadOnly] public float3 direction;
        [ReadOnly] public CollisionFilter filter;
        [ReadOnly] public CollisionWorld world;
        [WriteOnly] public NativeReference<Entity> regimentHit;

        
        public unsafe void Execute()
        {
            ClosestHitCollector<ColliderCastHit> hitCollector = new ClosestHitCollector<ColliderCastHit>(4);
            bool isHit = world.SphereCastCustom(origin, radius, direction, distance, ref hitCollector, filter);
            regimentHit.Value = hitCollector.ClosestHit.Entity;
        }
    }
}

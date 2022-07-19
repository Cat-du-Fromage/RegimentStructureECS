using System.Collections;
using System.Collections.Generic;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Jobs;
using Unity.Physics.Systems;
using Unity.Physics.Extensions;
using UnityEngine;

namespace KaizerWald
{
    public static class PhysicsUtils
    {
        public static CollisionFilter GetCollisionFilter(EntityManager em, Entity entity)
        {
            PhysicsCollider collider = em.GetComponentData<PhysicsCollider>(entity);
            Assert.IsTrue(collider.Value.Value.CollisionType == CollisionType.Convex);
            CollisionFilter filter;
            unsafe
            {
                ConvexCollider* header = (ConvexCollider*)collider.ColliderPtr;
                filter = header->Filter;
            }
            return filter;
        }
        
        
        public static void SetCollisionFilter(EntityManager em, Entity entity, int belongsTo, int collidesWith)
        {
            PhysicsCollider collider = em.GetComponentData<PhysicsCollider>(entity);
            Assert.IsTrue(collider.Value.Value.CollisionType == CollisionType.Convex);
     
            unsafe
            {
                ConvexCollider* header = (ConvexCollider*)collider.ColliderPtr;
                CollisionFilter filter = header->Filter;
     
                filter.BelongsTo = 1u << belongsTo;
                filter.CollidesWith = 1u << collidesWith;
     
                header->Filter = filter;
            }
        }
        
        public static void AddToCollisionFilter(EntityManager em, Entity entity, int belongsTo)
        {
            PhysicsCollider collider = em.GetComponentData<PhysicsCollider>(entity);
            Assert.IsTrue(collider.Value.Value.CollisionType == CollisionType.Convex);
            unsafe
            {
                ConvexCollider* header = (ConvexCollider*)collider.ColliderPtr;
                CollisionFilter filter = header->Filter;

                filter.BelongsTo |= 1u << belongsTo;

                header->Filter = filter;
            }
        }
    }
}

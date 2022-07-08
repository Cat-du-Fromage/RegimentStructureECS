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
    public static unsafe class PhysicsUtils
    {
        /*
        /// <summary>
        /// ECS RAYCAST BASIC Construction
        /// </summary>
        /// <param name="fromPosition"></param>
        /// <param name="toPosition"></param>
        /// <returns></returns>
        public static Entity Raycast(float3 fromPosition, float3 toPosition, uint collisionFilter)
        {
            BuildPhysicsWorld physicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
            CollisionWorld collisionWorld = physicsWorld.PhysicsWorld.CollisionWorld;

            RaycastInput raycastInput = new RaycastInput
            {
                Start = fromPosition,
                End = toPosition,
                //Layer filter
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, //belongs to all layers
                    CollidesWith = collisionFilter, //collides with all layers
                    GroupIndex = 0,
                }
            };
            
            Entity hit = !collisionWorld.CastRay(raycastInput, out RaycastHit raycastHit)
                ? Entity.Null
                : physicsWorld.PhysicsWorld.Bodies[raycastHit.RigidBodyIndex].Entity;
            
            return hit;
        }
        */
        [BurstCompile]
        public struct RaycastJob : IJobParallelFor
        {
            [ReadOnly] public CollisionWorld world;
            [ReadOnly] public NativeArray<RaycastInput> inputs;
            public NativeArray<RaycastHit> results;

            public unsafe void Execute(int index)
            {
                RaycastHit hit;
                world.CastRay(inputs[index], out hit);
                results[index] = hit;
            }
        }

        public static JobHandle ScheduleBatchRayCast(CollisionWorld world, NativeArray<RaycastInput> inputs, NativeArray<RaycastHit> results)
        {
            JobHandle rcj = new RaycastJob
            {
                inputs = inputs,
                results = results,
                world = world

            }.Schedule(inputs.Length, 4);
            return rcj;
        }
        
        public static void SingleRayCast(CollisionWorld world, RaycastInput input, ref RaycastHit result)
        {
            NativeArray<RaycastInput> rayCommands = new NativeArray<RaycastInput>(1, Allocator.TempJob);
            NativeArray<RaycastHit> rayResults = new NativeArray<RaycastHit>(1, Allocator.TempJob);
            rayCommands[0] = input;
            ScheduleBatchRayCast(world, rayCommands, rayResults).Complete();
            
            result = rayResults[0];
            rayCommands.Dispose();
            rayResults.Dispose();
        }
        
        public static Entity SphereCast(float3 rayFrom, float3 rayTo, float radius)
        {
            BuildPhysicsWorld physicsWorldSystem  = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>(); 
            CollisionWorld collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

            CollisionFilter filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            };

            SphereGeometry sphereGeometry = new SphereGeometry() { Center = float3.zero, Radius = radius };
            BlobAssetReference<Collider> sphereCollider = SphereCollider.Create(sphereGeometry, filter);

            ColliderCastInput input = new ColliderCastInput()
            {
                Collider = (Collider*)sphereCollider.GetUnsafePtr(),
                Orientation = quaternion.identity,
                Start = rayFrom,
                End = rayTo
            };

            ColliderCastHit hit = new ColliderCastHit();
            bool haveHit = collisionWorld.CastCollider(input, out hit);
            if (haveHit)
            {
                // see hit.Position
                // see hit.SurfaceNormal
                Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
                return e;
            }

            sphereCollider.Dispose();

            return Entity.Null;
        }
        

    }
}

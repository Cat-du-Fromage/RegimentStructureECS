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
    public static unsafe class RaycastUtils
    {
        /// <summary>
        /// ECS RAYCAST BASIC Construction
        /// </summary>
        /// <param name="collisionWorld"></param>
        /// <param name="fromPosition"></param>
        /// <param name="toPosition"></param>
        /// <param name="collisionFilter"></param>
        /// <returns></returns>
        public static unsafe RaycastHit Raycast(this CollisionWorld collisionWorld, in float3 fromPosition, in float3 toPosition, float distance,CollisionFilter collisionFilter)
        {
            RaycastInput input = new RaycastInput
            {
                Start = fromPosition,
                End = fromPosition + toPosition * distance,
                Filter = collisionFilter
            };

            collisionWorld.CastRay(input, out RaycastHit hit);
            return hit;
        }
        
        public static unsafe bool Raycast(UnityEngine.Ray ray, out RaycastHit entityHit, float distance, int collisionFilter)
        {
            //UnityEngine.Ray singleRay = playerCamera.ScreenPointToRay(mousePosition);
            float3 fromPosition = ray.origin;
            float3 toPosition = ray.GetPoint(distance);
            
            BuildPhysicsWorld physicsWorld = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BuildPhysicsWorld>();
            CollisionWorld collisionWorld = physicsWorld.PhysicsWorld.CollisionWorld;

            RaycastInput input = new RaycastInput
            {
                Start = fromPosition,
                End = toPosition,
                //Layer filter
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, //belongs to all layers
                    CollidesWith = 1u << collisionFilter, //collides with all layers
                    GroupIndex = 0,
                }
            };
            
            return collisionWorld.CastRay(input, out entityHit);
        }
        
        public static unsafe bool Raycast(this CollisionWorld collisionWorld, UnityEngine.Ray ray, out RaycastHit entityHit, float distance, int collisionFilter)
        {
            float3 fromPosition = ray.origin;
            float3 toPosition = ray.GetPoint(distance);

            RaycastInput input = new RaycastInput
            {
                Start = fromPosition,
                End = toPosition,
                //Layer filter
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, //belongs to all layers
                    CollidesWith = 1u << collisionFilter, //collides with all layers
                    GroupIndex = 0,
                }
            };
            
            return collisionWorld.CastRay(input, out entityHit);
        }

        public static RaycastInput GetRaycastInput(this UnityEngine.Ray ray, float distance, int collisionFilter)
        {
            RaycastInput input = new RaycastInput
            {
                Start = ray.origin,
                End = ray.GetPoint(distance),
                //Layer filter
                Filter = new CollisionFilter
                {
                    BelongsTo = ~0u, //belongs to all layers
                    CollidesWith = 1u << collisionFilter, //collides with all layers
                    GroupIndex = 0,
                }
            };
            return input;
        }
        
        public static JobHandle ScheduleBatchRayCast(CollisionWorld world, NativeArray<RaycastInput> inputs, NativeArray<RaycastHit> results, JobHandle dependency = default)
        {
            JobHandle rcj = new RaycastJob
            {
                inputs = inputs,
                results = results,
                world = world

            }.ScheduleParallel(inputs.Length, 4, dependency);
            return rcj;
        }
        
        public static RaycastHit SingleRayCast(CollisionWorld world, RaycastInput input, JobHandle dependency = default)
        {
            NativeArray<RaycastInput> rayCommands = new (1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            NativeArray<RaycastHit> rayResults = new (1, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
            rayCommands[0] = input;
            
            ScheduleBatchRayCast(world, rayCommands, rayResults, dependency).Complete();

            RaycastHit result = rayResults[0];
            rayCommands.Dispose();
            rayResults.Dispose();
            return result;
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    public struct RaycastJob : IJobFor
    {
        [ReadOnly] public CollisionWorld world;
        [ReadOnly] public NativeArray<RaycastInput> inputs;
        [WriteOnly] public NativeArray<RaycastHit> results;

        public unsafe void Execute(int index)
        {
            world.CastRay(inputs[index], out RaycastHit hit);
            results[index] = hit;
        }
    }
    
    [BurstCompile(CompileSynchronously = true)]
    public struct JSingleRaycast : IJob
    {
        [ReadOnly] public CollisionWorld World;
        [ReadOnly] public NativeReference<RaycastInput> Input;
        [WriteOnly] public NativeReference<RaycastHit> Result;

        public unsafe void Execute()
        {
            World.CastRay(Input.Value, out RaycastHit hit);
            Result.Value = hit;
        }
    }
}

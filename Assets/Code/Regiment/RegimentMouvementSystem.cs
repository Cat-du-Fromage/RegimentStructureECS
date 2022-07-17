using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using KWUtils;
using UnityEngine;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlacementSystem))]
    public partial class RegimentMouvementSystem : SystemBase
    {
        private EntityQuery regimentMovingQuery;
        private EntityQuery unitsMovingQuery;
        protected override void OnCreate()
        {
            regimentMovingQuery = EntityManager.CreateEntityQuery(typeof(Tag_Regiment), typeof(Tag_Move));
            unitsMovingQuery = EntityManager.CreateEntityQuery(typeof(Tag_Unit), typeof(Shared_RegimentEntity), typeof(Translation), typeof(Data_AnimationPlayed));
            RequireForUpdate(regimentMovingQuery);
        }

        protected override void OnUpdate()
        {
            NativeArray<Entity> regiments = regimentMovingQuery.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < regiments.Length; i++)
            {
                Entity regimentEntity = regiments[i];
                unitsMovingQuery.SetSharedComponentFilter(new Shared_RegimentEntity(){Value = regimentEntity});
                NativeArray<float3> destinations = GetBuffer<Buffer_Destinations>(regimentEntity, true).Reinterpret<float3>().AsNativeArray();

                NativeCounter counter = new NativeCounter(Allocator.TempJob);
                
                JMoveUnits job = new JMoveUnits
                {
                    Speed = GetComponent<Data_RegimentClass>(regimentEntity).Speed,
                    DeltaTime = Time.DeltaTime,
                    LookRotation = GetComponent<Data_LookRotation>(regimentEntity).Value,
                    Destinations = destinations,
                    UnitArrivedCounter = counter
                };
                Dependency = job.ScheduleParallel(unitsMovingQuery, Dependency);
                Dependency.Complete();

                CheckRegimentArrived(counter, regimentEntity);
                
                destinations.Dispose(Dependency);
                counter.Dispose();
                unitsMovingQuery.ResetFilter();
            }
        }

        private void CheckRegimentArrived(NativeCounter counter, in Entity regiment)
        {
            if (counter.Count != unitsMovingQuery.CalculateEntityCount()) return;
            EntityManager.RemoveComponent<Tag_Move>(regiment);
        }


        [BurstCompile(CompileSynchronously = true)]
        [WithAll(typeof(Tag_Unit),typeof(Translation),typeof(Data_AnimationPlayed))]
        public partial struct JMoveUnits : IJobEntity
        {
            [ReadOnly] public float Speed;
            [ReadOnly] public float DeltaTime;
            [ReadOnly] public quaternion LookRotation;
            [ReadOnly] public NativeArray<float3> Destinations;
            [WriteOnly] public NativeCounter.ParallelWriter UnitArrivedCounter;
            
            public void Execute([EntityInQueryIndex] int entityInQueryIndex, ref Translation position, ref Rotation rotation, ref Data_AnimationPlayed animationToPlay)
            {
                float3 destination = Destinations[entityInQueryIndex];
                float timeSpeed = DeltaTime * Speed;
                float dst = distance(destination, position.Value);

                if (dst <= 0.06f) // NEED FOR COUNTER
                {
                    animationToPlay.Value = FusilierClips.Idle;
                    UnitArrivedCounter.Increment();
                    return;
                }
                
                quaternion newRotation = slerp(rotation.Value, LookRotation, timeSpeed);
                float2 direction =  normalizesafe(destination.xz - position.Value.xz);
                float3 newPosition = mad(new float3(direction.x, 0.0f, direction.y), timeSpeed, position.Value);

                position.Value = newPosition;
                rotation.Value = newRotation;
            }
        }
    }
}
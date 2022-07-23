using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

using static Unity.Mathematics.math;

namespace KaizerWald
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(RearrangementSystem))]
    public partial class RearrangementMoveSystem : SystemBase
    {
        private EntityQuery unitRearrangingQuery;
        protected override void OnCreate()
        {
            unitRearrangingQuery = EntityManager.CreateEntityQuery(typeof(Tag_Unit), typeof(Tag_MoveRearrange));
            RequireForUpdate(unitRearrangingQuery);
        }

        protected override void OnStartRunning()
        {
            Debug.Log("RearrangeMove");
        }

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;
            Entities
            .WithoutBurst()
            .WithStructuralChanges()
            .WithAll<Tag_Unit, Tag_MoveRearrange>()
            .WithStoreEntityQueryInField(ref unitRearrangingQuery)
            .ForEach((Entity unit ,ref Translation position, ref Data_AnimationPlayed anim ,in Data_Regiment regiment, in Data_IndexInRegiment indexInRegiment) =>
            {
                if (anim.Value != FusilierClips.Run)
                {
                    anim.Value = FusilierClips.Run;
                }
                
                float3 destination = GetBuffer<Buffer_Destinations>(regiment.Value)[indexInRegiment.Value].Position;
                float2 direction =  normalizesafe(destination.xz - position.Value.xz);
                float3 newPosition = mad(new float3(direction.x, 0.0f, direction.y), deltaTime*2, position.Value);
                
                position.Value = newPosition;
                
                float dst = distance(destination, position.Value);
                if (dst <= 0.06f)
                {
                    anim.Value = FusilierClips.Idle;
                    EntityManager.RemoveComponent<Tag_MoveRearrange>(unit);
                }
            }).Run();
        }
    }
}
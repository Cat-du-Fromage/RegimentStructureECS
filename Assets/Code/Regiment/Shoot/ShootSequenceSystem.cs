using System;
using Latios.Kinemation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

using static Unity.Entities.ComponentType;
using Random = Unity.Mathematics.Random;
using static Unity.Mathematics.Random;
namespace KaizerWald
{
    public enum UnitSequence
    {
        Idle = 0,
        Move = 1,
        Fire = 2,
        Dead = 3,
    }
    
    public struct State_PreviousAction : IComponentData
    {
        public FusilierClips Value;
    }
    
    public struct State_CurrentAction : IComponentData
    {
        public FusilierClips Value;
    }
    
    public partial class ShootSequenceSystem : SystemBase
    {
        [BurstCompile]
        public struct JAnimationSequence : IJobEntityBatch
        {
            //[ReadOnly] public ComponentTypeHandle<State_NewOrder> NewOrderTypeHandle;
            
            [ReadOnly] public ComponentTypeHandle<State_PreviousAction> PreviousActionTypeHandle;
            [ReadOnly] public ComponentTypeHandle<State_CurrentAction> CurrentActionTypeHandle;
            
            public ComponentTypeHandle<Sequence_Fire> SequenceFireTypeHandle;
            public ComponentTypeHandle<Data_AnimationPlayed> AnimationPlayedTypeHandle;
            
            public uint LastSystemVersion;

            public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
            {
                bool changed = batchInChunk.DidOrderChange(LastSystemVersion)
                               || batchInChunk.DidChange(PreviousActionTypeHandle, LastSystemVersion)
                               || batchInChunk.DidChange(CurrentActionTypeHandle, LastSystemVersion)
                               || batchInChunk.DidChange(SequenceFireTypeHandle, LastSystemVersion);
                if (!changed) return;
                
                int count = batchInChunk.Count;
                //bool hasNewOrder = batchInChunk.Has(NewOrderTypeHandle);
                //Change State
                bool hasPreviousAction = batchInChunk.Has(PreviousActionTypeHandle);
                bool hasCurrentAction = batchInChunk.Has(CurrentActionTypeHandle);
                NativeArray<State_PreviousAction> chunkPreviousActions = batchInChunk.GetNativeArray(PreviousActionTypeHandle);
                NativeArray<State_CurrentAction> chunkCurrentActions = batchInChunk.GetNativeArray(CurrentActionTypeHandle);
                
                //Animation actually played
                bool hasAnimationPlayed = batchInChunk.Has(AnimationPlayedTypeHandle);
                NativeArray<Data_AnimationPlayed> chunkAnimationPlayed = batchInChunk.GetNativeArray(AnimationPlayedTypeHandle);

                //1) Check if animation Change
                if (hasAnimationPlayed)
                {
                    for (int i = 0; i < count; i++)
                    {
                        FusilierClips currentClips = chunkCurrentActions[i].Value;
                        if (chunkPreviousActions[i].Value == currentClips) continue;
                        chunkAnimationPlayed[i] = new Data_AnimationPlayed() { Value = currentClips };
                        chunkPreviousActions[i] = new State_PreviousAction() { Value = currentClips };
                    }
                }
                
                //2) Check if animation Change
            }
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private partial struct JFireSequenceEvent : IJobEntity
        {
            [ReadOnly] public float Time;
            
            public void Execute(
                ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer,
                in OptimizedSkeletonHierarchyBlobReference hierarchyRef,
                in SingleClip singleClip,
                in Data_AnimationPlayed animationToPlay,
                in Sequence_Fire sequenceFire)
            {
                int animationIndex = (int)animationToPlay.Value;
                ref SkeletonClip clip = ref singleClip.blob.Value.clips[animationIndex];
                float clipTime = clip.LoopToClipTime(Time);
                clip.SamplePose(btrBuffer, hierarchyRef.blob, clipTime);
            }
        }

        protected override void OnUpdate()
        {
            return;
        }
    }
}

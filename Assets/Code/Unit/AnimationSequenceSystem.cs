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
    public struct FireSequenceClips : IComponentData
    {
        public BlobAssetReference<SkeletonClipSetBlob> blob;
    }
    
    public struct Data_FireEnd : IComponentData
    {
        public bool Value;
    }
    
    public struct Data_ReloadEnd : IComponentData
    {
        public bool Value;
    }
    
    public struct Sequence_Fire : IComponentData
    {
        public bool FireEnd;
        public bool ReloadEnd;
    }

    // Find changed animations => change current animation
    // Play Animation
    
    public partial class AnimationSequenceSystem : SystemBase
    {
        private EntityQuery regimentQuery;
        private EntityQuery unitsQuery;
        
        protected override void OnCreate()
        {
            regimentQuery = GetEntityQuery( ReadOnly<Tag_Regiment>());
            unitsQuery = GetEntityQuery(ReadOnly<Tag_Unit>(), ReadOnly<Shared_RegimentEntity>());
        }

        protected override void OnUpdate()
        {
            return;
        }

        private void EventSystem()
        {
            
        }

        private void Test()
        {
            float time = (float)Time.ElapsedTime;
            using NativeArray<Entity> regiments = regimentQuery.ToEntityArray(Allocator.TempJob);
            
            //For regiment
            // SuperState : enum  RegimentUnitSequence (Static: Define a set of animation)
            // SubState : enum RegimentFusilierClip (Dynamic: this is more an Initial State, this will change on Unit)
            
            // => On New State_NewOrder : Set Both RegimentUnitSequence and  RegimentFusilierClip
            // check on "Previous RegimentUnitSequence" and "Current RegimentUnitSequence" to check change
            
            //SEPARATE:
            //1) (begin with: regiment) Setup : when State_NewOrder we set all sequence to false (and events)
            //1*) How to Select Units Only?... setFilter remove "State_NewOrder" when Sequence value Setup
            
            //2) (begin with: unit) Actual Animation : Sequence Run
            
            
            for (int i = 0; i < regiments.Length; i++)
            {
                Entity regiment = regiments[i];
                FusilierClips animationPlayed = GetComponent<Data_RegimentAnimationPlayed>(regiment).Value;
                
                unitsQuery.SetSharedComponentFilter(new Shared_RegimentEntity() { Value = regiment });

            }
        }



        [BurstCompile(CompileSynchronously = true)]
        private partial struct JFireSequence : IJobEntity
        {
            [ReadOnly] public float Time;
            
            public void Execute(
                ref Data_FireEnd end,
                ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer,
                in OptimizedSkeletonHierarchyBlobReference hierarchyRef,
                in SingleClip singleClip,
                in Data_AnimationPlayed animationToPlay)
            {
                int animationIndex = (int)animationToPlay.Value;
                ref SkeletonClip clip = ref singleClip.blob.Value.clips[animationIndex];
                //float rand = CreateFromIndex((uint)index.Value).NextFloat(0.7f, 1f);
                float clipTime = clip.LoopToClipTime(Time/* * rand*/);
                clip.SamplePose(btrBuffer, hierarchyRef.blob, clipTime);
            }
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private partial struct JIdleSequence : IJobEntity
        {
            [ReadOnly] public float Time;
            
            public void Execute(
                ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer,
                in OptimizedSkeletonHierarchyBlobReference hierarchyRef,
                in SingleClip singleClip,
                in Data_AnimationPlayed animationToPlay,
                in Data_IndexInRegiment index)
            {
                int animationIndex = (int)animationToPlay.Value;
                ref SkeletonClip clip = ref singleClip.blob.Value.clips[animationIndex];
                //float rand = CreateFromIndex((uint)index.Value).NextFloat(0.7f, 1f);
                float clipTime = clip.LoopToClipTime(Time/* * rand*/);
                clip.SamplePose(btrBuffer, hierarchyRef.blob, clipTime);
            }
        }
    }
}
using Latios;
using Latios.Kinemation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Rendering.VirtualTexturing;
using static Unity.Mathematics.math;
using Random = Unity.Mathematics.Random;
using static Unity.Mathematics.Random;

namespace KaizerWald
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class SingleClipPlayerSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float t = (float)Time.ElapsedTime;
            //uint stateIndex = (uint)(entityInQueryIndex + select(t, t / uint.MaxValue, t >= uint.MaxValue));
            new JOptimizedMeshMultiClips() { Time = t }.ScheduleParallel();
            //OptimizedMeshMultiClips(t);
        }

        private void OptimizedMeshMultiClips(float t)
        {
            Entities
            .WithBurst(FloatMode.Default, FloatPrecision.Standard, true)
            .WithName("MultiClipPlayerSystem_OptimizedMesh")
            .WithAll<Tag_Unit>()
            .ForEach((int entityInQueryIndex,
            ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer, 
            in OptimizedSkeletonHierarchyBlobReference hierarchyRef, 
            in SingleClip singleClip,
            in Data_AnimationPlayed animationToPlay) =>
            {
                int animationIndex = (int)animationToPlay.Value;
                ref SkeletonClip clip = ref singleClip.blob.Value.clips[animationIndex];
                float rand = CreateFromIndex((uint)entityInQueryIndex).NextFloat(0.6f, 1f);
                float clipTime = clip.LoopToClipTime(t * rand);
                
                clip.SamplePose(btrBuffer, hierarchyRef.blob, clipTime);
            }).ScheduleParallel();
        }
        
        [BurstCompile(CompileSynchronously = true)]
        private partial struct JOptimizedMeshMultiClips : IJobEntity
        {
            [ReadOnly] public float Time;
            public void Execute([EntityInQueryIndex] int entityInQueryIndex,ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer, in OptimizedSkeletonHierarchyBlobReference hierarchyRef, in SingleClip singleClip, in Data_AnimationPlayed animationToPlay)
            {
                int animationIndex = (int)animationToPlay.Value;
                ref SkeletonClip clip = ref singleClip.blob.Value.clips[animationIndex];
                float rand = CreateFromIndex((uint)entityInQueryIndex).NextFloat(0.6f, 1f);
                float clipTime = clip.LoopToClipTime(Time * rand);
                //Debug.Log($"Time {clipTime}; Duration = {clip.duration}");
                clip.SamplePose(btrBuffer, hierarchyRef.blob, clipTime);
            }
        }
        
        private void OptimizedMeshSingleClip(float t)
        {
            Entities
                .WithName("SingleClipPlayerSystem_OptimizedMesh")
                .WithBurst()
                .ForEach((int entityInQueryIndex,
                    ref DynamicBuffer<OptimizedBoneToRoot> btrBuffer, 
                    in OptimizedSkeletonHierarchyBlobReference hierarchyRef, 
                    in SingleClip singleClip) =>
                {
                    float rand = Random.CreateFromIndex((uint)entityInQueryIndex).NextFloat(0.3f, 1f);
                    ref SkeletonClip clip = ref singleClip.blob.Value.clips[0];
                    float clipTime = clip.LoopToClipTime(t * rand);
                    clip.SamplePose(btrBuffer, hierarchyRef.blob, clipTime);
                }).ScheduleParallel();
        }
        
        private void UsingParallel2(float t)
        {
            Entities
            .ForEach((ref Translation trans, ref Rotation rot, ref NonUniformScale scale, in SingleClip singleClip, in BoneIndex boneIndex) =>
            {
                if (boneIndex.index == 0) return;

                ref SkeletonClip clip = ref singleClip.blob.Value.clips[0];
                float clipTime = clip.LoopToClipTime(t);

                BoneTransform boneTransform = clip.SampleBone(boneIndex.index, clipTime);

                trans.Value = boneTransform.translation;
                rot.Value   = boneTransform.rotation;
                scale.Value = boneTransform.scale;
            }).ScheduleParallel();
        }

        private void UsingParallel(float t)
        {
            Entities
            .WithBurst()
            .ForEach((ref Translation trans, ref Rotation rot, ref NonUniformScale scale, in BoneOwningSkeletonReference skeletonRef, in BoneIndex boneIndex) =>
            {
                if (boneIndex.index == 0) return;
                SingleClip singleClip = GetComponent<SingleClip>(skeletonRef.skeletonRoot);

                ref SkeletonClip clip     = ref singleClip.blob.Value.clips[0];
                float clipTime = clip.LoopToClipTime(t);

                BoneTransform boneTransform = clip.SampleBone(boneIndex.index, clipTime);
                trans.Value = boneTransform.translation;
                rot.Value   = boneTransform.rotation;
                scale.Value = boneTransform.scale;
            }).ScheduleParallel();
        }

        private void UsingSchedule(float t)
        {
            Entities
            .WithBurst()
            .ForEach((in DynamicBuffer<BoneReference> bones, in SingleClip singleClip) =>
            {
                ref SkeletonClip clip = ref singleClip.blob.Value.clips[0];
                float clipTime = clip.LoopToClipTime(t);
                for (int i = 1; i < bones.Length; i++)
                {
                    BoneTransform boneTransform = clip.SampleBone(i, clipTime);

                    Translation trans = new Translation { Value = boneTransform.translation };
                    Rotation rot   = new Rotation { Value = boneTransform.rotation };
                    NonUniformScale scale = new NonUniformScale { Value = boneTransform.scale };

                    Entity entity = bones[i].bone;

                    SetComponent(entity, trans);
                    SetComponent(entity, rot);
                    SetComponent(entity, scale);
                }
            }).Schedule();
        }
    }
}
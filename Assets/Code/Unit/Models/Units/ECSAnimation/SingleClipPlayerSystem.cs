using Latios;
using Latios.Kinemation;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace KaizerWald
{
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class SingleClipPlayerSystem : SubSystem
    {
        protected override void OnUpdate()
        {
            float t = (float)Time.ElapsedTime;
            UsingParallel2(t);
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
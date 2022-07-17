using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using Unity.Collections;

//THIS PART IS MADE TO REFERENCE AN ANIMATION CLIP DIRECTLY ON AN GAMEOBJECT
namespace KaizerWald
{
    //[Flags]
    public enum FusilierClips : int
    {
        Idle        = 0,
        Walk        = 1,
        Run         = 2,
        AimIdle     = 3,
        AimDown     = 4,
        AimUp       = 5,
        Fire        = 6,
        Reload      = 7,
        DeathFront1 = 8,
        DeathFront2 = 9,
        DeathFront3 = 10,
    }

    public struct MultipleClip : IComponentData
    {
        public BlobAssetReference<SkeletonClipSetBlob> blob;
    }
    
    [DisallowMultipleComponent]
    public class MultipleClipAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IRequestBlobAssets
    {
        [SerializeField] private AnimationClip[] clips;

        private SmartBlobberHandle<SkeletonClipSetBlob> blob;

        public void RequestBlobAssets(Entity entity, EntityManager dstEntityManager, GameObjectConversionSystem conversionSystem)
        {
            SkeletonClipConfig[] configs = new SkeletonClipConfig[clips.Length];
            for (int i = 0; i < configs.Length; i++)
            {
                configs[i] = new SkeletonClipConfig(){ clip = clips[i], settings = SkeletonClipCompressionSettings.kDefaultSettings };
            }
            
            blob = conversionSystem.CreateBlob(gameObject, new SkeletonClipSetBakeData
            {
                animator = GetComponent<Animator>(),
                clips    = configs
            });
        }
        
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            SingleClip singleClip = new SingleClip { blob = blob.Resolve() };
            dstManager.AddComponentData(entity, new Data_AnimationPlayed() { Value = (FusilierClips)0 });

            if (dstManager.HasComponent<BoneReference>(entity))
            {
                NativeArray<BoneReference> bones = dstManager.GetBuffer<BoneReference>(entity, true).ToNativeArray(Allocator.Temp);
                foreach (BoneReference b in bones)
                {
                    dstManager.AddComponentData(b.bone, singleClip);
                }
            }
            else
            {
                dstManager.AddComponentData(entity, singleClip);
            }
        }
    }
}
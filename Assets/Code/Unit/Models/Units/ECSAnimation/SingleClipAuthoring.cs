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
    public struct SingleClip : IComponentData
    {
        public BlobAssetReference<SkeletonClipSetBlob> blob;
    }
    
    [DisallowMultipleComponent]
    public class SingleClipAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IRequestBlobAssets
    {
        public AnimationClip clip;

        private SmartBlobberHandle<SkeletonClipSetBlob> blob;

        public void RequestBlobAssets(Entity entity, EntityManager dstEntityManager, GameObjectConversionSystem conversionSystem)
        {
            SkeletonClipConfig config = new SkeletonClipConfig { clip = clip, settings = SkeletonClipCompressionSettings.kDefaultSettings };
            blob = conversionSystem.CreateBlob(gameObject, new SkeletonClipSetBakeData
            {
                animator = GetComponent<Animator>(),
                clips    = new SkeletonClipConfig[] { config }
            });
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            SecondTutorial(entity, dstManager);
            //SingleClip singleClip = new SingleClip { blob = blob.Resolve() };
            //dstManager.AddComponentData(entity, singleClip);
        }

        private void FirstTutorial(Entity entity, EntityManager dstManager)
        {
            SingleClip singleClip = new SingleClip { blob = blob.Resolve() };
            dstManager.AddComponentData(entity, singleClip);
        }
        
        private void SecondTutorial(Entity entity, EntityManager em)
        {
            SingleClip singleClip = new SingleClip { blob = blob.Resolve() };

            if (em.HasComponent<BoneReference>(entity))
            {
                NativeArray<BoneReference> bones = em.GetBuffer<BoneReference>(entity, true).ToNativeArray(Allocator.Temp);
                foreach (BoneReference b in bones)
                {
                    em.AddComponentData(b.bone, singleClip);
                }
            }
            else
            {
                em.AddComponentData(entity, singleClip);
            }
        }
    }
}
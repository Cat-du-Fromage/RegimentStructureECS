using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    public interface IBlobify<T> where T: struct
    {
        public BlobAssetReference<RegimentClass> CreateBlob(Allocator allocator);
    }
    
    public struct RegimentClass : IBlobify<RegimentClass>
    {
        public int NumUnit;
        public int MinLine;
        public int MaxLine;
        
        public BlobAssetReference<RegimentClass> CreateBlob(Allocator allocator)
        {
            using (BlobBuilder blob = new (Allocator.TempJob))
            {
                ref RegimentClass regimentClass = ref blob.ConstructRoot<RegimentClass>();
                regimentClass.NumUnit = 100;
                regimentClass.MinLine = 4;
                regimentClass.MaxLine = 10;
                return blob.CreateBlobAssetReference<RegimentClass>(allocator);
            }
        }
    }
    
    public struct SimpleAnimationBlob
    {
        BlobArray<float> Keys;
        float            InvLength;
        float            KeyCount;

        // When t exceeds the curve time, repeat it

        public static BlobAssetReference<SimpleAnimationBlob> CreateBlob(AnimationCurve curve, Allocator allocator)
        {
            using (BlobBuilder blob = new (Allocator.TempJob))
            {
                ref SimpleAnimationBlob anim = ref blob.ConstructRoot<SimpleAnimationBlob>();
                int keyCount = 12;

                float endTime = curve[curve.length - 1].time;
                anim.InvLength = 1.0F / endTime;
                anim.KeyCount = keyCount;

                BlobBuilderArray<float> array = blob.Allocate(ref anim.Keys, keyCount + 1);
                for (int i = 0; i < keyCount; i++)
                {
                    float t = (float) i / (float)(keyCount - 1) * endTime;
                    array[i] = curve.Evaluate(t);
                }
                array[keyCount] = array[keyCount-1];

                return blob.CreateBlobAssetReference<SimpleAnimationBlob>(allocator);
            }
        }
    }
}

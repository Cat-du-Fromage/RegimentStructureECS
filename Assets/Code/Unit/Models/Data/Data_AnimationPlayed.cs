using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    [Serializable]
    public struct Data_AnimationPlayed : IComponentData
    {
        public FusilierClips Value;
    }
}
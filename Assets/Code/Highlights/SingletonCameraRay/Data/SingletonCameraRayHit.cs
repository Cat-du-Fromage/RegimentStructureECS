using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    [Serializable]
    public struct SingletonCameraRayHit : IComponentData
    {
        public Entity UnitHit;
        public Entity RegimentHit;
    }
}
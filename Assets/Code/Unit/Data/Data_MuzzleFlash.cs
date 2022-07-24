using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    [GenerateAuthoringComponent]
    public struct Data_MuzzleFlash : IComponentData
    {
        public Entity Value;
    }
}
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace KaizerWald
{
    public class Authoring_PlayerCamera : IComponentData
    {
        public Camera Value;
    }
}
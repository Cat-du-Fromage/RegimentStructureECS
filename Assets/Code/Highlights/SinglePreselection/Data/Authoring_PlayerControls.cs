using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace KaizerWald
{
    public class Authoring_PlayerControls : IComponentData
    {
        public PlayerControls Value;
    }
}